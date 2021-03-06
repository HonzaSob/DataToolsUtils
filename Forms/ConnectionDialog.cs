﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell.Interop;
using DataToolsUtils.Services;
using DataToolsUtils.Entities;
using Microsoft.Data.ConnectionUI;

namespace DataToolsUtils.Forms
{
    internal partial class ConnectionDialog : Form
    {
        SettingService settingService = null;

        private ConnectionDialog()
        {
            InitializeComponent();
        }

        public ConnectionDialog(IVsSettingsManager settingsManager) : this()
        {
            settingService = new SettingService(settingsManager);
        }

        private ConnectionString selectedConnectionString = null;

        internal ConnectionString SelectedConnectionString
        {
            get
            {
                return selectedConnectionString;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            { 
                if (listBoxConnections.SelectedItem == null)
                    return;

                this.selectedConnectionString = listBoxConnections.SelectedItem as ConnectionString;
                this.settingService.SetDefaultConnectionString(this.selectedConnectionString);
 
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void ConnectionDialog_Load(object sender, EventArgs e)
        {
            var connStrings = this.settingService.GetConnectionStrings();
            var def = this.settingService.GetDefaultConnectionString();

            foreach (var it in connStrings)
            {
                listBoxConnections.Items.Add(it);
                if (it.ConnectionStringRaw == def.ConnectionStringRaw)
                    listBoxConnections.SelectedItem = it;
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                DataConnectionConfiguration dcs = new DataConnectionConfiguration(null);
                DataConnectionDialog dialog = new DataConnectionDialog();
                dcs.LoadConfiguration(dialog);
                DialogResult dr = DataConnectionDialog.Show(dialog);
                if (dr == DialogResult.OK)
                {
                    string connString = dialog.ConnectionString;
                    ConnectionString cs = new ConnectionString() { ConnectionStringRaw = connString };
                    this.settingService.AddConnectionString(cs);
                    listBoxConnections.Items.Add(cs);
                    listBoxConnections.SelectedItem = cs;
                }
            }
            catch (SettingService.DuplicateConnectionStringException)
            {
                MessageBox.Show("Connection string already exists in the list");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
}

        private void btnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (listBoxConnections.SelectedItem == null)
                    return;

                ConnectionString selected = listBoxConnections.SelectedItem as ConnectionString;
                this.settingService.DeleteConnectionString(selected);
                listBoxConnections.Items.Remove(selected);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
