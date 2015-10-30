using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using DataToolsUtils.Forms;
using System.Windows.Forms;
using System.Data.SqlClient;
using DataToolsUtils.Entities;
using DataToolsUtils.Services;

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
                    IVsSettingsManager settingsManager = ServiceProvider.GetService(typeof(SVsSettingsManager)) as IVsSettingsManager;                                      

                    ConnectionDialog dialog = new ConnectionDialog(settingsManager);
                    DialogResult dr = dialog.ShowDialog();
                    ConnectionString connectionString = dialog.SelectedConnectionString;

                    if (dr == DialogResult.OK && connectionString != null)
                    {                    
                        IVsOutputWindow outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

                        outWindow.CreatePane(ref OutputWindowGuid, OutputWindowTitle, 1, 1);

                        IVsOutputWindowPane customPane;
                        outWindow.GetPane(ref OutputWindowGuid, out customPane);

                        customPane.OutputString(string.Format("\r\n{0}: Deploying script \"{1}\" to \"{2}\"\r\n",DateTime.Now.ToString(),doc.Name,connectionString));
                        string command = "";
                        IDbTransaction tran = null;
                        try
                        {
                            using (IDbConnection connection = new SqlConnection(connectionString.ConnectionStringRaw))
                            {
                                connection.Open();
                                string content = GetTextDocumentContent(td);
                                List<string> commands = new ScriptGeneratorService().GenerateDropAndCreateScripts(content);

                                tran = connection.BeginTransaction();

                                foreach (string it in commands)
                                {
                                    command = it;
                                    if (!string.IsNullOrEmpty(command))
                                    {
                                        IDbCommand cmd = connection.CreateCommand();
                                        cmd.CommandText = command;
                                        cmd.Transaction = tran;
                                        cmd.ExecuteNonQuery();
                                    }
                                }

                                tran.Commit();
                                connection.Close();
                                customPane.OutputString("Deployed successfully\r\n");
                            }
                        }
                        catch (ScriptGeneratorService.UnsupportedObjectException exc)
                        {
                            if (tran != null && tran.Connection.State != ConnectionState.Closed)
                                tran.Rollback();
                            string message = "ALTER script could not be created. The object is not supported. Supported types of objects: " + exc.SupportedObjectTypes;
                            customPane.OutputString("Deployment stopped");
                            customPane.OutputString(message);
                            VsShellUtilities.ShowMessageBox(this.ServiceProvider, message, null, OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                        }
                        catch (Exception ex)
                        {
                            if (tran != null && tran.Connection.State != ConnectionState.Closed)
                                tran.Rollback();

                            string error = "\r\nError during deployment: " + ex.ToString() + "\r\n";
                            customPane.OutputString(command);
                            customPane.OutputString(error);
                            VsShellUtilities.ShowMessageBox(this.ServiceProvider, error, "Error", OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                        }
                        customPane.Activate();
                    }
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
    }
}
