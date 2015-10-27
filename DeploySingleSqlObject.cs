//------------------------------------------------------------------------------
// <copyright file="DeploySingleSqlObject.cs" company="Microsoft">
//     Copyright (c) Microsoft.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using System.Data;
using System.Collections.Generic;


namespace DataToolsUtils
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class DeploySingleSqlObject
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("F1732CDA-75A4-4A9E-A7B0-C2F3AF84A31B");


        /// <summary>
        /// OutputWindow Guid
        /// </summary>
        private Guid OutputWindowGuid = new Guid("89682E10-8B5B-4732-BDF3-672E2D1BA7A6");

        private const string OutputWindowTitle = "Deploy Single SQL Object";


        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeploySingleSqlObject"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private DeploySingleSqlObject(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static DeploySingleSqlObject Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new DeploySingleSqlObject(package);
        }


        private IVsWritableSettingsStore _settingsStore = null;

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            Document doc = GetActiveDocument(this.ServiceProvider);
            if (doc != null)
            {
                doc.Activate();

                if (doc.Language != "SQL Server Tools")
                {
                    VsShellUtilities.ShowMessageBox(this.ServiceProvider, "Can not deploy file. It is not supported SQL.", null, OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                TextDocument td = GetTextDocument(doc);

                if (td != null)
                {
                    string content = GetTextDocumentContent(td);
                    string command = PrepareCommand(content);

                    IVsSettingsManager settingsManager = ServiceProvider.GetService(typeof(SVsSettingsManager)) as IVsSettingsManager;
                    
                    settingsManager.GetWritableSettingsStore((uint)__VsSettingsScope.SettingsScope_UserSettings, out _settingsStore);

                    int ret = 0;
                    string SSDTCollection = "\\SSDT\\ConnectionMruList";
                    _settingsStore.CollectionExists(SSDTCollection, out ret);
                    
                    if (ret == 0)
                    {
                        VsShellUtilities.ShowMessageBox(this.ServiceProvider, "There is no connection configured in the SQL Server Object Explorer", null, OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }

                    uint ret2;
                    _settingsStore.GetPropertyCount(SSDTCollection, out ret2);

                    for (uint j = 0; j < ret2; j++)
                    {
                        string propname;
                        _settingsStore.GetPropertyName(SSDTCollection, j, out propname);
                        Console.WriteLine(propname);

                        uint type;
                        _settingsStore.GetPropertyType(SSDTCollection, propname, out type);
                        Console.WriteLine(type);

                        string val;
                        _settingsStore.GetStringOrDefault(SSDTCollection, propname, String.Empty, out val);
                        Console.WriteLine(val);
                        //_settingsStore.GetPropertyCount()
                    }


                    _settingsStore.GetSubCollectionCount(SSDTCollection, out ret2);

                    for (uint j = 0; j<ret2; j++)
                    {
                        string subcollname;
                        _settingsStore.GetSubCollectionName(SSDTCollection, j, out subcollname);
                        Console.WriteLine(subcollname);

                        uint ret3;
                        _settingsStore.GetPropertyCount(SSDTCollection, out ret3);
                        for (uint k = 0; k < ret3; k++)
                        {
                            string propname;
                            _settingsStore.GetPropertyName(SSDTCollection, k, out propname);
                            Console.WriteLine(propname);

                            uint type;
                            _settingsStore.GetPropertyType(SSDTCollection, propname, out type);
                            Console.WriteLine(type);

                            string val;
                            _settingsStore.GetStringOrDefault(SSDTCollection, propname, String.Empty, out val);
                            Console.WriteLine(val);
                            //_settingsStore.GetPropertyCount()
                        }
                    }

                    string connectionName;
                    string connectionString;
                    string ConnectionNamePattern = "ConnectionName{0}";
                    string ConnectionKeyPattern = "Connection{0}";

                    Dictionary<string, string> connections = new Dictionary<string, string>();

                    for (int i = 0; i < 10; i++)
                    {
                        _settingsStore.GetStringOrDefault("\\ConnectionMruList", string.Format(ConnectionNamePattern, i), string.Empty, out connectionName);
                        _settingsStore.GetStringOrDefault("\\ConnectionMruList", string.Format(ConnectionKeyPattern, i), string.Empty, out connectionString);

                        connections.Add(connectionName, connectionString);
                    }


                        //Microsoft.VisualStudio.Data.Tools.SqlEditor.VSIntegration.ShellConnectionDialog dialog = new Microsoft.VisualStudio.Data.Tools.SqlEditor.VSIntegration.ShellConnectionDialog();

                        //dialog.AddServer(new SqlServerType());
                        //Win32WindowWrapper win32WindowWrapper = new Win32WindowWrapper((IntPtr)doc.ActiveWindow.HWnd);
                        IDbConnection connection = null;
                    //UIConnectionInfo connInfo = new UIConnectionInfo();
                    //DialogResult dr = dialog.ShowDialogValidateConnection(win32WindowWrapper, ref connInfo, out connection);
                    //if (dr == DialogResult.OK && connection != null && !string.IsNullOrEmpty(connection.Database) && connection.State == ConnectionState.Open)
                    {
                        //VsShellUtilities.ShowMessageBox(this.ServiceProvider, "EXECUTE ON :"+ connection.ConnectionString +"; Command: " + command, "Command", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                        IVsOutputWindow outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

                        
                        outWindow.CreatePane(ref OutputWindowGuid, OutputWindowTitle, 1, 1);

                        IVsOutputWindowPane customPane;
                        outWindow.GetPane(ref OutputWindowGuid, out customPane);

                        customPane.OutputString("Executing script on "+connection.ConnectionString + "\r\n");

                        connection.ChangeDatabase("UCB"); // TODO

                        var cmd = connection.CreateCommand();
                        cmd.CommandText = command;
                        try
                        {
                            cmd.ExecuteNonQuery();
                            customPane.OutputString("Deployed successfully\r\n");
                        }
                        catch (Exception ex)
                        {
                            string error = "\r\nError during deployment: " + ex.ToString() + "\r\n";
                            customPane.OutputString(error);
                            VsShellUtilities.ShowMessageBox(this.ServiceProvider, error, "Error", OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                        }
                        customPane.Activate();
                    }
                    //else
                    //{
                    //    VsShellUtilities.ShowMessageBox(this.ServiceProvider, command, "Error during connecting", OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    //}
                }
            }
            else
            {
                VsShellUtilities.ShowMessageBox(this.ServiceProvider, "There is no active tab which could be processed", null, OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }
        }

        private Document GetActiveDocument(IServiceProvider serviceProvider)
        {
            DTE dte = (DTE)serviceProvider.GetService(typeof(DTE));
            return dte.ActiveDocument;
        }

        private TextDocument GetTextDocument(Document document)
        {
            object ret = document.Object("TextDocument");
            if (ret != null) 
                return ret as TextDocument;
            return null;
        }

        private string GetTextDocumentContent(TextDocument textDocument)
        {
            EditPoint start = textDocument.StartPoint.CreateEditPoint();
            EditPoint end = textDocument.EndPoint.CreateEditPoint();

            string text = start.GetText(end);

            return text;
        }

        private string PrepareCommand(string text)
        {
            //string ret = System.Text.RegularExpressions.Regex.Replace(text, "CREATE\\s+(PROCEDURE|FUNCTION|VIEW)\\s+([^ (@\n]+)", "IF EXISTS");
            string ret = System.Text.RegularExpressions.Regex.Replace(text, "CREATE\\s+(PROCEDURE|FUNCTION|VIEW)", "ALTER $1");

            return ret;
        }
    }
}
