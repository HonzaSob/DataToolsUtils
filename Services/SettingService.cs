using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataToolsUtils.Entities;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Data;

namespace DataToolsUtils.Services
{
    internal class SettingService
    {
        private IVsSettingsManager settingsManager;
        private IVsWritableSettingsStore settingsStore;

        private const string CollectionPathConnStrings = "\\DataToolsUtilsExtension\\ConnStrings";
        private const string CollectionPathRoot = "\\DataToolsUtilsExtension";
        private const string RecordNamePattern = "ConnString{0}";
        private const string DefaultConnString = "DefaultConnString";

        private const int MaxCount = 20;

        internal SettingService(IVsSettingsManager settingsManager)
        {
            if (settingsManager == null)
                throw new ArgumentNullException();
            this.settingsManager = settingsManager;
            settingsManager.GetWritableSettingsStore((uint)__VsSettingsScope.SettingsScope_UserSettings, out settingsStore);
        }

        internal List<ConnectionString> GetConnectionStrings()
        {
            List<ConnectionString> ret = new List<ConnectionString>();

            int i;
            this.settingsStore.CollectionExists(CollectionPathConnStrings, out i);

            if (i == 0)
            {
                return ret;
            }
            else
            {
                for (int j = 0; j < MaxCount; j++)
                {
                    string connectionStringEncrypted;
                    this.settingsStore.GetStringOrDefault(CollectionPathConnStrings,string.Format(RecordNamePattern, j), string.Empty, out connectionStringEncrypted);

                    if (!string.IsNullOrEmpty(connectionStringEncrypted))
                    {
                        string connStringDecrypted = DataProtection.DecryptString(connectionStringEncrypted);
                        ret.Add(new ConnectionString() { ConnectionStringRaw = connStringDecrypted });
                    }
                }
            }
            return ret;
        }

        internal void DeleteConnectionString(ConnectionString connString)
        {
            int i;
            this.settingsStore.CollectionExists(CollectionPathConnStrings, out i);

            if (i == 0)
            {
                return;
            }
            string connStringEncrypted = DataProtection.EncryptString(connString.ConnectionStringRaw);

            int indexToDelete = -1;

            for (int j = 0; j < MaxCount; j++)
            {
                string connectionStringIterator;
                string propName = string.Format(RecordNamePattern, j);
                this.settingsStore.GetStringOrDefault(CollectionPathConnStrings, propName , string.Empty, out connectionStringIterator);

                if (connectionStringIterator == connStringEncrypted)
                {
                    indexToDelete = j;
                    break;
                }
            }

            if (indexToDelete >= 0)
            {
                this.settingsStore.DeleteProperty(CollectionPathConnStrings, string.Format(RecordNamePattern, indexToDelete));
                for (int j = indexToDelete+1; j<MaxCount; j++)
                {
                    string data;
                    string jPropName = string.Format(RecordNamePattern, j);
                    this.settingsStore.PropertyExists(CollectionPathConnStrings, jPropName, out i);
                    if (i == 1)
                    {
                        this.settingsStore.GetString(CollectionPathConnStrings, jPropName, out data);
                        this.settingsStore.SetString(CollectionPathConnStrings, string.Format(RecordNamePattern, j - 1), data);
                    }
                }
            }
        }

        internal void AddConnectionString(ConnectionString connString)
        {
            int i;
            this.settingsStore.CollectionExists(CollectionPathConnStrings, out i);

            if (i == 0)
            {
                this.settingsStore.CreateCollection(CollectionPathConnStrings);
            }

            uint j;
            this.settingsStore.GetPropertyCount(CollectionPathConnStrings, out j);

            string connStringEncrypted = DataProtection.EncryptString(connString.ConnectionStringRaw);
            j++;

            this.settingsStore.SetString(CollectionPathConnStrings, string.Format(RecordNamePattern, j), connStringEncrypted);
        }

        internal void SetDefaultConnectionString(ConnectionString connString)
        {
            int i;
            this.settingsStore.CollectionExists(CollectionPathRoot, out i);

            if (i == 0)
            {
                this.settingsStore.CreateCollection(CollectionPathRoot);
            }
            string connStringEncrypted = DataProtection.EncryptString(connString.ConnectionStringRaw);

            this.settingsStore.SetString(CollectionPathRoot, DefaultConnString, connStringEncrypted);
        }


        internal ConnectionString GetDefaultConnectionString()
        {
            int i;
            this.settingsStore.CollectionExists(CollectionPathRoot, out i);

            if (i == 0)
            {
                return null;
            }

            this.settingsStore.PropertyExists(CollectionPathRoot, DefaultConnString,out i);
            if (i==0)
            {
                return null;
            }

            string connStringEncrypted;
            this.settingsStore.GetString(CollectionPathRoot, DefaultConnString, out connStringEncrypted);

            return new ConnectionString() { ConnectionStringRaw = DataProtection.DecryptString(connStringEncrypted) };
        }
    }
}
