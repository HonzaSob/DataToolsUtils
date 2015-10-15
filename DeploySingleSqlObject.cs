//------------------------------------------------------------------------------
// <copyright file="DeploySingleSqlObject.cs" company="Microsoft">
//     Copyright (c) Microsoft.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using Microsoft.VisualStudio.Data.Tools.SqlEditor;

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
        public static readonly Guid CommandSet = new Guid("8998fbba-6e4d-42e6-8447-648d814829ed");

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
                    VsShellUtilities.ShowMessageBox(this.ServiceProvider, "Can not deploy file. It is not supported SQL.", null, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                TextDocument td = GetTextDocument(doc);

                if (td != null)
                {
                    string content = GetTextDocumentContent(td);
                    string command = PrepareCommand(content);

                    Microsoft.VisualStudio.Data.Tools.SqlEditor.VSIntegration.ShellConnectionDialog dialog = new Microsoft.VisualStudio.Data.Tools.SqlEditor.VSIntegration.ShellConnectionDialog();
                    
                    dialog.ShowDialog();
                    //dialog.
                    VsShellUtilities.ShowMessageBox(this.ServiceProvider, command, "Command", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                }
            }
            //VsShellUtilities.ShowMessageBox(this.ServiceProvider, text, null, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
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
            string ret = System.Text.RegularExpressions.Regex.Replace(text, "CREATE\\s+(PROCEDURE|FUNCTION|VIEW)", "ALTER $1");
            return ret;
        }
    }
}
