using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;

namespace Dynamics365UserManager
{
    public class MainForm : MaterialForm
    {
        private readonly ConnectionManager _connection = new ConnectionManager();
        private readonly MaterialSkinManager _skin;

        // Connection
        private MaterialButton btnConnect, btnDisconnect, btnResetLogin;
        private MaterialLabel lblStatus;
        private MaterialProgressBar progressBar;

        // Tabs
        private MaterialTabControl tabControl;
        private MaterialTabSelector tabSelector;

        // Tab 1
        private MaterialTextBox2 txtBUSearch;
        private MaterialButton btnBUSearch;
        private MaterialListView lvUsers, lvRoles;
        private MaterialComboBox cbBusinessUnits;
        private MaterialButton btnChangeBU;

        // Tab 2
        private MaterialTextBox2 txtCloneSource, txtCloneTarget;
        private MaterialButton btnCloneSearchSource, btnCloneSearchTarget;
        private MaterialLabel lblSourceInfo, lblTargetInfo;
        private MaterialCheckbox chkCloneBU, chkCloneRoles, chkCloneTeams;
        private CheckedListBox clbTeams;
        private MaterialButton btnSelectAll, btnDeselectAll, btnClone;
        private UserInfo _cloneSource, _cloneTarget;

        // Tab 3
        private MaterialTextBox2 txtReassignOld, txtReassignNew;
        private MaterialButton btnReassignSearchOld, btnReassignSearchNew;
        private MaterialLabel lblOldOwner, lblNewOwner;
        private MaterialCheckbox chkAccount, chkContact, chkOpportunity, chkQuote, chkOrder, chkLead, chkCase;
        private MaterialLabel lblCounts;
        private MaterialButton btnCountRecords, btnReassign;
        private UserInfo _reassignOld, _reassignNew;

        // Log
        private RichTextBox rtbLog;

        private bool _busy;

        public MainForm()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // Material skin setup
            _skin = MaterialSkinManager.Instance;
            _skin.AddFormToManage(this);
            _skin.Theme = MaterialSkinManager.Themes.DARK;
            _skin.ColorScheme = new ColorScheme(
                Primary.Blue600, Primary.Blue900,
                Primary.Blue200, Accent.LightBlue400,
                TextShade.WHITE);

            Text = "Dynamics 365 User Manager";
            Size = new Size(1050, 780);
            MinimumSize = new Size(1050, 780);
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            Sizable = false;

            BuildUI();
        }

        private void BuildUI()
        {
            int top = 64; // under MaterialForm title bar

            // ── Connection row ──
            btnConnect = MBtn("CONNETTI", new Point(14, top + 6), 100, true);
            btnConnect.Click += async (s, e) => await ConnectAsync();
            Controls.Add(btnConnect);

            btnDisconnect = MBtn("DISCONNETTI", new Point(120, top + 6), 120);
            btnDisconnect.Enabled = false;
            btnDisconnect.Click += (s, e) => Disconnect();
            Controls.Add(btnDisconnect);

            btnResetLogin = MBtn("RESET LOGIN", new Point(246, top + 6), 120);
            btnResetLogin.Click += (s, e) => { _connection.ResetLogin(); Log("Cache login cancellata."); };
            Controls.Add(btnResetLogin);

            progressBar = new MaterialProgressBar
            {
                Location = new Point(380, top + 18),
                Size = new Size(120, 5),
                Visible = false
            };
            Controls.Add(progressBar);

            lblStatus = new MaterialLabel
            {
                Text = "Disconnesso",
                Location = new Point(510, top + 10),
                Size = new Size(500, 24),
                ForeColor = Color.Gray
            };
            Controls.Add(lblStatus);

            top += 48;

            // ── Tab selector ──
            tabSelector = new MaterialTabSelector
            {
                Location = new Point(0, top),
                Size = new Size(Width, 48),
                BaseTabControl = null // set after tabControl created
            };

            top += 48;

            // ── Tab control ──
            tabControl = new MaterialTabControl
            {
                Location = new Point(6, top),
                Size = new Size(Width - 22, 380)
            };
            tabControl.TabPages.Add(CreateTabChangeBU());
            tabControl.TabPages.Add(CreateTabClone());
            tabControl.TabPages.Add(CreateTabReassign());
            tabControl.Enabled = false;

            tabSelector.BaseTabControl = tabControl;
            Controls.Add(tabSelector);
            Controls.Add(tabControl);

            top += 386;

            // ── Log ──
            var btnClear = MBtn("CANCELLA LOG", new Point(6, top), 130);
            btnClear.Type = MaterialButton.MaterialButtonType.Outlined;
            btnClear.Click += (s, e) => rtbLog.Clear();
            Controls.Add(btnClear);

            var lblCredits = new LinkLabel
            {
                Text = "Creato da Antonio Colamartino",
                Location = new Point(Width - 230, top + 6),
                Size = new Size(210, 20),
                Font = new Font("Segoe UI", 8.5f),
                LinkColor = Color.FromArgb(100, 180, 255),
                VisitedLinkColor = Color.FromArgb(100, 180, 255),
                ActiveLinkColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight
            };
            lblCredits.LinkClicked += (s, e) => Process.Start("https://antoniocolamartino.it");
            Controls.Add(lblCredits);

            top += 38;

            rtbLog = new RichTextBox
            {
                Location = new Point(6, top),
                Size = new Size(Width - 22, Height - top - 14),
                ReadOnly = true,
                BackColor = Color.FromArgb(25, 25, 25),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None
            };
            Controls.Add(rtbLog);
        }

