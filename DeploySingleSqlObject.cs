//------------------------------------------------------------------------------
// <copyright file="DeploySingleSqlObject.cs" company="Microsoft">
//     Copyright (c) Microsoft.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio.Data;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Text.RegularExpressions;
using DataToolsUtils.Forms;
using System.Windows.Forms;
using System.Data.SqlClient;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Dac;
using System.IO;
using DataToolsUtils.Entities;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Text;

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

                        customPane.OutputString("Executing script on " + connectionString + "\r\n");

                        try
                        {
                            using (IDbConnection connection = new SqlConnection(connectionString.ConnectionStringRaw))
                            {
                                connection.Open();
                                string content = GetTextDocumentContent(td);

                                //TSqlModelOptions options = new TSqlModelOptions();
                                //TSqlModel model = new TSqlModel(SqlServerVersion.Sql120,options);
                                //model.AddObjects(content);

                                //foreach (var obj in model.GetObjects(DacQueryScopes.UserDefined, null))
                                //{
                                //    string commandIt;
                                //    Sql120ScriptGenerator gen = new Sql120ScriptGenerator();

                                //    TSqlScript ast;
                                //    if (obj.TryGetAst(out ast))
                                //    {
                                //        ast.Accept()
                                //        gen.GenerateScript(ast, out commandIt);

                                //        command += "GO\r\n" + commandIt;
                                //    }
                                //}

                                StringBuilder commandBuilder = new StringBuilder();

                                TSql120Parser parser = new TSql120Parser(true);
                                IList<ParseError> errors = new List<ParseError>();
                                TextReader reader = new StringReader(content);
                                TSqlScript script = parser.Parse(reader,out errors) as TSqlScript;

                                foreach (TSqlBatch batch in script.Batches)
                                {
                                    foreach (TSqlStatement statement in batch.Statements)
                                    {
                                        foreach (var it in statement.ScriptTokenStream)
                                        {
                                            if (it.TokenType == TSqlTokenType.Create)
                                            {
                                                commandBuilder.Append("ALTER");
                                            }
                                            else
                                            {
                                                commandBuilder.Append(it.Text);
                                            }
                                        }
                                    }
                                }

                                string command = commandBuilder.ToString();

                                if (!string.IsNullOrEmpty(command))
                                {
                                    IDbCommand cmd = connection.CreateCommand();
                                    cmd.CommandText = command;
                                    cmd.ExecuteNonQuery();
                                }
                                // toto vyhazi milion erroru, proptoze v modelu chybi tabulky
                                //var errors = model.Validate();
                                //if (errors != null && errors.Count > 0)
                                //{
                                //    string result = "";
                                //    foreach (var it in errors)
                                //    {
                                //        result += it.Message + "\n";
                                //    }
                                //    VsShellUtilities.ShowMessageBox(this.ServiceProvider, result, null, OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                                //    return;
                                //}

                                //string databaseName = connectionString.InitialCatalog;

                                //DacServices svc = new DacServices(connectionString.ConnectionStringRaw);
                                //MemoryStream stream = new MemoryStream();
                                //svc.Extract(stream, databaseName, "WorkingApp", new Version(1, 0), extractOptions: new DacExtractOptions());
                                //DacPackage dacpac = DacPackage.Load(stream);
                                //svc.Deploy(dacpac, databaseName, true, new DacDeployOptions() { BlockOnPossibleDataLoss = true, DropObjectsNotInSource = false });

                                connection.Close();
                                customPane.OutputString("Deployed successfully\r\n");
                            }
                        }
                        catch (Exception ex)
                        {
                            string error = "\r\nError during deployment: " + ex.ToString() + "\r\n";
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

        //private string PrepareCommand(string text, IVsOutputWindowPane outputWindow) // do budoucna predavat log jinak
        //{
        //    //string ret = System.Text.RegularExpressions.Regex.Replace(text, "CREATE\\s+(PROCEDURE|FUNCTION|VIEW)\\s+([^ (@\n]+)", "IF EXISTS");
        //    string pattern = "CREATE\\s+(PROCEDURE|FUNCTION|VIEW)\\s+((\\[[^\\]]+\\]|[^.]+))?[.]?((\\[[^\\]]+\\]|[^\\s]+))";

        //    if (!Regex.IsMatch(text, pattern))
        //        outputWindow.OutputString(string.Format("Processed command does not contain pattern \"{0}\". Executing anyway.",pattern));

        //    string ret = Regex.Replace(text, pattern, "ALTER $1");

        //    return ret;
        //}
    }
}
