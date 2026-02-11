using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Diagnostics;
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

        // Tab 5
        private TextBox txtTeamSearch;
        private Button btnTeamSearch;
        private ListView lvTeamsList, lvTeamMembers;
        private TextBox txtTeamUserSearch;
        private Button btnTeamUserSearch, btnAddToTeam, btnRemoveFromTeam;
        private ListView lvTeamUserResults;

        // Tab 6 – Role Finder
        private ComboBox cbRFEntity, cbRFPermission, cbRFDepth;
        private Button btnRFAdd, btnRFRemove, btnRFSearch;
        private ListView lvRFRequirements, lvRFResults;
        private NumericUpDown nudRFMaxRoles;
        private readonly List<PrivilegeRequirement> _rfRequirements = new List<PrivilegeRequirement>();

        // Log
        private RichTextBox rtbLog;
        private LinkLabel lblCredits;

        private bool _busy;

        public MainForm()
        {
            Text = "Dynamics 365 User Manager";
            Size = new Size(1050, 780);
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = true;
            FormBorderStyle = FormBorderStyle.Sizable;
            BuildUI();
        }

        private void BuildUI()
        {
            SuspendLayout();

            // ── Bottom: Log area ──
            var logPanel = new Panel { Dock = DockStyle.Bottom, Height = 190, Padding = new Padding(6, 0, 6, 6) };

            var logToolbar = new Panel { Dock = DockStyle.Top, Height = 36 };
            var btnClear = MBtn("PULISCI LOG", 130);
            btnClear.Location = new Point(0, 2);
            btnClear.Click += (s, e) => rtbLog.Clear();
            logToolbar.Controls.Add(btnClear);

            lblCredits = new LinkLabel
            {
                Text = "Creato da Antonio Colamartino",
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f),
                LinkColor = AppTheme.Link,
                VisitedLinkColor = AppTheme.Link,
                ActiveLinkColor = AppTheme.LinkActive,
                BackColor = Color.Transparent,
                Location = new Point(800, 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            lblCredits.LinkClicked += (s, e) =>
                Process.Start(new ProcessStartInfo("https://antoniocolamartino.it") { UseShellExecute = true });
            logToolbar.Controls.Add(lblCredits);

            rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = AppTheme.LogBg,
                ForeColor = AppTheme.LogFg,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None
            };

            logPanel.Controls.Add(rtbLog);
            logPanel.Controls.Add(logToolbar);

            // ── Top: Connection bar ──
            var connPanel = new Panel { Dock = DockStyle.Top, Height = 48 };

            btnConnect = MBtn("CONNETTI", 100, true);
            btnConnect.Location = new Point(8, 6);
            btnConnect.Click += async (s, e) => await ConnectAsync();
            connPanel.Controls.Add(btnConnect);

            btnDisconnect = MBtn("DISCONNETTI", 120);
            btnDisconnect.Location = new Point(btnConnect.Right + 6, 6);
            btnDisconnect.Enabled = false;
            btnDisconnect.Click += (s, e) => Disconnect();
            connPanel.Controls.Add(btnDisconnect);

            btnResetLogin = MBtn("CANCELLA CREDENZIALI", 160);
            btnResetLogin.Location = new Point(btnDisconnect.Right + 6, 6);
            btnResetLogin.Click += (s, e) => { _connection.ResetLogin(); Log("Cache login cancellata."); };
            connPanel.Controls.Add(btnResetLogin);

            progressBar = new ProgressBar
            {
                Location = new Point(btnResetLogin.Right + 12, 18),
                Size = new Size(120, 5),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };
            connPanel.Controls.Add(progressBar);

            lblStatus = new Label
            {
                Text = "Disconnesso",
                Location = new Point(progressBar.Right + 12, 12),
                Size = new Size(400, 24),
                ForeColor = Color.Gray,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9f),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            connPanel.Controls.Add(lblStatus);

            // ── Center: Tabs ──
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f),
                Padding = new Point(12, 4),
                DrawMode = TabDrawMode.OwnerDrawFixed
            };
            tabControl.DrawItem += TabControl_DrawItem;
            tabControl.TabPages.Add(CreateTabChangeBU());
            tabControl.TabPages.Add(CreateTabClone());
            tabControl.TabPages.Add(CreateTabReassign());
            tabControl.TabPages.Add(CreateTabSecurityRoles());
            tabControl.TabPages.Add(CreateTabTeams());
            tabControl.TabPages.Add(CreateTabRoleFinder());
            tabControl.Enabled = false;

            // Dock order: last added = processed first by layout engine
            Controls.Add(tabControl);  // Fill (innermost)
            Controls.Add(logPanel);    // Bottom
            Controls.Add(connPanel);   // Top (outermost)

            ResumeLayout(true);
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

        // ─────────── Tab 1 – Cambio BU ───────────

        private TabPage CreateTabChangeBU()
        {
            var t = new TabPage("Cambio BU") { BackColor = AppTheme.ControlBg, Padding = new Padding(6) };

            // Search bar (top)
            var searchPanel = new Panel { Dock = DockStyle.Top, Height = 44 };
            txtBUSearch = MTxt("Cerca utente per nome o email...", 340);
            txtBUSearch.Location = new Point(6, 6);
            searchPanel.Controls.Add(txtBUSearch);
            btnBUSearch = MBtn("CERCA UTENTE", 110);
            btnBUSearch.Location = new Point(356, 4);
            btnBUSearch.Click += async (s, e) => await SearchUsersForBUAsync();
            searchPanel.Controls.Add(btnBUSearch);
            txtBUSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { btnBUSearch.PerformClick(); e.SuppressKeyPress = true; } };

            // Action bar (bottom)
            var actionPanel = new Panel { Dock = DockStyle.Bottom, Height = 52 };
            var lblBU = MLbl("Nuova Business Unit:", true);
            lblBU.Location = new Point(6, 14);
            actionPanel.Controls.Add(lblBU);
            cbBusinessUnits = new ComboBox
            {
                Location = new Point(180, 10), Size = new Size(320, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppTheme.InputBg, ForeColor = AppTheme.FgPrimary,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f)
            };
            actionPanel.Controls.Add(cbBusinessUnits);
            btnChangeBU = MBtn("ASSEGNA BU", 130, true);
            btnChangeBU.Location = new Point(510, 8);
            btnChangeBU.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnChangeBU.Click += async (s, e) => await ChangeBUAsync();
            actionPanel.Controls.Add(btnChangeBU);

            // Users | Roles split (fill)
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 8
            };

            var lblUsersHdr = MLbl("Utenti trovati:", true);
            lblUsersHdr.Dock = DockStyle.Top;
            lblUsersHdr.AutoSize = false;
            lblUsersHdr.Height = 24;
            lvUsers = MLv();
            lvUsers.Dock = DockStyle.Fill;
            lvUsers.Columns.Add("Nome", 175);
            lvUsers.Columns.Add("Email", 170);
            lvUsers.Columns.Add("Business Unit", 130);
            lvUsers.SelectedIndexChanged += async (s, e) => await OnUserSelectedAsync();
            split.Panel1.Controls.Add(lvUsers);
            split.Panel1.Controls.Add(lblUsersHdr);

            var lblRolesHdr = MLbl("Ruoli correnti:", true);
            lblRolesHdr.Dock = DockStyle.Top;
            lblRolesHdr.AutoSize = false;
            lblRolesHdr.Height = 24;
            lvRoles = MLv();
            lvRoles.Dock = DockStyle.Fill;
            lvRoles.Columns.Add("Nome Ruolo", 460);
            split.Panel2.Controls.Add(lvRoles);
            split.Panel2.Controls.Add(lblRolesHdr);

            t.Controls.Add(split);
            t.Controls.Add(actionPanel);
            t.Controls.Add(searchPanel);
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
            var t = new TabPage("Clone User") { BackColor = AppTheme.ControlBg, Padding = new Padding(6) };

            // Search area (top)
            var searchPanel = new Panel { Dock = DockStyle.Top, Height = 90 };
            txtCloneSource = MTxt("Email sorgente", 310);
            txtCloneSource.Location = new Point(6, 6);
            searchPanel.Controls.Add(txtCloneSource);
            btnCloneSearchSource = MBtn("CERCA SORGENTE", 130);
            btnCloneSearchSource.Location = new Point(326, 4);
            btnCloneSearchSource.Click += async (s, e) => { _cloneSource = await FindUserAsync(txtCloneSource.Text); SetInfo(lblSourceInfo, _cloneSource); };
            searchPanel.Controls.Add(btnCloneSearchSource);
            lblSourceInfo = MLbl("");
            lblSourceInfo.Location = new Point(470, 10);
            lblSourceInfo.AutoSize = false;
            lblSourceInfo.Size = new Size(500, 24);
            lblSourceInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            searchPanel.Controls.Add(lblSourceInfo);

            txtCloneTarget = MTxt("Email target", 310);
            txtCloneTarget.Location = new Point(6, 48);
            searchPanel.Controls.Add(txtCloneTarget);
            btnCloneSearchTarget = MBtn("CERCA DESTINAZIONE", 150);
            btnCloneSearchTarget.Location = new Point(326, 46);
            btnCloneSearchTarget.Click += async (s, e) => { _cloneTarget = await FindUserAsync(txtCloneTarget.Text); SetInfo(lblTargetInfo, _cloneTarget); };
            searchPanel.Controls.Add(btnCloneSearchTarget);
            lblTargetInfo = MLbl("");
            lblTargetInfo.Location = new Point(490, 52);
            lblTargetInfo.AutoSize = false;
            lblTargetInfo.Size = new Size(480, 24);
            lblTargetInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            searchPanel.Controls.Add(lblTargetInfo);

            // Clone button (bottom)
            var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 48 };
            btnClone = MBtn("AVVIA CLONAZIONE", 180, true);
            btnClone.Location = new Point(6, 6);
            btnClone.Click += async (s, e) => await CloneUserAsync();
            btnPanel.Controls.Add(btnClone);

            // Options + Teams (fill)
            var middlePanel = new Panel { Dock = DockStyle.Fill };

            var optPanel = new Panel { Dock = DockStyle.Left, Width = 240 };
            var lblOpt = MLbl("Opzioni:", true);
            lblOpt.Location = new Point(6, 8);
            optPanel.Controls.Add(lblOpt);
            chkCloneBU = MCk("Business Unit", true); chkCloneBU.Location = new Point(8, 34);
            chkCloneRoles = MCk("Security Roles", true); chkCloneRoles.Location = new Point(8, 62);
            chkCloneTeams = MCk("Teams"); chkCloneTeams.Location = new Point(8, 90);
            chkCloneTeams.CheckedChanged += async (s, e) => { if (chkCloneTeams.Checked && _cloneSource != null) await LoadTeamsAsync(); };
            optPanel.Controls.AddRange(new Control[] { chkCloneBU, chkCloneRoles, chkCloneTeams });

            var teamsPanel = new Panel { Dock = DockStyle.Fill };
            var teamsToolbar = new Panel { Dock = DockStyle.Top, Height = 28 };
            var lblTeamsHdr = MLbl("Teams sorgente:", true);
            lblTeamsHdr.Location = new Point(0, 4);
            teamsToolbar.Controls.Add(lblTeamsHdr);

            var teamsBtnPanel = new Panel { Dock = DockStyle.Right, Width = 160 };
            btnSelectAll = MBtn("SELEZIONA TUTTI", 140);
            btnSelectAll.Location = new Point(10, 8);
            btnSelectAll.Click += (s, e) => { for (int i = 0; i < clbTeams.Items.Count; i++) clbTeams.SetItemChecked(i, true); };
            teamsBtnPanel.Controls.Add(btnSelectAll);
            btnDeselectAll = MBtn("DESELEZIONA TUTTI", 140);
            btnDeselectAll.Location = new Point(10, 48);
            btnDeselectAll.Click += (s, e) => { for (int i = 0; i < clbTeams.Items.Count; i++) clbTeams.SetItemChecked(i, false); };
            teamsBtnPanel.Controls.Add(btnDeselectAll);

            clbTeams = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f),
                BackColor = AppTheme.CheckListBg,
                ForeColor = AppTheme.CheckListFg
            };

            teamsPanel.Controls.Add(clbTeams);
            teamsPanel.Controls.Add(teamsBtnPanel);
            teamsPanel.Controls.Add(teamsToolbar);

            middlePanel.Controls.Add(teamsPanel);
            middlePanel.Controls.Add(optPanel);

            t.Controls.Add(middlePanel);
            t.Controls.Add(btnPanel);
            t.Controls.Add(searchPanel);
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
            var t = new TabPage("Reassign") { BackColor = AppTheme.ControlBg, Padding = new Padding(6) };

            // Search area (top)
            var searchPanel = new Panel { Dock = DockStyle.Top, Height = 90 };
            txtReassignOld = MTxt("Email vecchio owner", 310);
            txtReassignOld.Location = new Point(6, 6);
            searchPanel.Controls.Add(txtReassignOld);
            btnReassignSearchOld = MBtn("CERCA VECCHIO", 120);
            btnReassignSearchOld.Location = new Point(326, 4);
            btnReassignSearchOld.Click += async (s, e) => { _reassignOld = await FindUserAsync(txtReassignOld.Text); SetInfo(lblOldOwner, _reassignOld); };
            searchPanel.Controls.Add(btnReassignSearchOld);
            lblOldOwner = MLbl("");
            lblOldOwner.Location = new Point(460, 10);
            lblOldOwner.AutoSize = false;
            lblOldOwner.Size = new Size(500, 24);
            lblOldOwner.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            searchPanel.Controls.Add(lblOldOwner);

            txtReassignNew = MTxt("Email nuovo owner", 310);
            txtReassignNew.Location = new Point(6, 48);
            searchPanel.Controls.Add(txtReassignNew);
            btnReassignSearchNew = MBtn("CERCA NUOVO", 120);
            btnReassignSearchNew.Location = new Point(326, 46);
            btnReassignSearchNew.Click += async (s, e) => { _reassignNew = await FindUserAsync(txtReassignNew.Text); SetInfo(lblNewOwner, _reassignNew); };
            searchPanel.Controls.Add(btnReassignSearchNew);
            lblNewOwner = MLbl("");
            lblNewOwner.Location = new Point(460, 52);
            lblNewOwner.AutoSize = false;
            lblNewOwner.Size = new Size(500, 24);
            lblNewOwner.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            searchPanel.Controls.Add(lblNewOwner);

            // Button bar (bottom)
            var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 48 };
            btnCountRecords = MBtn("ANTEPRIMA CONTEGGIO", 180);
            btnCountRecords.Location = new Point(6, 6);
            btnCountRecords.Click += async (s, e) => await CountRecordsAsync();
            btnPanel.Controls.Add(btnCountRecords);
            btnReassign = MBtn("TRASFERISCI RECORD", 170, true);
            btnReassign.Location = new Point(btnCountRecords.Right + 8, 6);
            btnReassign.Click += async (s, e) => await ReassignRecordsAsync();
            btnPanel.Controls.Add(btnReassign);

            // Checkboxes + Preview (fill)
            var middlePanel = new Panel { Dock = DockStyle.Fill };

            var checkPanel = new Panel { Dock = DockStyle.Left, Width = 340 };
            var lblTypes = MLbl("Tipi di record:", true);
            lblTypes.Location = new Point(6, 8);
            checkPanel.Controls.Add(lblTypes);
            chkAccount = MCk("Account", true); chkAccount.Location = new Point(8, 34);
            chkContact = MCk("Contact", true); chkContact.Location = new Point(8, 62);
            chkOpportunity = MCk("Opportunity", true); chkOpportunity.Location = new Point(8, 90);
            chkQuote = MCk("Quote", true); chkQuote.Location = new Point(8, 118);
            chkOrder = MCk("Sales Order", true); chkOrder.Location = new Point(160, 34);
            chkLead = MCk("Lead", true); chkLead.Location = new Point(160, 62);
            chkCase = MCk("Case", true); chkCase.Location = new Point(160, 90);
            checkPanel.Controls.AddRange(new Control[] { chkAccount, chkContact, chkOpportunity, chkQuote, chkOrder, chkLead, chkCase });

            var previewPanel = new Panel { Dock = DockStyle.Fill };
            var lblPreview = MLbl("Anteprima:", true);
            lblPreview.Location = new Point(6, 8);
            previewPanel.Controls.Add(lblPreview);
            lblCounts = MLbl("");
            lblCounts.Location = new Point(6, 34);
            lblCounts.AutoSize = false;
            lblCounts.Size = new Size(320, 128);
            previewPanel.Controls.Add(lblCounts);

            middlePanel.Controls.Add(previewPanel);
            middlePanel.Controls.Add(checkPanel);

            t.Controls.Add(middlePanel);
            t.Controls.Add(btnPanel);
            t.Controls.Add(searchPanel);
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
            var t = new TabPage("Security Roles") { BackColor = AppTheme.ControlBg, Padding = new Padding(6) };

            // Search bar (top)
            var searchPanel = new Panel { Dock = DockStyle.Top, Height = 44 };
            txtRoleSearch = MTxt("Cerca ruolo per nome...", 340);
            txtRoleSearch.Location = new Point(6, 6);
            searchPanel.Controls.Add(txtRoleSearch);
            btnRoleSearch = MBtn("CERCA RUOLO", 110);
            btnRoleSearch.Location = new Point(356, 4);
            btnRoleSearch.Click += async (s, e) => await SearchRolesAsync();
            searchPanel.Controls.Add(btnRoleSearch);
            txtRoleSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { btnRoleSearch.PerformClick(); e.SuppressKeyPress = true; } };

            // Main split: top (roles/users) | bottom (assign area)
            var mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 8
            };

            // ── Top: roles list | role users ──
            var topSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 8
            };

            var lblRolesHdr = MLbl("Ruoli trovati:", true);
            lblRolesHdr.Dock = DockStyle.Top;
            lblRolesHdr.AutoSize = false;
            lblRolesHdr.Height = 24;
            lvRolesList = MLv();
            lvRolesList.Dock = DockStyle.Fill;
            lvRolesList.Columns.Add("Nome", 230);
            lvRolesList.Columns.Add("BU", 95);
            lvRolesList.SelectedIndexChanged += async (s, e) => await OnRoleSelectedAsync();
            topSplit.Panel1.Controls.Add(lvRolesList);
            topSplit.Panel1.Controls.Add(lblRolesHdr);

            var lblRoleUsersHdr = MLbl("Utenti con il ruolo selezionato:", true);
            lblRoleUsersHdr.Dock = DockStyle.Top;
            lblRoleUsersHdr.AutoSize = false;
            lblRoleUsersHdr.Height = 24;
            var removeBtnPanel = new Panel { Dock = DockStyle.Bottom, Height = 42 };
            btnRemoveRole = MBtn("RIMUOVI RUOLO", 140);
            btnRemoveRole.Location = new Point(0, 4);
            btnRemoveRole.Click += async (s, e) => await RemoveRoleFromSelectedAsync();
            removeBtnPanel.Controls.Add(btnRemoveRole);
            lvRoleUsers = MLv();
            lvRoleUsers.Dock = DockStyle.Fill;
            lvRoleUsers.MultiSelect = true;
            lvRoleUsers.Columns.Add("Nome", 200);
            lvRoleUsers.Columns.Add("Email", 220);
            lvRoleUsers.Columns.Add("Business Unit", 190);
            topSplit.Panel2.Controls.Add(lvRoleUsers);
            topSplit.Panel2.Controls.Add(removeBtnPanel);
            topSplit.Panel2.Controls.Add(lblRoleUsersHdr);

            mainSplit.Panel1.Controls.Add(topSplit);

            // ── Bottom: assign section ──
            var assignHeader = new Panel { Dock = DockStyle.Top, Height = 72 };
            var lblAssign = MLbl("Assegna ruolo a utenti:", true);
            lblAssign.Location = new Point(6, 8);
            assignHeader.Controls.Add(lblAssign);
            txtRoleUserSearch = MTxt("Cerca utente per nome o email...", 340);
            txtRoleUserSearch.Location = new Point(6, 36);
            assignHeader.Controls.Add(txtRoleUserSearch);
            btnRoleUserSearch = MBtn("CERCA UTENTE", 110);
            btnRoleUserSearch.Location = new Point(356, 34);
            btnRoleUserSearch.Click += async (s, e) => await SearchUsersForRoleAsync();
            assignHeader.Controls.Add(btnRoleUserSearch);
            txtRoleUserSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { btnRoleUserSearch.PerformClick(); e.SuppressKeyPress = true; } };
            btnAssignRole = MBtn("ASSEGNA RUOLO", 140, true);
            btnAssignRole.Location = new Point(btnRoleUserSearch.Right + 8, 34);
            btnAssignRole.Click += async (s, e) => await AssignRoleToSelectedAsync();
            assignHeader.Controls.Add(btnAssignRole);

            lvRoleUserResults = MLv();
            lvRoleUserResults.Dock = DockStyle.Fill;
            lvRoleUserResults.MultiSelect = true;
            lvRoleUserResults.Columns.Add("Nome", 250);
            lvRoleUserResults.Columns.Add("Email", 350);
            lvRoleUserResults.Columns.Add("Business Unit", 350);

            mainSplit.Panel2.Controls.Add(lvRoleUserResults);
            mainSplit.Panel2.Controls.Add(assignHeader);

            t.Controls.Add(mainSplit);
            t.Controls.Add(searchPanel);
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
            var t = new TabPage("Teams") { BackColor = AppTheme.ControlBg, Padding = new Padding(6) };

            // Search bar (top)
            var searchPanel = new Panel { Dock = DockStyle.Top, Height = 44 };
            txtTeamSearch = MTxt("Cerca team per nome...", 340);
            txtTeamSearch.Location = new Point(6, 6);
            searchPanel.Controls.Add(txtTeamSearch);
            btnTeamSearch = MBtn("CERCA TEAM", 110);
            btnTeamSearch.Location = new Point(356, 4);
            btnTeamSearch.Click += async (s, e) => await SearchTeamsAsync();
            searchPanel.Controls.Add(btnTeamSearch);
            txtTeamSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { btnTeamSearch.PerformClick(); e.SuppressKeyPress = true; } };

            // Main split: top (teams/members) | bottom (add area)
            var mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 8
            };

            // ── Top: teams list | team members ──
            var topSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 8
            };

            var lblTeamsHdr = MLbl("Teams trovati:", true);
            lblTeamsHdr.Dock = DockStyle.Top;
            lblTeamsHdr.AutoSize = false;
            lblTeamsHdr.Height = 24;
            lvTeamsList = MLv();
            lvTeamsList.Dock = DockStyle.Fill;
            lvTeamsList.Columns.Add("Nome", 195);
            lvTeamsList.Columns.Add("BU", 130);
            lvTeamsList.SelectedIndexChanged += async (s, e) => await OnTeamSelectedAsync();
            topSplit.Panel1.Controls.Add(lvTeamsList);
            topSplit.Panel1.Controls.Add(lblTeamsHdr);

            var lblMembersHdr = MLbl("Membri del team selezionato:", true);
            lblMembersHdr.Dock = DockStyle.Top;
            lblMembersHdr.AutoSize = false;
            lblMembersHdr.Height = 24;
            var removeBtnPanel = new Panel { Dock = DockStyle.Bottom, Height = 42 };
            btnRemoveFromTeam = MBtn("RIMUOVI DAL TEAM", 160);
            btnRemoveFromTeam.Location = new Point(0, 4);
            btnRemoveFromTeam.Click += async (s, e) => await RemoveUsersFromTeamAsync();
            removeBtnPanel.Controls.Add(btnRemoveFromTeam);
            lvTeamMembers = MLv();
            lvTeamMembers.Dock = DockStyle.Fill;
            lvTeamMembers.MultiSelect = true;
            lvTeamMembers.Columns.Add("Nome", 200);
            lvTeamMembers.Columns.Add("Email", 220);
            lvTeamMembers.Columns.Add("Business Unit", 190);
            topSplit.Panel2.Controls.Add(lvTeamMembers);
            topSplit.Panel2.Controls.Add(removeBtnPanel);
            topSplit.Panel2.Controls.Add(lblMembersHdr);

            mainSplit.Panel1.Controls.Add(topSplit);

            // ── Bottom: add users section ──
            var addHeader = new Panel { Dock = DockStyle.Top, Height = 72 };
            var lblAdd = MLbl("Aggiungi utenti al team:", true);
            lblAdd.Location = new Point(6, 8);
            addHeader.Controls.Add(lblAdd);
            txtTeamUserSearch = MTxt("Cerca utente per nome o email...", 340);
            txtTeamUserSearch.Location = new Point(6, 36);
            addHeader.Controls.Add(txtTeamUserSearch);
            btnTeamUserSearch = MBtn("CERCA UTENTE", 110);
            btnTeamUserSearch.Location = new Point(356, 34);
            btnTeamUserSearch.Click += async (s, e) => await SearchUsersForTeamAsync();
            addHeader.Controls.Add(btnTeamUserSearch);
            txtTeamUserSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { btnTeamUserSearch.PerformClick(); e.SuppressKeyPress = true; } };
            btnAddToTeam = MBtn("AGGIUNGI AL TEAM", 160, true);
            btnAddToTeam.Location = new Point(btnTeamUserSearch.Right + 8, 34);
            btnAddToTeam.Click += async (s, e) => await AddUsersToTeamAsync();
            addHeader.Controls.Add(btnAddToTeam);

            lvTeamUserResults = MLv();
            lvTeamUserResults.Dock = DockStyle.Fill;
            lvTeamUserResults.MultiSelect = true;
            lvTeamUserResults.Columns.Add("Nome", 250);
            lvTeamUserResults.Columns.Add("Email", 350);
            lvTeamUserResults.Columns.Add("Business Unit", 350);

            mainSplit.Panel2.Controls.Add(lvTeamUserResults);
            mainSplit.Panel2.Controls.Add(addHeader);

            t.Controls.Add(mainSplit);
            t.Controls.Add(searchPanel);
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

        // ─────────── Tab 6 – Trova Ruoli ───────────

        private TabPage CreateTabRoleFinder()
        {
            var t = new TabPage("Trova Ruoli") { BackColor = AppTheme.ControlBg, Padding = new Padding(6) };

            // ── Top: Input area (labels above combos) ──
            var inputPanel = new Panel { Dock = DockStyle.Top, Height = 108 };

            // Row 0: Labels
            var lblEntity = MLbl("Entita':");
            lblEntity.Location = new Point(8, 4);
            inputPanel.Controls.Add(lblEntity);
            var lblPerm = MLbl("Permesso:");
            lblPerm.Location = new Point(200, 4);
            inputPanel.Controls.Add(lblPerm);
            var lblDepth = MLbl("Livello:");
            lblDepth.Location = new Point(360, 4);
            inputPanel.Controls.Add(lblDepth);

            // Row 1: ComboBoxes
            cbRFEntity = new ComboBox
            {
                Location = new Point(8, 24), Size = new Size(180, 28),
                DropDownStyle = ComboBoxStyle.DropDown,
                Font = new Font("Segoe UI", 9f),
                BackColor = AppTheme.InputBg, ForeColor = AppTheme.FgPrimary
            };
            cbRFEntity.Items.AddRange(new object[] {
                "account", "contact", "lead", "opportunity", "quote",
                "salesorder", "invoice", "incident", "task", "phonecall",
                "email", "appointment", "annotation", "knowledgearticle",
                "campaign", "list", "queue", "connection", "goal"
            });
            inputPanel.Controls.Add(cbRFEntity);

            cbRFPermission = new ComboBox
            {
                Location = new Point(200, 24), Size = new Size(148, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                BackColor = AppTheme.InputBg, ForeColor = AppTheme.FgPrimary
            };
            cbRFPermission.Items.AddRange(new object[] { "Create", "Read", "Write", "Delete", "Append", "AppendTo", "Assign", "Share" });
            cbRFPermission.SelectedIndex = 1;
            inputPanel.Controls.Add(cbRFPermission);

            cbRFDepth = new ComboBox
            {
                Location = new Point(360, 24), Size = new Size(160, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                BackColor = AppTheme.InputBg, ForeColor = AppTheme.FgPrimary
            };
            cbRFDepth.Items.Add(new DepthItem("User", 1));
            cbRFDepth.Items.Add(new DepthItem("Business Unit", 2));
            cbRFDepth.Items.Add(new DepthItem("BU + Child", 4));
            cbRFDepth.Items.Add(new DepthItem("Organization", 8));
            cbRFDepth.SelectedIndex = 0;
            inputPanel.Controls.Add(cbRFDepth);

            // Row 2: Buttons
            btnRFAdd = MBtn("AGGIUNGI", 110, true);
            btnRFAdd.Location = new Point(8, 64);
            btnRFAdd.Click += (s, e) => AddRequirement();
            inputPanel.Controls.Add(btnRFAdd);

            btnRFRemove = MBtn("RIMUOVI", 110);
            btnRFRemove.Location = new Point(128, 64);
            btnRFRemove.Click += (s, e) => RemoveRequirement();
            inputPanel.Controls.Add(btnRFRemove);

            // ── Requirements list (top portion, fixed height) ──
            var reqPanel = new Panel { Dock = DockStyle.Top, Height = 180 };
            var lblReqs = MLbl("Requisiti:", true);
            lblReqs.Dock = DockStyle.Top;
            lblReqs.AutoSize = false;
            lblReqs.Height = 24;
            lvRFRequirements = MLv();
            lvRFRequirements.Dock = DockStyle.Fill;
            lvRFRequirements.Columns.Add("Entita'", 180);
            lvRFRequirements.Columns.Add("Permesso", 120);
            lvRFRequirements.Columns.Add("Livello", 150);
            reqPanel.Controls.Add(lvRFRequirements);
            reqPanel.Controls.Add(lblReqs);

            // ── Results area (fill) ──
            var resultPanel = new Panel { Dock = DockStyle.Fill };

            var searchBar = new Panel { Dock = DockStyle.Top, Height = 46 };
            btnRFSearch = MBtn("CERCA COMBINAZIONI", 190, true);
            btnRFSearch.Location = new Point(0, 6);
            btnRFSearch.Click += async (s, e) => await FindCombinationsAsync();
            searchBar.Controls.Add(btnRFSearch);

            var lblMax = MLbl("Max ruoli:");
            lblMax.Location = new Point(200, 14);
            searchBar.Controls.Add(lblMax);

            nudRFMaxRoles = new NumericUpDown
            {
                Location = new Point(280, 8), Size = new Size(60, 28),
                Minimum = 1, Maximum = 8, Value = 3,
                Font = new Font("Segoe UI", 10f),
                BackColor = AppTheme.InputBg, ForeColor = AppTheme.FgPrimary
            };
            searchBar.Controls.Add(nudRFMaxRoles);

            var lblResults = MLbl("Risultati:", true);
            lblResults.Dock = DockStyle.Top;
            lblResults.AutoSize = false;
            lblResults.Height = 24;

            lvRFResults = MLv();
            lvRFResults.Dock = DockStyle.Fill;
            lvRFResults.Columns.Add("# Ruoli", 70);
            lvRFResults.Columns.Add("Combinazione Ruoli", 800);

            resultPanel.Controls.Add(lvRFResults);
            resultPanel.Controls.Add(lblResults);
            resultPanel.Controls.Add(searchBar);

            // Dock order: last added = processed first
            t.Controls.Add(resultPanel);   // Fill
            t.Controls.Add(reqPanel);      // Top (below inputPanel)
            t.Controls.Add(inputPanel);    // Top (topmost)
            return t;
        }

        private class DepthItem
        {
            public string Name { get; set; }
            public int Value { get; set; }
            public DepthItem(string name, int value) { Name = name; Value = value; }
            public override string ToString() => Name;
        }

        private void AddRequirement()
        {
            var entityText = cbRFEntity.Text?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(entityText))
            {
                ShowMsg("Selezionare un'entita'."); return;
            }
            if (cbRFPermission.SelectedItem == null)
            {
                ShowMsg("Selezionare un permesso."); return;
            }
            if (cbRFDepth.SelectedItem == null)
            {
                ShowMsg("Selezionare un livello."); return;
            }

            var permission = cbRFPermission.SelectedItem.ToString();
            var depthItem = (DepthItem)cbRFDepth.SelectedItem;

            if (_rfRequirements.Any(r => r.EntityLogicalName == entityText
                && r.AccessRight == permission && r.MinDepthMask == depthItem.Value))
            {
                ShowMsg("Requisito gia' presente."); return;
            }

            var req = new PrivilegeRequirement
            {
                EntityLogicalName = entityText,
                EntityDisplayName = entityText,
                AccessRight = permission,
                MinDepthMask = depthItem.Value,
                DepthDisplayName = depthItem.Name
            };
            _rfRequirements.Add(req);

            var item = new ListViewItem(entityText);
            item.SubItems.Add(permission);
            item.SubItems.Add(depthItem.Name);
            item.Tag = req;
            lvRFRequirements.Items.Add(item);
            Log($"Requisito aggiunto: {req}");
        }

        private void RemoveRequirement()
        {
            if (lvRFRequirements.SelectedItems.Count == 0)
            {
                ShowMsg("Selezionare un requisito da rimuovere."); return;
            }
            var item = lvRFRequirements.SelectedItems[0];
            var req = (PrivilegeRequirement)item.Tag;
            _rfRequirements.Remove(req);
            lvRFRequirements.Items.Remove(item);
            Log($"Requisito rimosso: {req}");
        }

        private async Task FindCombinationsAsync()
        {
            if (_rfRequirements.Count == 0)
            {
                ShowMsg("Aggiungere almeno un requisito."); return;
            }

            int maxRoles = (int)nudRFMaxRoles.Value;
            var reqCopy = _rfRequirements.ToList();

            lvRFResults.Items.Clear();
            Log($"Ricerca combinazioni (max {maxRoles} ruoli, {reqCopy.Count} requisiti)...");

            await RunAsync(() =>
            {
                var results = DynamicsOperations.FindRoleCombinations(
                    _connection.ServiceClient, reqCopy, maxRoles, s => Log(s));

                Invoke((Action)(() =>
                {
                    lvRFResults.Items.Clear();
                    foreach (var combo in results)
                    {
                        var item = new ListViewItem(combo.Count.ToString());
                        item.SubItems.Add(string.Join(", ", combo.RoleNames));
                        lvRFResults.Items.Add(item);
                    }
                    Log($"Ricerca completata: {results.Count} combinazioni trovate.");
                }));
            });
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

        private Button MBtn(string text, int minWidth, bool primary = false)
        {
            var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            int textW = TextRenderer.MeasureText(text, font).Width;
            int w = Math.Max(minWidth, textW + 24);
            var btn = new Button
            {
                Text = text,
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

        private TextBox MTxt(string placeholder, int width)
        {
            var txt = new TextBox
            {
                Size = new Size(width, 28),
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

        private Label MLbl(string text, bool bold = false)
        {
            return new Label
            {
                Text = text, AutoSize = true,
                ForeColor = AppTheme.FgPrimary, BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9f, bold ? FontStyle.Bold : FontStyle.Regular)
            };
        }

        private ListView MLv()
        {
            return new ListView
            {
                View = View.Details, FullRowSelect = true, GridLines = false, MultiSelect = false,
                BackColor = AppTheme.ListBg, ForeColor = AppTheme.FgPrimary,
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private CheckBox MCk(string text, bool chk = false)
        {
            return new CheckBox
            {
                Text = text, Checked = chk, AutoSize = true,
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