        // ─────────── Tab 1 – Cambio BU ───────────

        private TabPage CreateTabChangeBU()
        {
            var t = new TabPage("Cambio BU") { BackColor = Color.FromArgb(48, 48, 48) };
            int y = 12;

            txtBUSearch = new MaterialTextBox2 { Hint = "Cerca utente per nome o email...", Location = new Point(12, y), Size = new Size(340, 48) };
            t.Controls.Add(txtBUSearch);

            btnBUSearch = MBtn("CERCA", new Point(362, y + 8), 80);
            btnBUSearch.Click += async (s, e) => await SearchUsersForBUAsync();
            t.Controls.Add(btnBUSearch);
            txtBUSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { btnBUSearch.PerformClick(); e.SuppressKeyPress = true; } };

            y += 60;
            t.Controls.Add(new MaterialLabel { Text = "Utenti trovati:", Location = new Point(12, y), AutoSize = true, FontType = MaterialSkinManager.fontType.Subtitle2 });
            t.Controls.Add(new MaterialLabel { Text = "Ruoli correnti:", Location = new Point(510, y), AutoSize = true, FontType = MaterialSkinManager.fontType.Subtitle2 });

            y += 24;
            lvUsers = new MaterialListView
            {
                Location = new Point(12, y), Size = new Size(485, 180),
                View = View.Details, FullRowSelect = true, GridLines = false, MultiSelect = false
            };
            lvUsers.Columns.Add("Nome", 175);
            lvUsers.Columns.Add("Email", 170);
            lvUsers.Columns.Add("Business Unit", 130);
            lvUsers.SelectedIndexChanged += async (s, e) => await OnUserSelectedAsync();
            t.Controls.Add(lvUsers);

            lvRoles = new MaterialListView
            {
                Location = new Point(510, y), Size = new Size(480, 180),
                View = View.Details, FullRowSelect = true, GridLines = false
            };
            lvRoles.Columns.Add("Nome Ruolo", 460);
            t.Controls.Add(lvRoles);

            y += 190;
            var cbLabel = new MaterialLabel { Text = "Nuova Business Unit:", Location = new Point(12, y + 12), AutoSize = true };
            t.Controls.Add(cbLabel);

            cbBusinessUnits = new MaterialComboBox { Location = new Point(180, y), Size = new Size(320, 50), DropDownStyle = ComboBoxStyle.DropDownList };
            t.Controls.Add(cbBusinessUnits);

            btnChangeBU = MBtn("CAMBIA BU", new Point(520, y + 6), 130, true);
            btnChangeBU.Click += async (s, e) => await ChangeBUAsync();
            t.Controls.Add(btnChangeBU);

