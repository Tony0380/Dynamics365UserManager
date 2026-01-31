using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.WebServiceClient;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json.Linq;

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

        private readonly string _tokenCachePath;
        private IPublicClientApplication _msalApp;
        private CrmServiceClient _serviceClient;
        private string _currentAccessToken;

        public CrmServiceClient ServiceClient => _serviceClient;
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

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _currentAccessToken);

                var response = await client.GetAsync(DiscoveryUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(json);
                var instances = data["value"] as JArray;

                if (instances != null)
                {
                    foreach (var instance in instances)
                    {
                        var state = instance["State"]?.ToString();
                        if (state != "0" && !string.Equals(state, "Enabled", StringComparison.OrdinalIgnoreCase))
                            continue;

                        environments.Add(new EnvironmentInfo
                        {
                            Id = instance["Id"]?.ToString(),
                            FriendlyName = instance["FriendlyName"]?.ToString(),
                            Url = instance["Url"]?.ToString(),
                            ApiUrl = instance["ApiUrl"]?.ToString(),
                            State = state,
                            Purpose = instance["Purpose"]?.ToString(),
                            Region = instance["Region"]?.ToString(),
                            Version = instance["Version"]?.ToString()
                        });
                    }
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

            var serviceUri = new Uri(envUrl + "/XRMServices/2011/Organization.svc/web");
            var proxy = new OrganizationWebProxyClient(serviceUri, false);
            proxy.HeaderToken = authResult.AccessToken;

            _serviceClient = new CrmServiceClient(proxy);

            if (!_serviceClient.IsReady)
            {
                var error = _serviceClient.LastCrmError;
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

    internal static class EnumerableExtensions
    {
        public static T FirstOrDefault<T>(this IEnumerable<T> source)
        {
            foreach (var item in source)
                return item;
            return default(T);
        }
    }
}
