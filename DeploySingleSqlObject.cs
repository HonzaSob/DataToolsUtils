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
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            string title = "DeploySingleSqlObject";

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}

/*
'Version: 3.5
'Modified: 2012-05-28 - sobotkaja - Zmena funkce Deploy
'Modified: 2012-05-28 - pokryvkada - uprava grantovani prav pro ostatni role, pridani alternativni repository
'Modified: 2012-05-28 - pokryvkada - vylepseni recreate, grantovani odstraneno pro DB4CHeck
'Modified: 2012-05-29 - pokryvkada - oprava bugu pri pokusu o disconnect okna, ktere neni connectene
'Modified: 2012-06-05 - sobotkaja - oprava Statistics, tabulkove funkce se jmenem tabulky
'Modified: 2013-03-14 - sobotkaja - uprava db
'Modified: 2013-05-21 - sobotkaja - podpora changescripts

Imports System
Imports EnvDTE
Imports EnvDTE80
Imports EnvDTE90
Imports EnvDTE90a
Imports EnvDTE100
Imports System.Diagnostics
Imports System.Windows.Forms

Public Module Deployment

    Sub ToggleResultPane()
        DTE.ExecuteCommand("Data.SqlEditorToggleResultsPane")
    End Sub

    Sub Disconect_and_Execute()

        On Error Resume Next

        DTE.ExecuteCommand("Data.SqlEditorDisconnect")

        DTE.ExecuteCommand("Data.SqlEditorExecuteSql")

    End Sub

    Sub Deploy()

        Dim doc As EnvDTE.Document = DTE.ActiveDocument
        doc.Activate()

        DTE.ExecuteCommand("Edit.FindinFiles")
        DTE.Find.Target = vsFindTarget.vsFindTargetCurrentDocument
        DTE.Find.MatchInHiddenText = False
        DTE.Find.PatternSyntax = vsFindPatternSyntax.vsFindPatternSyntaxRegExpr
        DTE.Find.FindWhat = "USE (COMMON|CONTRACT|CLAIM|BANKING|STATISTICS|DB4CHECK)"
        DTE.Find.Action = vsFindAction.vsFindActionFind
        DTE.Find.MatchCase = False
        DTE.Find.MatchWholeWord = False
        DTE.Find.Backwards = False
        If (DTE.Find.Execute() = vsFindResult.vsFindResultNotFound) Then
            doc.Activate()
            Prepare()
        End If

        'pro pripady, ze okno je disconnected
        On Error Resume Next
        DTE.ExecuteCommand("Data.SqlEditorDisconnect")

        DTE.ExecuteCommand("Data.SqlEditorConnect")


        DTE.ExecuteCommand("Data.SqlEditorExecuteSql")



        doc.Selection.StartOfDocument()
        DTE.ExecuteCommand("Edit.Cut")
        DTE.ExecuteCommand("Edit.Cut")
        DTE.ExecuteCommand("Edit.Cut")
        DTE.ExecuteCommand("Edit.Cut")

        doc.Selection.EndOfDocument()

        DTE.ExecuteCommand("Edit.Cut")
        doc.Selection.LineUp()
        DTE.ExecuteCommand("Edit.Cut")
        doc.Selection.LineUp()
        DTE.ExecuteCommand("Edit.Cut")

        doc.Save()

    End Sub





    Sub Prepare()

        DTE.ActiveDocument.Save()

        DTE.ExecuteCommand("Edit.FindinFiles")
        DTE.Find.Target = vsFindTarget.vsFindTargetCurrentDocument
        DTE.Find.MatchInHiddenText = False
        DTE.Find.PatternSyntax = vsFindPatternSyntax.vsFindPatternSyntaxRegExpr
        DTE.Find.FindWhat = "CREATE:Wh+(PROCEDURE|FUNCTION|VIEW):Wh+[^ (@\n]+"
        DTE.Find.Action = vsFindAction.vsFindActionFind
        DTE.Find.MatchCase = False
        DTE.Find.MatchWholeWord = False
        DTE.Find.Backwards = False
        If (DTE.Find.Execute() = vsFindResult.vsFindResultNotFound) Then
            Throw New System.Exception("vsFindResultNotFound")
        End If

        Dim name As String
        name = DTE.ActiveDocument.Selection.Text

        DTE.ActiveDocument.Selection.StartOfDocument()

        Dim doc As EnvDTE.Document = DTE.ActiveDocument



        ''DATABASE

        ''Dim form As Form = New Form()
        ''Dim cb As ComboBox = New ComboBox()
        ''form.Controls.Add(cb)
        ''cb.Items.Add("COMMON")
        ''cb.Items.Add("CONTRACT")
        ''cb.Items.Add("CLAIM")
        ''cb.Items.Add("BANKING")
        ''cb.Items.Add("STATISTICS")
        ''cb.Items.Add("DB4CHECK")

        ''form.ShowDialog()

        ''Dim db As String = cb.SelectedItem.ToString()

        '''form.Dispose()

        Dim fileName As String = DTE.ActiveDocument.FullName

        Dim db As String = ""
        If fileName.Contains("COMMON") Then
            db = "COMMON"
        End If
        If fileName.Contains("CONTRACT") Then
            db = "CONTRACT"
        End If
        If fileName.Contains("CLAIM") Then
            db = "CLAIM"
        End If
        If fileName.Contains("BANKING") Then
            db = "BANKING"
        End If
        If fileName.Contains("DB4CHECK") Then
            db = "DB4CHECK"
        End If
        If fileName.Contains("STATISTICS") Then
            db = "STATISTICS"
        End If

        'If fileName.Contains("WINGS_DB_SERVER\") Then
        '    db = fileName.Substring(fileName.IndexOf("WINGS_DB_SERVER\"))
        '    db = db.Replace("WINGS_DB_SERVER\", "")
        'Else 'fileName.Contains("WINGS_DB_2010\")
        '    db = fileName.Substring(fileName.IndexOf("WINGS_DB_2010\DEV\"))
        '    db = db.Replace("WINGS_DB_2010\DEV\", "")
        'End If

        db = "[" + db + "]"

        'priprava recreate politiky
        name = name.ToLower().Replace("create", "drop")

        Dim objectName As String = name.Replace("drop procedure", "").Replace("drop function", "")

        Dim procedureName As String = objectName.Substring(objectName.IndexOf(".") + 1).Replace("[", "").Replace("]", "")

        Dim IfExistsString As String = "IF EXISTS(SELECT 1 FROM " + db + ".sys.objects WHERE Name='" + procedureName + "')"




        doc.Activate()
        doc.Selection.NewLine()
        doc.Selection.StartOfDocument()

        doc.Selection.Text = "USE " + db
        doc.Selection.NewLine()
        doc.Selection.Text = "GO"
        doc.Selection.NewLine()

        'DROP
        doc.Selection.Text = IfExistsString + " " + name
        doc.Selection.NewLine()
        doc.Selection.Text = "GO"

        doc.Selection.EndOfDocument()


        Dim isTableValued As Boolean = False

        DTE.SuppressUI = True
        DTE.ExecuteCommand("Edit.FindinFiles")
        DTE.Find.Target = vsFindTarget.vsFindTargetCurrentDocument
        DTE.Find.MatchInHiddenText = False
        DTE.Find.PatternSyntax = vsFindPatternSyntax.vsFindPatternSyntaxRegExpr
        DTE.Find.FindWhat = "RETURNS:Wh+([@][A-Za-z]*)*:Wh*TABLE"
        DTE.Find.Action = vsFindAction.vsFindActionFind
        DTE.Find.MatchCase = False
        DTE.Find.MatchWholeWord = False
        DTE.Find.Backwards = False


        If (DTE.Find.Execute() = vsFindResult.vsFindResultFound) Then
            isTableValued = True
        Else
            isTableValued = False
        End If

        doc.Selection.EndOfDocument()
        doc.Selection.NewLine() ' nepridava newline po RETURN 0

        Dim IsWithGrant As Boolean = True

        If fileName.Contains(".view.") Then
            IsWithGrant = False
        End If

        If db = "DB4CHECK" Then
            IsWithGrant = False
        End If
       

            If IsWithGrant = True Then

                doc.Selection.Text = "GO"
                doc.Selection.NewLine()

                If name.Contains("premiumimport") Or name.Contains("goodsimport") Or name.Contains("claimimport") Then
                    doc.Selection.Text = "GRANT" + " " + IIf(isTableValued, "SELECT", "EXECUTE") + " " + "ON" + " " + objectName + " " + "TO role_public"
                ElseIf name.Contains("kofax") Then
                    doc.Selection.Text = "GRANT" + " " + IIf(isTableValued, "SELECT", "EXECUTE") + " " + "ON" + " " + objectName + " " + "TO role_kofax"
				ElseIf fileName.Contains("IWingsImport")
					doc.Selection.Text = "GRANT" + " " + IIf(isTableValued, "SELECT", "EXECUTE") + " " + "ON" + " " + objectName + " " + "TO role_iwings_import"
                Else
                    doc.Selection.Text = "GRANT" + " " + IIf(isTableValued, "SELECT", "EXECUTE") + " " + "ON" + " " + objectName + " " + "TO role_public"
                End If

                doc.Selection.NewLine()
                doc.Selection.Text = "GO"

            Else
                'toto je FAKE, protoze deploy funkce ke konci odsekava tri radky pryc, jinymi slovy grantovani se po deployi odmaze
                doc.Selection.Text = "GO"
                doc.Selection.NewLine()
                doc.Selection.Text = "GO"
                doc.Selection.NewLine()
                doc.Selection.Text = "GO"
            End If

            doc.Activate()
            'doc.Save()
    End Sub

    Sub Execute()
        On Error Resume Next

        DTE.ExecuteCommand("Data.SqlEditorExecuteSql")
    End Sub

    Sub DeploySqlcmd()

        Dim fileName As String = DTE.ActiveDocument.FullName

        Dim command As String = String.Format("sqlcmd {0}", fileName)

        Shell(command, AppWinStyle.NormalNoFocus, True)

    End Sub
End Module
    */
