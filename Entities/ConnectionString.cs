using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace DataToolsUtils.Entities
{
    class ConnectionString
    {
        private SqlConnectionStringBuilder connectionStringBuilder = new SqlConnectionStringBuilder();

        private string connectionString = null;

        internal string ConnectionStringRaw
        {
            get
            {
                return connectionString;
            }

            set
            {
                connectionStringBuilder.ConnectionString = value;
                connectionString = value;
            }
        }

        internal string Label
        {
            get
            {
                return (this.connectionStringBuilder.DataSource + "." + this.connectionStringBuilder.InitialCatalog).Trim('.');
            }
        }
    }
}
