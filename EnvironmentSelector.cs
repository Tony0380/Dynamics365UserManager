using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Dynamics365UserManager
{
    public class EnvironmentSelector : Form
    {
        private ListView listView;
        private TextBox filterBox;
        private Button btnConnect, btnCancel;
        private List<EnvironmentInfo> _allEnvironments;

        public EnvironmentInfo SelectedEnvironment { get; private set; }

        public EnvironmentSelector(List<EnvironmentInfo> environments)
        {
            _allEnvironments = environments;

            Text = "Seleziona Ambiente";
            Size = new Size(800, 480);
            MinimumSize = new Size(600, 350);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = AppTheme.FormBg;

            int top = 12;

            filterBox = new TextBox
            {
                Location = new Point(12, top),
                Size = new Size(400, 28),
                Font = new Font("Segoe UI", 10f),
                BackColor = AppTheme.InputBg,
                ForeColor = AppTheme.FgPlaceholder,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Filtra ambienti..."
            };
            filterBox.GotFocus += (s, e) => { if (filterBox.ForeColor == AppTheme.FgPlaceholder || filterBox.ForeColor == Color.Gray) { filterBox.ForeColor = AppTheme.FgPrimary; filterBox.Text = ""; } };
            filterBox.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(filterBox.Text)) { filterBox.ForeColor = AppTheme.FgPlaceholder; filterBox.Text = "Filtra ambienti..."; } };
            filterBox.TextChanged += (s, e) =>
            {
                if (filterBox.ForeColor == AppTheme.FgPlaceholder || filterBox.ForeColor == Color.Gray) return;
                PopulateList(
                    _allEnvironments.Where(env =>
                        env.FriendlyName != null &&
                        env.FriendlyName.IndexOf(filterBox.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList());
            };
            Controls.Add(filterBox);

            top += 40;

            listView = new ListView
            {
                Location = new Point(12, top),
                Size = new Size(ClientSize.Width - 24, ClientSize.Height - top - 56),
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                MultiSelect = false,
                BackColor = AppTheme.ListBg,
                ForeColor = AppTheme.FgPrimary,
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            listView.Columns.Add("Nome", 220);
            listView.Columns.Add("Tipo", 80);
            listView.Columns.Add("Regione", 80);
            listView.Columns.Add("URL", 270);
            listView.Columns.Add("Versione", 90);
            listView.DoubleClick += (s, e) => { if (listView.SelectedItems.Count > 0) SelectAndClose(); };
            Controls.Add(listView);

            var btnFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);

            btnCancel = new Button
            {
                Text = "ANNULLA",
                Size = new Size(Math.Max(100, TextRenderer.MeasureText("ANNULLA", btnFont).Width + 24), 36),
                AutoSize = false,
                FlatStyle = FlatStyle.Flat,
                Font = btnFont,
                ForeColor = AppTheme.FgPrimary,
                BackColor = AppTheme.BtnBg,
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom
            };
            btnCancel.FlatAppearance.BorderColor = AppTheme.Border;
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = AppTheme.BtnHover;
            Controls.Add(btnCancel);

            btnConnect = new Button
            {
                Text = "CONNETTI",
                Size = new Size(Math.Max(100, TextRenderer.MeasureText("CONNETTI", btnFont).Width + 24), 36),
                AutoSize = false,
                FlatStyle = FlatStyle.Flat,
                Font = btnFont,
                ForeColor = Color.White,
                BackColor = AppTheme.AccentBlue,
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom
            };
            btnConnect.FlatAppearance.BorderColor = AppTheme.AccentBlue;
            btnConnect.FlatAppearance.BorderSize = 1;
            btnConnect.FlatAppearance.MouseOverBackColor = AppTheme.AccentHover;
            btnConnect.Click += (s, e) => SelectAndClose();
            Controls.Add(btnConnect);

            AcceptButton = btnConnect;
            CancelButton = btnCancel;

            PositionButtons();
            Resize += (s, e) => PositionButtons();

            PopulateList(environments);
        }

        private void PositionButtons()
        {
            int btnY = ClientSize.Height - 44;
            btnCancel.Location = new Point(ClientSize.Width - 220, btnY);
            btnConnect.Location = new Point(ClientSize.Width - 112, btnY);
        }

        private void PopulateList(List<EnvironmentInfo> environments)
        {
            listView.Items.Clear();
            foreach (var env in environments)
            {
                var item = new ListViewItem(env.FriendlyName ?? "");
                item.SubItems.Add(env.Purpose ?? "");
                item.SubItems.Add(env.Region ?? "");
                item.SubItems.Add(env.Url ?? "");
                item.SubItems.Add(env.Version ?? "");
                item.Tag = env;
                listView.Items.Add(item);
            }
        }

        private void SelectAndClose()
        {
            if (listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Selezionare un ambiente.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SelectedEnvironment = (EnvironmentInfo)listView.SelectedItems[0].Tag;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
