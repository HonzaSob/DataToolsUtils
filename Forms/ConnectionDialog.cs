using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DataToolsUtils.Forms
{
    public partial class ConnectionDialog : Form
    {
        public ConnectionDialog()
        {
            InitializeComponent();
        }

        public ConnectionDialog(Dictionary<string,string> connectionStrings)
        {
            this.connectionStrings = connectionStrings;

            this.listBoxConnections.DataSource = connectionStrings;
        }

        private string selectedConnectionString = null;

        public string SelectedConnectionString
        {
            get
            {
                return selectedConnectionString;
            }

            set
            {
                selectedConnectionString = value;
            }
        }

        public Dictionary<string, string> ConnectionStrings
        {
            get
            {
                return connectionStrings;
            }
        }

        private Dictionary<string, string> connectionStrings = new Dictionary<string, string>();


        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.selectedConnectionString = listBoxConnections.SelectedValue.ToString();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
