using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dynamics365UserManager
{
    public class MainForm : Form
    {
        private readonly ConnectionManager _connection = new ConnectionManager();

        // Connection
        private Button btnConnect, btnDisconnect, btnResetLogin;
        private Label lblStatus;
        private ProgressBar progressBar;

        // Tabs
        private TabControl tabControl;

        // Tab 1
        private Label _lblRolesHeader;
        private TextBox txtBUSearch;
        private Button btnBUSearch;
        private ListView lvUsers, lvRoles;
        private ComboBox cbBusinessUnits;
        private Button btnChangeBU;

        // Tab 2
        private TextBox txtCloneSource, txtCloneTarget;
        private Button btnCloneSearchSource, btnCloneSearchTarget;
        private Label lblSourceInfo, lblTargetInfo;
        private CheckBox chkCloneBU, chkCloneRoles, chkCloneTeams;
        private CheckedListBox clbTeams;
        private Button btnSelectAll, btnDeselectAll, btnClone;
        private UserInfo _cloneSource, _cloneTarget;

        // Tab 3
        private TextBox txtReassignOld, txtReassignNew;
        private Button btnReassignSearchOld, btnReassignSearchNew;
        private Label lblOldOwner, lblNewOwner;
        private CheckBox chkAccount, chkContact, chkOpportunity, chkQuote, chkOrder, chkLead, chkCase;
        private Label lblCounts;
        private Button btnCountRecords, btnReassign;
        private UserInfo _reassignOld, _reassignNew;

        // Tab 4
        private TextBox txtRoleSearch;
        private Button btnRoleSearch;
        private ListView lvRolesList, lvRoleUsers;
        private TextBox txtRoleUserSearch;
        private Button btnRoleUserSearch, btnAssignRole, btnRemoveRole;
        private ListView lvRoleUserResults;
        private Label lblRoleAssignHeader;

        // Tab 5
        private TextBox txtTeamSearch;
        private Button btnTeamSearch;
        private ListView lvTeamsList, lvTeamMembers;
        private TextBox txtTeamUserSearch;
        private Button btnTeamUserSearch, btnAddToTeam, btnRemoveFromTeam;
        private ListView lvTeamUserResults;
        private Label lblTeamAddHeader;

        // Log
        private RichTextBox rtbLog;
        private LinkLabel lblCredits;

        private bool _busy;

        public MainForm()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            Text = "Dynamics 365 User Manager";
            Size = new Size(1050, 780);
            MinimumSize = new Size(1050, 780);
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = true;
            FormBorderStyle = FormBorderStyle.Sizable;

            BuildUI();
        }

        private void BuildUI()
        {
            int top = 8;

            // ── Connection row ──
            btnConnect = MBtn("CONNETTI", new Point(14, top + 6), 100, true);
            btnConnect.Click += async (s, e) => await ConnectAsync();
            Controls.Add(btnConnect);

            btnDisconnect = MBtn("DISCONNETTI", new Point(btnConnect.Right + 6, top + 6), 120);
            btnDisconnect.Enabled = false;
            btnDisconnect.Click += (s, e) => Disconnect();
            Controls.Add(btnDisconnect);

            btnResetLogin = MBtn("CANCELLA CREDENZIALI", new Point(btnDisconnect.Right + 6, top + 6), 160);
            btnResetLogin.Click += (s, e) => { _connection.ResetLogin(); Log("Cache login cancellata."); };
            Controls.Add(btnResetLogin);

            progressBar = new ProgressBar
            {
                Location = new Point(btnResetLogin.Right + 12, top + 18),
                Size = new Size(120, 5),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };
            Controls.Add(progressBar);

            lblStatus = new Label
            {
                Text = "Disconnesso",
                Location = new Point(progressBar.Right + 12, top + 10),
                Size = new Size(ClientSize.Width - progressBar.Right - 20, 24),
                ForeColor = Color.Gray,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9f),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(lblStatus);

            top += 48;

            // ── Tab control ──
            int logAreaHeight = 190;
            tabControl = new TabControl
            {
                Location = new Point(6, top),
                Size = new Size(ClientSize.Width - 12, ClientSize.Height - top - logAreaHeight),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Font = new Font("Segoe UI", 9f)
            };
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += TabControl_DrawItem;
            tabControl.TabPages.Add(CreateTabChangeBU());
            tabControl.TabPages.Add(CreateTabClone());
            tabControl.TabPages.Add(CreateTabReassign());
            tabControl.TabPages.Add(CreateTabSecurityRoles());
            tabControl.TabPages.Add(CreateTabTeams());
            tabControl.Enabled = false;
            tabControl.Resize += (s, e) => OnTabControlResized();
            Controls.Add(tabControl);

            // ── Log ──
            var btnClear = MBtn("PULISCI LOG", new Point(6, 0), 130);
            btnClear.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            btnClear.Click += (s, e) => rtbLog.Clear();
            Controls.Add(btnClear);

            lblCredits = new LinkLabel
            {
                Text = "Creato da Antonio Colamartino",
                Size = new Size(210, 20),
                Font = new Font("Segoe UI", 8.5f),
                LinkColor = AppTheme.Link,
                VisitedLinkColor = AppTheme.Link,
                ActiveLinkColor = AppTheme.LinkActive,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom
            };
            lblCredits.LinkClicked += (s, e) => Process.Start("https://antoniocolamartino.it");
            Controls.Add(lblCredits);

            rtbLog = new RichTextBox
            {
                Size = new Size(ClientSize.Width - 12, 150),
                ReadOnly = true,
                BackColor = AppTheme.LogBg,
                ForeColor = AppTheme.LogFg,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            Controls.Add(rtbLog);

            PositionBottomControls(btnClear, lblCredits);
            Resize += (s, e) => PositionBottomControls(btnClear, lblCredits);
        }

        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tc = (TabControl)sender;
            var tab = tc.TabPages[e.Index];
            bool selected = (e.Index == tc.SelectedIndex);
            var bg = selected ? AppTheme.TabActiveBg : AppTheme.TabBg;
            using (var brush = new SolidBrush(bg))
                e.Graphics.FillRectangle(brush, e.Bounds);
            var fg = selected ? (AppTheme.IsDark ? Color.White : AppTheme.AccentBlue) : AppTheme.FgSecondary;
            using (var brush = new SolidBrush(fg))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(tab.Text, tc.Font, brush, e.Bounds, sf);
            }
            if (selected)
            {
                using (var pen = new Pen(AppTheme.AccentBlue, 2))
                    e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }
        }

        private void PositionBottomControls(Control btnClear, Control lblCredits)
        {
            int logTop = ClientSize.Height - 150 - 8;
            int barTop = logTop - 38;
            btnClear.Location = new Point(6, barTop);
            lblCredits.Location = new Point(ClientSize.Width - 220, barTop + 6);
            rtbLog.Location = new Point(6, logTop);
            rtbLog.Size = new Size(ClientSize.Width - 12, ClientSize.Height - logTop - 8);
        }

        private void OnTabControlResized()
        {
            int tw = tabControl.ClientSize.Width;
            int th = tabControl.ClientSize.Height;

            if (lvUsers != null)
            {
                int half = (tw - 36) / 2;
                lvUsers.Size = new Size(half, th - lvUsers.Top - 60);
                _lblRolesHeader.Location = new Point(half + 24, _lblRolesHeader.Location.Y);
                lvRoles.Location = new Point(half + 24, lvRoles.Location.Y);
                lvRoles.Size = new Size(tw - half - 36, th - lvRoles.Top - 60);
                cbBusinessUnits.Location = new Point(cbBusinessUnits.Location.X, th - 50);
                btnChangeBU.Location = new Point(half + 24, th - 44);
            }

            if (lvRolesList != null)
            {
                int leftW = (tw - 36) / 3;
                int rightW = tw - leftW - 36;
                int topListsTop = lvRolesList.Top;
                int listH = (th - topListsTop - 130) / 2;
                if (listH < 60) listH = 60;
                lvRolesList.Size = new Size(leftW, listH);
                lvRoleUsers.Location = new Point(leftW + 24, topListsTop);
                lvRoleUsers.Size = new Size(rightW, listH);
                int y4 = topListsTop + listH + 8;
                btnRemoveRole.Location = new Point(leftW + 24, y4);
                y4 += 6;
                lblRoleAssignHeader.Location = new Point(12, y4);
                y4 += 24;
                txtRoleUserSearch.Location = new Point(12, y4);
                btnRoleUserSearch.Location = new Point(362, y4 + 2);
                btnAssignRole.Location = new Point(460, y4 + 2);
                y4 += 40;
                lvRoleUserResults.Location = new Point(12, y4);
                lvRoleUserResults.Size = new Size(tw - 24, th - y4 - 8);
            }

            if (lvTeamsList != null)
            {
                int leftW = (tw - 36) / 3;
                int rightW = tw - leftW - 36;
                int topListsTop = lvTeamsList.Top;
                int listH = (th - topListsTop - 130) / 2;
                if (listH < 60) listH = 60;
                lvTeamsList.Size = new Size(leftW, listH);
                lvTeamMembers.Location = new Point(leftW + 24, topListsTop);
                lvTeamMembers.Size = new Size(rightW, listH);
                int y5 = topListsTop + listH + 8;
                btnRemoveFromTeam.Location = new Point(leftW + 24, y5);
                y5 += 6;
                lblTeamAddHeader.Location = new Point(12, y5);
                y5 += 24;
                txtTeamUserSearch.Location = new Point(12, y5);
                btnTeamUserSearch.Location = new Point(362, y5 + 2);
                btnAddToTeam.Location = new Point(460, y5 + 2);
                y5 += 40;
                lvTeamUserResults.Location = new Point(12, y5);
                lvTeamUserResults.Size = new Size(tw - 24, th - y5 - 8);
            }
        }

        // ─────────── Tab 1 – Cambio BU ───────────

        private TabPage CreateTabChangeBU()
        {
            var t = new TabPage("Cambio BU") { BackColor = AppTheme.ControlBg };
            int y = 12;

            txtBUSearch = MTxt("Cerca utente per nome o email...", new Point(12, y), 340);
            t.Controls.Add(txtBUSearch);

            btnBUSearch = MBtn("CERCA UTENTE", new Point(362, y + 2), 110);
            btnBUSearch.Click += async (s, e) => await SearchUsersForBUAsync();
            t.Controls.Add(btnBUSearch);
            txtBUSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { btnBUSearch.PerformClick(); e.SuppressKeyPress = true; } };

            y += 40;
            t.Controls.Add(MLbl("Utenti trovati:", new Point(12, y), true));
            _lblRolesHeader = MLbl("Ruoli correnti:", new Point(510, y), true);
            t.Controls.Add(_lblRolesHeader);

            y += 24;
            lvUsers = MLv(new Point(12, y), new Size(485, 180));
            lvUsers.Columns.Add("Nome", 175);
            lvUsers.Columns.Add("Email", 170);
            lvUsers.Columns.Add("Business Unit", 130);
            lvUsers.SelectedIndexChanged += async (s, e) => await OnUserSelectedAsync();
            t.Controls.Add(lvUsers);

            lvRoles = MLv(new Point(510, y), new Size(480, 180));
            lvRoles.Columns.Add("Nome Ruolo", 460);
            t.Controls.Add(lvRoles);

            y += 190;
            t.Controls.Add(MLbl("Nuova Business Unit:", new Point(12, y + 6)));

            cbBusinessUnits = new ComboBox
            {
                Location = new Point(180, y + 2), Size = new Size(320, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppTheme.InputBg, ForeColor = AppTheme.FgPrimary,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f)
            };
            t.Controls.Add(cbBusinessUnits);

            btnChangeBU = MBtn("ASSEGNA BU", new Point(520, y + 2), 130, true);
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
            var t = new TabPage("Clone User") { BackColor = AppTheme.ControlBg };
            int y = 12;

            txtCloneSource = MTxt("Email sorgente", new Point(12, y), 310);
            t.Controls.Add(txtCloneSource);
            btnCloneSearchSource = MBtn("CERCA SORGENTE", new Point(332, y + 2), 130);
            btnCloneSearchSource.Click += async (s, e) => { _cloneSource = await FindUserAsync(txtCloneSource.Text); SetInfo(lblSourceInfo, _cloneSource); };
            t.Controls.Add(btnCloneSearchSource);
            lblSourceInfo = MLbl("", new Point(470, y + 6));
            lblSourceInfo.Size = new Size(510, 24);
            t.Controls.Add(lblSourceInfo);

            y += 40;
            txtCloneTarget = MTxt("Email target", new Point(12, y), 310);
            t.Controls.Add(txtCloneTarget);
            btnCloneSearchTarget = MBtn("CERCA DESTINAZIONE", new Point(332, y + 2), 150);
            btnCloneSearchTarget.Click += async (s, e) => { _cloneTarget = await FindUserAsync(txtCloneTarget.Text); SetInfo(lblTargetInfo, _cloneTarget); };
            t.Controls.Add(btnCloneSearchTarget);
            lblTargetInfo = MLbl("", new Point(490, y + 6));
            lblTargetInfo.Size = new Size(560, 24);
            t.Controls.Add(lblTargetInfo);

            y += 48;
            t.Controls.Add(MLbl("Opzioni:", new Point(12, y), true));
            y += 24;
            chkCloneBU = MCk("Business Unit", 8, y, true);
            chkCloneRoles = MCk("Security Roles", 8, y + 28, true);
            chkCloneTeams = MCk("Teams", 8, y + 56);
            chkCloneTeams.CheckedChanged += async (s, e) => { if (chkCloneTeams.Checked && _cloneSource != null) await LoadTeamsAsync(); };
            t.Controls.AddRange(new Control[] { chkCloneBU, chkCloneRoles, chkCloneTeams });

            t.Controls.Add(MLbl("Teams sorgente:", new Point(250, y - 24), true));
            clbTeams = new CheckedListBox
            {
                Location = new Point(250, y), Size = new Size(380, 108),
                Font = new Font("Segoe UI", 9f),
                BackColor = AppTheme.CheckListBg, ForeColor = AppTheme.CheckListFg
            };
            t.Controls.Add(clbTeams);

            btnSelectAll = MBtn("SELEZIONA TUTTI", new Point(645, y), 130);
            btnSelectAll.Click += (s, e) => { for (int i = 0; i < clbTeams.Items.Count; i++) clbTeams.SetItemChecked(i, true); };
            t.Controls.Add(btnSelectAll);
            btnDeselectAll = MBtn("DESELEZIONA TUTTI", new Point(645, y + 38), 130);
            btnDeselectAll.Click += (s, e) => { for (int i = 0; i < clbTeams.Items.Count; i++) clbTeams.SetItemChecked(i, false); };
            t.Controls.Add(btnDeselectAll);

            btnClone = MBtn("AVVIA CLONAZIONE", new Point(12, y + 118), 180, true);
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
            var t = new TabPage("Reassign") { BackColor = AppTheme.ControlBg };
            int y = 12;

            txtReassignOld = MTxt("Email vecchio owner", new Point(12, y), 310);
            t.Controls.Add(txtReassignOld);
            btnReassignSearchOld = MBtn("CERCA VECCHIO", new Point(332, y + 2), 120);
            btnReassignSearchOld.Click += async (s, e) => { _reassignOld = await FindUserAsync(txtReassignOld.Text); SetInfo(lblOldOwner, _reassignOld); };
            t.Controls.Add(btnReassignSearchOld);
            lblOldOwner = MLbl("", new Point(460, y + 6));
            lblOldOwner.Size = new Size(560, 24);
            t.Controls.Add(lblOldOwner);

            y += 40;
            txtReassignNew = MTxt("Email nuovo owner", new Point(12, y), 310);
            t.Controls.Add(txtReassignNew);
            btnReassignSearchNew = MBtn("CERCA NUOVO", new Point(332, y + 2), 120);
            btnReassignSearchNew.Click += async (s, e) => { _reassignNew = await FindUserAsync(txtReassignNew.Text); SetInfo(lblNewOwner, _reassignNew); };
            t.Controls.Add(btnReassignSearchNew);
            lblNewOwner = MLbl("", new Point(460, y + 6));
            lblNewOwner.Size = new Size(560, 24);
            t.Controls.Add(lblNewOwner);

            y += 48;
            t.Controls.Add(MLbl("Tipi di record:", new Point(12, y), true));
            y += 24;
            chkAccount = MCk("Account", 8, y, true);         chkContact = MCk("Contact", 8, y + 28, true);
            chkOpportunity = MCk("Opportunity", 8, y + 56, true); chkQuote = MCk("Quote", 8, y + 84, true);
            chkOrder = MCk("Sales Order", 160, y, true);      chkLead = MCk("Lead", 160, y + 28, true);
            chkCase = MCk("Case", 160, y + 56, true);
            t.Controls.AddRange(new Control[] { chkAccount, chkContact, chkOpportunity, chkQuote, chkOrder, chkLead, chkCase });

            t.Controls.Add(MLbl("Anteprima:", new Point(360, y - 24), true));
            lblCounts = MLbl("", new Point(360, y));
            lblCounts.Size = new Size(320, 128);
            t.Controls.Add(lblCounts);

            btnCountRecords = MBtn("ANTEPRIMA CONTEGGIO", new Point(12, y + 118), 180);
            btnCountRecords.Click += async (s, e) => await CountRecordsAsync();
            t.Controls.Add(btnCountRecords);

            btnReassign = MBtn("TRASFERISCI RECORD", new Point(btnCountRecords.Right + 8, y + 118), 170, true);
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

        // ─────────── Tab 4 – Security Roles ───────────

        private TabPage CreateTabSecurityRoles()
        {
            var t = new TabPage("Security Roles") { BackColor = AppTheme.ControlBg };
            int y = 12;

            txtRoleSearch = MTxt("Cerca ruolo per nome...", new Point(12, y), 340);
            t.Controls.Add(txtRoleSearch);
            btnRoleSearch = MBtn("CERCA RUOLO", new Point(362, y + 2), 110);
            btnRoleSearch.Click += async (s, e) => await SearchRolesAsync();
            t.Controls.Add(btnRoleSearch);
            txtRoleSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { btnRoleSearch.PerformClick(); e.SuppressKeyPress = true; } };

            y += 40;
            t.Controls.Add(MLbl("Ruoli trovati:", new Point(12, y), true));
            t.Controls.Add(MLbl("Utenti con il ruolo selezionato:", new Point(360, y), true));

            y += 24;
            lvRolesList = MLv(new Point(12, y), new Size(335, 140));
            lvRolesList.Columns.Add("Nome", 230);
            lvRolesList.Columns.Add("BU", 95);
            lvRolesList.SelectedIndexChanged += async (s, e) => await OnRoleSelectedAsync();
            t.Controls.Add(lvRolesList);

            lvRoleUsers = MLv(new Point(360, y), new Size(630, 140));
            lvRoleUsers.MultiSelect = true;
            lvRoleUsers.Columns.Add("Nome", 200);
            lvRoleUsers.Columns.Add("Email", 220);
            lvRoleUsers.Columns.Add("Business Unit", 190);
            t.Controls.Add(lvRoleUsers);

            y += 148;
            btnRemoveRole = MBtn("RIMUOVI RUOLO", new Point(360, y), 140);
            btnRemoveRole.Click += async (s, e) => await RemoveRoleFromSelectedAsync();
            t.Controls.Add(btnRemoveRole);

            y += 6;
            lblRoleAssignHeader = MLbl("Assegna ruolo a utenti:", new Point(12, y), true);
            t.Controls.Add(lblRoleAssignHeader);

            y += 24;
            txtRoleUserSearch = MTxt("Cerca utente per nome o email...", new Point(12, y), 340);
            t.Controls.Add(txtRoleUserSearch);
            btnRoleUserSearch = MBtn("CERCA UTENTE", new Point(362, y + 2), 110);
            btnRoleUserSearch.Click += async (s, e) => await SearchUsersForRoleAsync();
            t.Controls.Add(btnRoleUserSearch);
            txtRoleUserSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { btnRoleUserSearch.PerformClick(); e.SuppressKeyPress = true; } };

            btnAssignRole = MBtn("ASSEGNA RUOLO", new Point(btnRoleUserSearch.Right + 8, y + 2), 140, true);
            btnAssignRole.Click += async (s, e) => await AssignRoleToSelectedAsync();
            t.Controls.Add(btnAssignRole);

            y += 40;
            lvRoleUserResults = MLv(new Point(12, y), new Size(978, 80));
            lvRoleUserResults.MultiSelect = true;
            lvRoleUserResults.Columns.Add("Nome", 250);
            lvRoleUserResults.Columns.Add("Email", 350);
            lvRoleUserResults.Columns.Add("Business Unit", 350);
            t.Controls.Add(lvRoleUserResults);

            return t;
        }

        private async Task SearchRolesAsync()
        {
            if (string.IsNullOrWhiteSpace(txtRoleSearch.Text)) return;
            await RunAsync(() =>
            {
                var roles = DynamicsOperations.SearchRoles(_connection.ServiceClient, txtRoleSearch.Text);
                Invoke((Action)(() =>
                {
                    lvRolesList.Items.Clear(); lvRoleUsers.Items.Clear();
                    foreach (var r in roles)
                    {
                        var it = new ListViewItem(r.Name);
                        it.SubItems.Add(r.BusinessUnitId.ToString().Substring(0, 8));
                        it.Tag = r;
                        lvRolesList.Items.Add(it);
                    }
                    Log($"Trovati {roles.Count} ruoli.");
                }));
            });
        }

        private async Task OnRoleSelectedAsync()
        {
            if (lvRolesList.SelectedItems.Count == 0) return;
            var role = (RoleInfo)lvRolesList.SelectedItems[0].Tag;
            await RunAsync(() =>
            {
                var users = DynamicsOperations.GetUsersWithRole(_connection.ServiceClient, role.Id);
                Invoke((Action)(() =>
                {
                    lvRoleUsers.Items.Clear();
                    foreach (var u in users)
                    {
                        var it = new ListViewItem(u.FullName);
                        it.SubItems.Add(u.Email);
                        it.SubItems.Add(u.BusinessUnitName);
                        it.Tag = u;
                        lvRoleUsers.Items.Add(it);
                    }
                    Log($"Ruolo '{role.Name}': {users.Count} utenti.");
                }));
            });
        }

        private async Task RemoveRoleFromSelectedAsync()
        {
            if (lvRolesList.SelectedItems.Count == 0) { ShowMsg("Selezionare un ruolo."); return; }
            if (lvRoleUsers.SelectedItems.Count == 0) { ShowMsg("Selezionare gli utenti da cui rimuovere il ruolo."); return; }
            var role = (RoleInfo)lvRolesList.SelectedItems[0].Tag;
            var userIds = new List<Guid>();
            foreach (ListViewItem item in lvRoleUsers.SelectedItems) userIds.Add(((UserInfo)item.Tag).Id);
            if (Ask($"Rimuovere il ruolo '{role.Name}' da {userIds.Count} utenti?") != DialogResult.Yes) return;
            await RunAsync(() =>
            {
                var r = DynamicsOperations.RemoveRoleFromUsers(_connection.ServiceClient, role.Id, role.Name, userIds, s => Log(s));
                Invoke((Action)(() => { Log(r.Message); ShowMsg(r.Message, r.Success); }));
            });
            await OnRoleSelectedAsync();
        }

        private async Task SearchUsersForRoleAsync()
        {
            if (string.IsNullOrWhiteSpace(txtRoleUserSearch.Text)) return;
            await RunAsync(() =>
            {
                var users = DynamicsOperations.SearchUsers(_connection.ServiceClient, txtRoleUserSearch.Text);
                Invoke((Action)(() =>
                {
                    lvRoleUserResults.Items.Clear();
                    foreach (var u in users)
                    {
                        var it = new ListViewItem(u.FullName);
                        it.SubItems.Add(u.Email);
                        it.SubItems.Add(u.BusinessUnitName);
                        it.Tag = u;
                        lvRoleUserResults.Items.Add(it);
                    }
                }));
            });
        }

        private async Task AssignRoleToSelectedAsync()
        {
            if (lvRolesList.SelectedItems.Count == 0) { ShowMsg("Selezionare un ruolo."); return; }
            if (lvRoleUserResults.SelectedItems.Count == 0) { ShowMsg("Selezionare gli utenti a cui assegnare il ruolo."); return; }
            var role = (RoleInfo)lvRolesList.SelectedItems[0].Tag;
            var userIds = new List<Guid>();
            foreach (ListViewItem item in lvRoleUserResults.SelectedItems) userIds.Add(((UserInfo)item.Tag).Id);
            if (Ask($"Assegnare il ruolo '{role.Name}' a {userIds.Count} utenti?") != DialogResult.Yes) return;
            await RunAsync(() =>
            {
                var r = DynamicsOperations.AssignRoleToUsers(_connection.ServiceClient, role.Id, role.Name, userIds, s => Log(s));
                Invoke((Action)(() => { Log(r.Message); ShowMsg(r.Message, r.Success); }));
            });
            await OnRoleSelectedAsync();
        }

        // ─────────── Tab 5 – Teams ───────────

        private TabPage CreateTabTeams()
        {
            var t = new TabPage("Teams") { BackColor = AppTheme.ControlBg };
            int y = 12;

            txtTeamSearch = MTxt("Cerca team per nome...", new Point(12, y), 340);
            t.Controls.Add(txtTeamSearch);
            btnTeamSearch = MBtn("CERCA TEAM", new Point(362, y + 2), 110);
            btnTeamSearch.Click += async (s, e) => await SearchTeamsAsync();
            t.Controls.Add(btnTeamSearch);
            txtTeamSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { btnTeamSearch.PerformClick(); e.SuppressKeyPress = true; } };

            y += 40;
            t.Controls.Add(MLbl("Teams trovati:", new Point(12, y), true));
            t.Controls.Add(MLbl("Membri del team selezionato:", new Point(360, y), true));

            y += 24;
            lvTeamsList = MLv(new Point(12, y), new Size(335, 140));
            lvTeamsList.Columns.Add("Nome", 195);
            lvTeamsList.Columns.Add("BU", 130);
            lvTeamsList.SelectedIndexChanged += async (s, e) => await OnTeamSelectedAsync();
            t.Controls.Add(lvTeamsList);

            lvTeamMembers = MLv(new Point(360, y), new Size(630, 140));
            lvTeamMembers.MultiSelect = true;
            lvTeamMembers.Columns.Add("Nome", 200);
            lvTeamMembers.Columns.Add("Email", 220);
            lvTeamMembers.Columns.Add("Business Unit", 190);
            t.Controls.Add(lvTeamMembers);

            y += 148;
            btnRemoveFromTeam = MBtn("RIMUOVI DAL TEAM", new Point(360, y), 160);
            btnRemoveFromTeam.Click += async (s, e) => await RemoveUsersFromTeamAsync();
            t.Controls.Add(btnRemoveFromTeam);

            y += 6;
            lblTeamAddHeader = MLbl("Aggiungi utenti al team:", new Point(12, y), true);
            t.Controls.Add(lblTeamAddHeader);

            y += 24;
            txtTeamUserSearch = MTxt("Cerca utente per nome o email...", new Point(12, y), 340);
            t.Controls.Add(txtTeamUserSearch);
            btnTeamUserSearch = MBtn("CERCA UTENTE", new Point(362, y + 2), 110);
            btnTeamUserSearch.Click += async (s, e) => await SearchUsersForTeamAsync();
            t.Controls.Add(btnTeamUserSearch);
            txtTeamUserSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { btnTeamUserSearch.PerformClick(); e.SuppressKeyPress = true; } };

            btnAddToTeam = MBtn("AGGIUNGI AL TEAM", new Point(btnTeamUserSearch.Right + 8, y + 2), 160, true);
            btnAddToTeam.Click += async (s, e) => await AddUsersToTeamAsync();
            t.Controls.Add(btnAddToTeam);

            y += 40;
            lvTeamUserResults = MLv(new Point(12, y), new Size(978, 80));
            lvTeamUserResults.MultiSelect = true;
            lvTeamUserResults.Columns.Add("Nome", 250);
            lvTeamUserResults.Columns.Add("Email", 350);
            lvTeamUserResults.Columns.Add("Business Unit", 350);
            t.Controls.Add(lvTeamUserResults);

            return t;
        }

        private async Task SearchTeamsAsync()
        {
            if (string.IsNullOrWhiteSpace(txtTeamSearch.Text)) return;
            await RunAsync(() =>
            {
                var teams = DynamicsOperations.SearchTeams(_connection.ServiceClient, txtTeamSearch.Text);
                Invoke((Action)(() =>
                {
                    lvTeamsList.Items.Clear(); lvTeamMembers.Items.Clear();
                    foreach (var te in teams)
                    {
                        var it = new ListViewItem(te.Name);
                        it.SubItems.Add(te.BusinessUnitName);
                        it.Tag = te;
                        lvTeamsList.Items.Add(it);
                    }
                    Log($"Trovati {teams.Count} teams.");
                }));
            });
        }

        private async Task OnTeamSelectedAsync()
        {
            if (lvTeamsList.SelectedItems.Count == 0) return;
            var team = (TeamInfo)lvTeamsList.SelectedItems[0].Tag;
            await RunAsync(() =>
            {
                var members = DynamicsOperations.GetTeamMembers(_connection.ServiceClient, team.Id);
                Invoke((Action)(() =>
                {
                    lvTeamMembers.Items.Clear();
                    foreach (var u in members)
                    {
                        var it = new ListViewItem(u.FullName);
                        it.SubItems.Add(u.Email);
                        it.SubItems.Add(u.BusinessUnitName);
                        it.Tag = u;
                        lvTeamMembers.Items.Add(it);
                    }
                    Log($"Team '{team.Name}': {members.Count} membri.");
                }));
            });
        }

        private async Task RemoveUsersFromTeamAsync()
        {
            if (lvTeamsList.SelectedItems.Count == 0) { ShowMsg("Selezionare un team."); return; }
            if (lvTeamMembers.SelectedItems.Count == 0) { ShowMsg("Selezionare i membri da rimuovere."); return; }
            var team = (TeamInfo)lvTeamsList.SelectedItems[0].Tag;
            var userIds = new List<Guid>();
            foreach (ListViewItem item in lvTeamMembers.SelectedItems) userIds.Add(((UserInfo)item.Tag).Id);
            if (Ask($"Rimuovere {userIds.Count} utenti dal team '{team.Name}'?") != DialogResult.Yes) return;
            await RunAsync(() =>
            {
                var r = DynamicsOperations.RemoveUsersFromTeam(_connection.ServiceClient, team.Id, team.Name, userIds, s => Log(s));
                Invoke((Action)(() => { Log(r.Message); ShowMsg(r.Message, r.Success); }));
            });
            await OnTeamSelectedAsync();
        }

        private async Task SearchUsersForTeamAsync()
        {
            if (string.IsNullOrWhiteSpace(txtTeamUserSearch.Text)) return;
            await RunAsync(() =>
            {
                var users = DynamicsOperations.SearchUsers(_connection.ServiceClient, txtTeamUserSearch.Text);
                Invoke((Action)(() =>
                {
                    lvTeamUserResults.Items.Clear();
                    foreach (var u in users)
                    {
                        var it = new ListViewItem(u.FullName);
                        it.SubItems.Add(u.Email);
                        it.SubItems.Add(u.BusinessUnitName);
                        it.Tag = u;
                        lvTeamUserResults.Items.Add(it);
                    }
                }));
            });
        }

        private async Task AddUsersToTeamAsync()
        {
            if (lvTeamsList.SelectedItems.Count == 0) { ShowMsg("Selezionare un team."); return; }
            if (lvTeamUserResults.SelectedItems.Count == 0) { ShowMsg("Selezionare gli utenti da aggiungere."); return; }
            var team = (TeamInfo)lvTeamsList.SelectedItems[0].Tag;
            var userIds = new List<Guid>();
            foreach (ListViewItem item in lvTeamUserResults.SelectedItems) userIds.Add(((UserInfo)item.Tag).Id);
            if (Ask($"Aggiungere {userIds.Count} utenti al team '{team.Name}'?") != DialogResult.Yes) return;
            await RunAsync(() =>
            {
                var r = DynamicsOperations.AddUsersToTeam(_connection.ServiceClient, team.Id, team.Name, userIds, s => Log(s));
                Invoke((Action)(() => { Log(r.Message); ShowMsg(r.Message, r.Success); }));
            });
            await OnTeamSelectedAsync();
        }

        // ─────────── Connection ───────────

        private async Task ConnectAsync()
        {
            try
            {
                SetBusy(true); btnConnect.Enabled = false;
                lblStatus.Text = "Autenticazione..."; lblStatus.ForeColor = AppTheme.Warning;
                Log("Avvio autenticazione...");
                await _connection.AuthenticateAsync();
                Log("Autenticazione riuscita. Recupero ambienti...");
                var envs = await _connection.GetAvailableEnvironmentsAsync();
                Log($"Trovati {envs.Count} ambienti.");
                if (envs.Count == 0) { ShowMsg("Nessun ambiente disponibile."); lblStatus.Text = "Disconnesso"; lblStatus.ForeColor = AppTheme.FgSecondary; btnConnect.Enabled = true; SetBusy(false); return; }
                SetBusy(false);
                using (var sel = new EnvironmentSelector(envs))
                {
                    if (sel.ShowDialog(this) != DialogResult.OK) { lblStatus.Text = "Disconnesso"; lblStatus.ForeColor = AppTheme.FgSecondary; btnConnect.Enabled = true; return; }
                    SetBusy(true);
                    Log($"Connessione a {sel.SelectedEnvironment.FriendlyName}...");
                    lblStatus.Text = $"Connessione a {sel.SelectedEnvironment.FriendlyName}...";
                    await _connection.ConnectToEnvironmentAsync(sel.SelectedEnvironment);
                }
                var env = _connection.CurrentEnvironment;
                lblStatus.Text = $"Connesso: {env.FriendlyName} (v{env.Version})";
                lblStatus.ForeColor = AppTheme.InfoBlue;
                btnConnect.Enabled = false; btnDisconnect.Enabled = true; tabControl.Enabled = true;
                Log($"Connesso a {env.FriendlyName}.");
                await RunAsync(() =>
                {
                    var bus = DynamicsOperations.GetAllBusinessUnits(_connection.ServiceClient);
                    Invoke((Action)(() => { cbBusinessUnits.DataSource = bus; cbBusinessUnits.DisplayMember = "Name"; }));
                });
            }
            catch (Exception ex) { Log($"Errore: {ex.Message}"); ShowMsg(ex.Message); lblStatus.Text = "Disconnesso"; lblStatus.ForeColor = AppTheme.FgSecondary; btnConnect.Enabled = true; }
            finally { SetBusy(false); }
        }

        private void Disconnect()
        {
            _connection.Disconnect();
            lblStatus.Text = "Disconnesso"; lblStatus.ForeColor = AppTheme.FgSecondary;
            btnConnect.Enabled = true; btnDisconnect.Enabled = false; tabControl.Enabled = false;
            Log("Disconnesso.");
        }

        // ─────────── Helpers ───────────

        private Button MBtn(string text, Point loc, int minWidth, bool primary = false)
        {
            var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            int textW = TextRenderer.MeasureText(text, font).Width;
            int w = Math.Max(minWidth, textW + 24);
            var btn = new Button
            {
                Text = text, Location = loc,
                Size = new Size(w, 36),
                AutoSize = false,
                FlatStyle = FlatStyle.Flat,
                Font = font,
                ForeColor = primary ? Color.White : AppTheme.FgPrimary,
                BackColor = primary ? AppTheme.AccentBlue : AppTheme.BtnBg,
                Cursor = Cursors.Hand
            };
            if (primary) btn.Tag = "primary";
            btn.FlatAppearance.BorderColor = primary ? AppTheme.AccentBlue : AppTheme.Border;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = primary ? AppTheme.AccentHover : AppTheme.BtnHover;
            return btn;
        }

        private TextBox MTxt(string placeholder, Point loc, int width)
        {
            var txt = new TextBox
            {
                Location = loc, Size = new Size(width, 28),
                Font = new Font("Segoe UI", 10f),
                BackColor = AppTheme.InputBg,
                ForeColor = AppTheme.FgPlaceholder,
                BorderStyle = BorderStyle.FixedSingle,
                Text = placeholder
            };
            txt.GotFocus += (s, e) => { if (txt.ForeColor == AppTheme.FgPlaceholder || txt.ForeColor == Color.Gray) { txt.ForeColor = AppTheme.FgPrimary; txt.Text = ""; } };
            txt.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txt.Text)) { txt.ForeColor = AppTheme.FgPlaceholder; txt.Text = placeholder; } };
            return txt;
        }

        private Label MLbl(string text, Point loc, bool bold = false)
        {
            return new Label
            {
                Text = text, Location = loc, AutoSize = true,
                ForeColor = AppTheme.FgPrimary, BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9f, bold ? FontStyle.Bold : FontStyle.Regular)
            };
        }

        private ListView MLv(Point loc, Size size)
        {
            return new ListView
            {
                Location = loc, Size = size,
                View = View.Details, FullRowSelect = true, GridLines = false, MultiSelect = false,
                BackColor = AppTheme.ListBg, ForeColor = AppTheme.FgPrimary,
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private CheckBox MCk(string text, int x, int y, bool chk = false)
        {
            return new CheckBox
            {
                Text = text, Location = new Point(x, y), Checked = chk, AutoSize = true,
                ForeColor = AppTheme.FgPrimary, BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9f)
            };
        }

        private void SetInfo(Label lbl, UserInfo u)
        {
            if (u != null) { lbl.Text = $"{u.FullName} ({u.BusinessUnitName})"; lbl.ForeColor = AppTheme.InfoBlue; }
            else { lbl.Text = "Non trovato"; lbl.ForeColor = AppTheme.Error; }
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
            rtbLog.SelectionColor = AppTheme.LogTimestamp;
            rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] ");
            Color c = AppTheme.LogFg;
            if (msg.StartsWith("Errore", StringComparison.OrdinalIgnoreCase) || msg.Contains("fallita") || msg.Contains("fallito")) c = AppTheme.Error;
            else if (msg.Contains("riuscit") || msg.Contains("Successo") || msg.Contains("completat") || msg.Contains("Connesso")) c = AppTheme.Success;
            rtbLog.SelectionStart = rtbLog.TextLength; rtbLog.SelectionLength = 0;
            rtbLog.SelectionColor = c;
            rtbLog.AppendText(msg + "\n"); rtbLog.ScrollToCaret();
        }

        protected override void OnFormClosing(FormClosingEventArgs e) { _connection.Dispose(); base.OnFormClosing(e); }
    }
}
