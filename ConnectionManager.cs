using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.PowerPlatform.Dataverse.Client.Model;

namespace Dynamics365UserManager
{
    public class EnvironmentInfo
    {
        public string Id { get; set; }
        public string FriendlyName { get; set; }
        public string Url { get; set; }
        public string ApiUrl { get; set; }
        public string State { get; set; }
        public string Purpose { get; set; }
        public string Region { get; set; }
        public string Version { get; set; }
    }

    public class ConnectionManager : IDisposable
    {
        private const string ClientId = "51f81489-12ee-4a9e-aaae-a2591f45987d";
        private const string Authority = "https://login.microsoftonline.com/organizations";
        private const string DiscoveryUrl = "https://globaldisco.crm.dynamics.com/api/discovery/v2.0/Instances";
        private static readonly string[] DiscoveryScopes = { "https://globaldisco.crm.dynamics.com/.default" };
        private static readonly HttpClient _httpClient = new HttpClient();

        private readonly string _tokenCachePath;
        private IPublicClientApplication _msalApp;
        private ServiceClient _serviceClient;
        private string _currentAccessToken;

        public ServiceClient ServiceClient => _serviceClient;
        public bool IsConnected => _serviceClient != null && _serviceClient.IsReady;
        public EnvironmentInfo CurrentEnvironment { get; private set; }

        public ConnectionManager()
        {
            var appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Dynamics365UserManager");
            Directory.CreateDirectory(appDataDir);
            _tokenCachePath = Path.Combine(appDataDir, "tokencache.dat");

            InitializeMsal();
        }

        private void InitializeMsal()
        {
            _msalApp = PublicClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(Authority)
                .WithRedirectUri("http://localhost")
                .Build();

            _msalApp.UserTokenCache.SetBeforeAccess(args =>
            {
                if (File.Exists(_tokenCachePath))
                {
                    try
                    {
                        var encrypted = File.ReadAllBytes(_tokenCachePath);
                        var data = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                        args.TokenCache.DeserializeMsalV3(data);
                    }
                    catch
                    {
                        File.Delete(_tokenCachePath);
                    }
                }
            });

            _msalApp.UserTokenCache.SetAfterAccess(args =>
            {
                if (args.HasStateChanged)
                {
                    var data = args.TokenCache.SerializeMsalV3();
                    var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
                    File.WriteAllBytes(_tokenCachePath, encrypted);
                }
            });
        }

        public async Task<string> AuthenticateAsync()
        {
            var accounts = await _msalApp.GetAccountsAsync();
            AuthenticationResult result;

            try
            {
                result = await _msalApp.AcquireTokenSilent(DiscoveryScopes, accounts.FirstOrDefault())
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                result = await _msalApp.AcquireTokenInteractive(DiscoveryScopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync();
            }

            _currentAccessToken = result.AccessToken;
            return _currentAccessToken;
        }

        public async Task<List<EnvironmentInfo>> GetAvailableEnvironmentsAsync()
        {
            if (string.IsNullOrEmpty(_currentAccessToken))
                throw new InvalidOperationException("Non autenticato. Eseguire prima il login.");

            var environments = new List<EnvironmentInfo>();

            using var request = new HttpRequestMessage(HttpMethod.Get, DiscoveryUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentAccessToken);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("value", out var instances))
            {
                foreach (var instance in instances.EnumerateArray())
                {
                    var state = instance.TryGetProperty("State", out var stateProp) ? stateProp.ToString() : null;
                    if (state != "0" && !string.Equals(state, "Enabled", StringComparison.OrdinalIgnoreCase))
                        continue;

                    environments.Add(new EnvironmentInfo
                    {
                        Id = instance.TryGetProperty("Id", out var idProp) ? idProp.ToString() : null,
                        FriendlyName = instance.TryGetProperty("FriendlyName", out var nameProp) ? nameProp.ToString() : null,
                        Url = instance.TryGetProperty("Url", out var urlProp) ? urlProp.ToString() : null,
                        ApiUrl = instance.TryGetProperty("ApiUrl", out var apiProp) ? apiProp.ToString() : null,
                        State = state,
                        Purpose = instance.TryGetProperty("Purpose", out var purpProp) ? purpProp.ToString() : null,
                        Region = instance.TryGetProperty("Region", out var regProp) ? regProp.ToString() : null,
                        Version = instance.TryGetProperty("Version", out var verProp) ? verProp.ToString() : null,
                    });
                }
            }

            return environments;
        }

        public async Task ConnectToEnvironmentAsync(EnvironmentInfo environment)
        {
            CurrentEnvironment = environment;

            var envUrl = environment.Url.TrimEnd('/');
            var resource = envUrl + "/";
            string[] crmScopes = { resource + ".default" };

            var accounts = await _msalApp.GetAccountsAsync();
            AuthenticationResult authResult;
            try
            {
                authResult = await _msalApp.AcquireTokenSilent(crmScopes, accounts.FirstOrDefault())
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                authResult = await _msalApp.AcquireTokenInteractive(crmScopes)
                    .ExecuteAsync();
            }

            var cachedScopes = crmScopes;
            var cachedApp = _msalApp;

            var connectionOptions = new ConnectionOptions
            {
                AuthenticationType = AuthenticationType.ExternalTokenManagement,
                ServiceUri = new Uri(envUrl),
                AccessTokenProviderFunctionAsync = async (instanceUri) =>
                {
                    var accts = await cachedApp.GetAccountsAsync();
                    try
                    {
                        var result = await cachedApp.AcquireTokenSilent(cachedScopes, accts.FirstOrDefault())
                            .ExecuteAsync();
                        return result.AccessToken;
                    }
                    catch (MsalUiRequiredException)
                    {
                        var result = await cachedApp.AcquireTokenInteractive(cachedScopes)
                            .ExecuteAsync();
                        return result.AccessToken;
                    }
                }
            };

            _serviceClient = await Task.Run(() => new ServiceClient(connectionOptions));

            if (!_serviceClient.IsReady)
            {
                var error = _serviceClient.LastError;
                _serviceClient = null;
                throw new Exception($"Connessione fallita: {error}");
            }
        }

        public void Disconnect()
        {
            _serviceClient?.Dispose();
            _serviceClient = null;
            CurrentEnvironment = null;
        }

        public void ResetLogin()
        {
            Disconnect();
            if (File.Exists(_tokenCachePath))
                File.Delete(_tokenCachePath);
            InitializeMsal();
        }

        public void Dispose()
        {
            _serviceClient?.Dispose();
        }
    }
}