            return t;
        }

        private async Task SearchUsersForBUAsync()
        {
            if (string.IsNullOrWhiteSpace(txtBUSearch.Text)) return;
            await RunAsync(() =>
            {
                var users = DynamicsOperations.SearchUsers(_connection.ServiceClient, txtBUSearch.Text);
                Invoke((Action)(() =>
                {
                    lvUsers.Items.Clear(); lvRoles.Items.Clear();
                    foreach (var u in users)
                    {
                        var it = new ListViewItem(u.FullName);
                        it.SubItems.Add(u.Email);
                        it.SubItems.Add(u.BusinessUnitName);
                        it.Tag = u;
                        lvUsers.Items.Add(it);
                    }
                    Log($"Trovati {users.Count} utenti.");
                }));
            });
        }

        private async Task OnUserSelectedAsync()
        {
            if (lvUsers.SelectedItems.Count == 0) return;
            var user = (UserInfo)lvUsers.SelectedItems[0].Tag;
            await RunAsync(() =>
            {
                var roles = DynamicsOperations.GetUserRoles(_connection.ServiceClient, user.Id);
                Invoke((Action)(() => { lvRoles.Items.Clear(); foreach (var r in roles) lvRoles.Items.Add(new ListViewItem(r.Name)); }));
            });
        }

        private async Task ChangeBUAsync()
        {
            if (lvUsers.SelectedItems.Count == 0) { ShowMsg("Selezionare un utente."); return; }
            if (cbBusinessUnits.SelectedItem == null) { ShowMsg("Selezionare una Business Unit."); return; }
            var user = (UserInfo)lvUsers.SelectedItems[0].Tag;
            var bu = (BusinessUnitInfo)cbBusinessUnits.SelectedItem;
            if (Ask($"Cambiare la BU di {user.FullName} a {bu.Name}?") != DialogResult.Yes) return;
            await RunAsync(() =>
            {
                var r = DynamicsOperations.ChangeBusinessUnit(_connection.ServiceClient, user.Id, bu.Id, s => Log(s));
                Invoke((Action)(() => { Log(r.Message); ShowMsg(r.Message, r.Success); }));
            });
        }

        // ─────────── Tab 2 – Clone User ───────────

        private TabPage CreateTabClone()
        {
            var t = new TabPage("Clone User") { BackColor = Color.FromArgb(48, 48, 48) };
            int y = 12;

            txtCloneSource = new MaterialTextBox2 { Hint = "Email sorgente", Location = new Point(12, y), Size = new Size(310, 48) };
            t.Controls.Add(txtCloneSource);
            btnCloneSearchSource = MBtn("CERCA", new Point(332, y + 8), 80);
            btnCloneSearchSource.Click += async (s, e) => { _cloneSource = await FindUserAsync(txtCloneSource.Text); SetInfo(lblSourceInfo, _cloneSource); };
            t.Controls.Add(btnCloneSearchSource);
            lblSourceInfo = new MaterialLabel { Text = "", Location = new Point(420, y + 12), Size = new Size(560, 24) };
            t.Controls.Add(lblSourceInfo);

            y += 56;
            txtCloneTarget = new MaterialTextBox2 { Hint = "Email target", Location = new Point(12, y), Size = new Size(310, 48) };
            t.Controls.Add(txtCloneTarget);
            btnCloneSearchTarget = MBtn("CERCA", new Point(332, y + 8), 80);
            btnCloneSearchTarget.Click += async (s, e) => { _cloneTarget = await FindUserAsync(txtCloneTarget.Text); SetInfo(lblTargetInfo, _cloneTarget); };
            t.Controls.Add(btnCloneSearchTarget);
            lblTargetInfo = new MaterialLabel { Text = "", Location = new Point(420, y + 12), Size = new Size(560, 24) };
            t.Controls.Add(lblTargetInfo);

            y += 64;
            t.Controls.Add(new MaterialLabel { Text = "Opzioni:", Location = new Point(12, y), AutoSize = true, FontType = MaterialSkinManager.fontType.Subtitle2 });
            y += 24;
            chkCloneBU = new MaterialCheckbox { Text = "Business Unit", Location = new Point(8, y), Checked = true, AutoSize = true };
            chkCloneRoles = new MaterialCheckbox { Text = "Security Roles", Location = new Point(8, y + 36), Checked = true, AutoSize = true };
            chkCloneTeams = new MaterialCheckbox { Text = "Teams", Location = new Point(8, y + 72), AutoSize = true };
            chkCloneTeams.CheckedChanged += async (s, e) => { if (chkCloneTeams.Checked && _cloneSource != null) await LoadTeamsAsync(); };
            t.Controls.AddRange(new Control[] { chkCloneBU, chkCloneRoles, chkCloneTeams });

            t.Controls.Add(new MaterialLabel { Text = "Teams sorgente:", Location = new Point(250, y - 24), AutoSize = true, FontType = MaterialSkinManager.fontType.Subtitle2 });
            clbTeams = new CheckedListBox { Location = new Point(250, y), Size = new Size(380, 108), Font = new Font("Segoe UI", 9f), BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White };
            t.Controls.Add(clbTeams);

            btnSelectAll = MBtn("TUTTI", new Point(645, y), 80);
            btnSelectAll.Click += (s, e) => { for (int i = 0; i < clbTeams.Items.Count; i++) clbTeams.SetItemChecked(i, true); };
            t.Controls.Add(btnSelectAll);
            btnDeselectAll = MBtn("NESSUNO", new Point(645, y + 38), 80);
            btnDeselectAll.Click += (s, e) => { for (int i = 0; i < clbTeams.Items.Count; i++) clbTeams.SetItemChecked(i, false); };
            t.Controls.Add(btnDeselectAll);

            btnClone = MBtn("CLONA CONFIGURAZIONE", new Point(12, y + 118), 210, true);
            btnClone.Click += async (s, e) => await CloneUserAsync();
            t.Controls.Add(btnClone);

            return t;
        }

        private async Task LoadTeamsAsync()
        {
            if (_cloneSource == null) return;
            await RunAsync(() =>
            {
                var teams = DynamicsOperations.GetUserTeams(_connection.ServiceClient, _cloneSource.Id);
                Invoke((Action)(() => { clbTeams.Items.Clear(); foreach (var te in teams.Where(x => !x.IsDefault)) clbTeams.Items.Add(te, true); }));
            });
        }

        private async Task CloneUserAsync()
        {
            if (_cloneSource == null || _cloneTarget == null) { ShowMsg("Cercare entrambi gli utenti."); return; }
            if (Ask($"Clonare configurazione da {_cloneSource.FullName} a {_cloneTarget.FullName}?") != DialogResult.Yes) return;
            var sel = new List<TeamInfo>();
            if (chkCloneTeams.Checked) foreach (var item in clbTeams.CheckedItems) sel.Add((TeamInfo)item);
            await RunAsync(() =>
            {
                var r = DynamicsOperations.CloneUser(_connection.ServiceClient, _cloneSource, _cloneTarget, chkCloneBU.Checked, chkCloneRoles.Checked, chkCloneTeams.Checked, sel, s => Log(s));
                Invoke((Action)(() => { Log(r.Message); ShowMsg(r.Message, r.Success); }));
            });
        }

        // ─────────── Tab 3 – Reassign Records ───────────

        private TabPage CreateTabReassign()
        {
            var t = new TabPage("Reassign") { BackColor = Color.FromArgb(48, 48, 48) };
            int y = 12;

            txtReassignOld = new MaterialTextBox2 { Hint = "Email vecchio owner", Location = new Point(12, y), Size = new Size(310, 48) };
            t.Controls.Add(txtReassignOld);
            btnReassignSearchOld = MBtn("CERCA", new Point(332, y + 8), 80);
            btnReassignSearchOld.Click += async (s, e) => { _reassignOld = await FindUserAsync(txtReassignOld.Text); SetInfo(lblOldOwner, _reassignOld); };
            t.Controls.Add(btnReassignSearchOld);
            lblOldOwner = new MaterialLabel { Text = "", Location = new Point(420, y + 12), Size = new Size(560, 24) };
            t.Controls.Add(lblOldOwner);

            y += 56;
            txtReassignNew = new MaterialTextBox2 { Hint = "Email nuovo owner", Location = new Point(12, y), Size = new Size(310, 48) };
            t.Controls.Add(txtReassignNew);
            btnReassignSearchNew = MBtn("CERCA", new Point(332, y + 8), 80);
            btnReassignSearchNew.Click += async (s, e) => { _reassignNew = await FindUserAsync(txtReassignNew.Text); SetInfo(lblNewOwner, _reassignNew); };
            t.Controls.Add(btnReassignSearchNew);
            lblNewOwner = new MaterialLabel { Text = "", Location = new Point(420, y + 12), Size = new Size(560, 24) };
            t.Controls.Add(lblNewOwner);

            y += 64;
            t.Controls.Add(new MaterialLabel { Text = "Tipi di record:", Location = new Point(12, y), AutoSize = true, FontType = MaterialSkinManager.fontType.Subtitle2 });
            y += 24;
            chkAccount = MCk("Account", 8, y, true);         chkContact = MCk("Contact", 8, y + 32, true);
            chkOpportunity = MCk("Opportunity", 8, y + 64, true); chkQuote = MCk("Quote", 8, y + 96, true);
            chkOrder = MCk("Sales Order", 160, y, true);      chkLead = MCk("Lead", 160, y + 32, true);
            chkCase = MCk("Case", 160, y + 64, true);
            t.Controls.AddRange(new Control[] { chkAccount, chkContact, chkOpportunity, chkQuote, chkOrder, chkLead, chkCase });

            t.Controls.Add(new MaterialLabel { Text = "Anteprima:", Location = new Point(360, y - 24), AutoSize = true, FontType = MaterialSkinManager.fontType.Subtitle2 });
            lblCounts = new MaterialLabel { Text = "", Location = new Point(360, y), Size = new Size(320, 128) };
            t.Controls.Add(lblCounts);

            btnCountRecords = MBtn("CONTA RECORD", new Point(12, y + 132), 150);
            btnCountRecords.Click += async (s, e) => await CountRecordsAsync();
            t.Controls.Add(btnCountRecords);

            btnReassign = MBtn("TRASFERISCI", new Point(172, y + 132), 150, true);
            btnReassign.HighEmphasis = true;
            btnReassign.Click += async (s, e) => await ReassignRecordsAsync();
            t.Controls.Add(btnReassign);

            return t;
        }

        private async Task CountRecordsAsync()
        {
            if (_reassignOld == null) { ShowMsg("Cercare il vecchio owner."); return; }
            await RunAsync(() =>
            {
                var c = DynamicsOperations.CountRecords(_connection.ServiceClient, _reassignOld.Id);
                Invoke((Action)(() =>
                {
                    lblCounts.Text = $"Account: {Fmt(c.AccountCount)}   Contact: {Fmt(c.ContactCount)}\nOpportunity: {Fmt(c.OpportunityCount)}   Quote: {Fmt(c.QuoteCount)}\nSales Order: {Fmt(c.OrderCount)}   Lead: {Fmt(c.LeadCount)}\nCase: {Fmt(c.CaseCount)}";
                }));
            });
        }

        private static string Fmt(int n) => n >= 0 ? n.ToString() : "N/A";

        private async Task ReassignRecordsAsync()
        {
            if (_reassignOld == null || _reassignNew == null) { ShowMsg("Cercare entrambi gli utenti."); return; }
            if (Ask($"Trasferire i record da {_reassignOld.FullName} a {_reassignNew.FullName}?\n\nOperazione irreversibile.") != DialogResult.Yes) return;
            if (Ask("Sei sicuro? Questa operazione non puo' essere annullata.") != DialogResult.Yes) return;
            await RunAsync(() =>
            {
                var r = DynamicsOperations.ReassignRecords(_connection.ServiceClient, _reassignOld.Id, _reassignNew.Id,
                    chkAccount.Checked, chkContact.Checked, chkOpportunity.Checked, chkQuote.Checked, chkOrder.Checked, chkLead.Checked, chkCase.Checked, s => Log(s));
                Invoke((Action)(() => { Log(r.Message); ShowMsg(r.Message, r.Success); }));
            });
        }

        // ─────────── Connection ───────────

        private async Task ConnectAsync()
        {
            try
            {
                SetBusy(true); btnConnect.Enabled = false;
                lblStatus.Text = "Autenticazione..."; lblStatus.ForeColor = Color.Orange;
                Log("Avvio autenticazione...");
                await _connection.AuthenticateAsync();
                Log("Autenticazione riuscita. Recupero ambienti...");
                var envs = await _connection.GetAvailableEnvironmentsAsync();
                Log($"Trovati {envs.Count} ambienti.");
                if (envs.Count == 0) { ShowMsg("Nessun ambiente disponibile."); lblStatus.Text = "Disconnesso"; lblStatus.ForeColor = Color.Gray; btnConnect.Enabled = true; SetBusy(false); return; }
                SetBusy(false);
                using (var sel = new EnvironmentSelector(envs))
                {
                    if (sel.ShowDialog(this) != DialogResult.OK) { lblStatus.Text = "Disconnesso"; lblStatus.ForeColor = Color.Gray; btnConnect.Enabled = true; return; }
                    SetBusy(true);
                    Log($"Connessione a {sel.SelectedEnvironment.FriendlyName}...");
                    lblStatus.Text = $"Connessione a {sel.SelectedEnvironment.FriendlyName}...";
                    await _connection.ConnectToEnvironmentAsync(sel.SelectedEnvironment);
                }
                var env = _connection.CurrentEnvironment;
                lblStatus.Text = $"Connesso: {env.FriendlyName} (v{env.Version})";
                lblStatus.ForeColor = Color.FromArgb(100, 180, 255);
                btnConnect.Enabled = false; btnDisconnect.Enabled = true; tabControl.Enabled = true;
                Log($"Connesso a {env.FriendlyName}.");
                await RunAsync(() =>
                {
                    var bus = DynamicsOperations.GetAllBusinessUnits(_connection.ServiceClient);
                    Invoke((Action)(() => { cbBusinessUnits.DataSource = bus; cbBusinessUnits.DisplayMember = "Name"; }));
                });
            }
            catch (Exception ex) { Log($"Errore: {ex.Message}"); ShowMsg(ex.Message); lblStatus.Text = "Disconnesso"; lblStatus.ForeColor = Color.Gray; btnConnect.Enabled = true; }
            finally { SetBusy(false); }
        }

        private void Disconnect()
        {
            _connection.Disconnect();
            lblStatus.Text = "Disconnesso"; lblStatus.ForeColor = Color.Gray;
            btnConnect.Enabled = true; btnDisconnect.Enabled = false; tabControl.Enabled = false;
            Log("Disconnesso.");
        }

        // ─────────── Helpers ───────────

        private MaterialButton MBtn(string text, Point loc, int width, bool raised = false)
        {
            return new MaterialButton
            {
                Text = text, Location = loc, Size = new Size(width, 36), AutoSize = false,
                Type = raised ? MaterialButton.MaterialButtonType.Contained : MaterialButton.MaterialButtonType.Outlined
            };
        }

        private MaterialCheckbox MCk(string text, int x, int y, bool chk)
        {
            return new MaterialCheckbox { Text = text, Location = new Point(x, y), Checked = chk, AutoSize = true };
        }

        private void SetInfo(MaterialLabel lbl, UserInfo u)
        {
            if (u != null) { lbl.Text = $"{u.FullName} ({u.BusinessUnitName})"; lbl.ForeColor = Color.FromArgb(100, 180, 255); }
            else { lbl.Text = "Non trovato"; lbl.ForeColor = Color.Red; }
        }

        private async Task<UserInfo> FindUserAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            UserInfo user = null;
            await RunAsync(() =>
            {
                user = DynamicsOperations.SearchUserByEmail(_connection.ServiceClient, email.Trim());
                Invoke((Action)(() => { if (user != null) Log($"Trovato: {user.FullName} ({user.BusinessUnitName})"); else Log($"Utente non trovato: {email}"); }));
            });
            return user;
        }

        private void SetBusy(bool busy) { _busy = busy; progressBar.Visible = busy; Cursor = busy ? Cursors.WaitCursor : Cursors.Default; }

        private async Task RunAsync(Action action)
        {
            SetBusy(true);
            try { await Task.Run(action); }
            catch (Exception ex) { Log($"Errore: {ex.Message}"); ShowMsg(ex.Message); }
            finally { SetBusy(false); }
        }

        private void ShowMsg(string msg, bool success = false)
        {
            MessageBox.Show(msg, success ? "Successo" : "Info", MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private DialogResult Ask(string msg)
        {
            return MessageBox.Show(msg, "Conferma", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }

        private void Log(string msg)
        {
            if (InvokeRequired) { Invoke((Action<string>)Log, msg); return; }
            rtbLog.SelectionStart = rtbLog.TextLength; rtbLog.SelectionLength = 0;
            rtbLog.SelectionColor = Color.FromArgb(80, 140, 220);
            rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] ");
            Color c = Color.LightGray;
            if (msg.StartsWith("Errore", StringComparison.OrdinalIgnoreCase) || msg.Contains("fallita") || msg.Contains("fallito")) c = Color.FromArgb(255, 100, 100);
            else if (msg.Contains("riuscit") || msg.Contains("Successo") || msg.Contains("completat") || msg.Contains("Connesso")) c = Color.FromArgb(80, 200, 120);
            rtbLog.SelectionStart = rtbLog.TextLength; rtbLog.SelectionLength = 0;
            rtbLog.SelectionColor = c;
            rtbLog.AppendText(msg + "\n"); rtbLog.ScrollToCaret();
        }

        protected override void OnFormClosing(FormClosingEventArgs e) { _connection.Dispose(); base.OnFormClosing(e); }
    }
}
