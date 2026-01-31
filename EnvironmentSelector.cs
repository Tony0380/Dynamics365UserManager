using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;

namespace Dynamics365UserManager
{
    public class EnvironmentSelector : MaterialForm
    {
        private MaterialListView listView;
        private MaterialTextBox2 filterBox;
        private MaterialButton btnConnect, btnCancel;
        private List<EnvironmentInfo> _allEnvironments;

        public EnvironmentInfo SelectedEnvironment { get; private set; }

        public EnvironmentSelector(List<EnvironmentInfo> environments)
        {
            _allEnvironments = environments;

            var skin = MaterialSkinManager.Instance;
            skin.AddFormToManage(this);

            Text = "Seleziona Ambiente";
            Size = new Size(800, 530);
            MinimumSize = new Size(800, 530);
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            Sizable = false;

            int top = 64;

            filterBox = new MaterialTextBox2
            {
                Hint = "Filtra ambienti...",
                Location = new Point(12, top),
                Size = new Size(400, 48)
            };
            filterBox.TextChanged += (s, e) => PopulateList(
                _allEnvironments.Where(env =>
                    env.FriendlyName != null &&
                    env.FriendlyName.IndexOf(filterBox.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList());
            Controls.Add(filterBox);

            top += 56;

            listView = new MaterialListView
            {
                Location = new Point(12, top),
                Size = new Size(760, 310),
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                MultiSelect = false
            };
            listView.Columns.Add("Nome", 220);
            listView.Columns.Add("Tipo", 80);
            listView.Columns.Add("Regione", 80);
            listView.Columns.Add("URL", 270);
            listView.Columns.Add("Versione", 90);
            listView.DoubleClick += (s, e) => { if (listView.SelectedItems.Count > 0) SelectAndClose(); };
            Controls.Add(listView);

            top += 318;

            btnCancel = new MaterialButton
            {
                Text = "ANNULLA",
                Location = new Point(560, top),
                Size = new Size(100, 36),
                AutoSize = false,
                Type = MaterialButton.MaterialButtonType.Outlined,
                DialogResult = DialogResult.Cancel
            };
            Controls.Add(btnCancel);

            btnConnect = new MaterialButton
            {
                Text = "CONNETTI",
                Location = new Point(670, top),
                Size = new Size(100, 36),
                AutoSize = false,
                Type = MaterialButton.MaterialButtonType.Contained
            };
            btnConnect.Click += (s, e) => SelectAndClose();
            Controls.Add(btnConnect);

            AcceptButton = btnConnect;
            CancelButton = btnCancel;

            PopulateList(environments);
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
