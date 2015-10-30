using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.IO;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Dac;


namespace DataToolsUtils.Services
{
    internal class ScriptGeneratorService
    {
        public class UnsupportedObjectException : Exception
        {
            public string SupportedObjectTypes
            {
                get
                {
                    return "PROCEDURE, VIEW, FUNCTION";
                }
            }
        }

        /// <summary>
        /// Function creates DROP and CREATE script for object specified
        /// </summary>
        /// <param name="sql">original create script of object</param>
        public string GenerateDropAndCreateScript(string sql)
        {
            
            StringBuilder commandBuilder = new StringBuilder();

            TSql120Parser parser = new TSql120Parser(true);
            IList<ParseError> errors = new List<ParseError>();
            TextReader reader = new StringReader(sql);
            TSqlScript script = parser.Parse(reader, out errors) as TSqlScript;

            bool isSupported = false;

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

                        if (it.TokenType == TSqlTokenType.Procedure
                            || it.TokenType == TSqlTokenType.Function
                            || it.TokenType == TSqlTokenType.View
                            )
                        {
                            isSupported = true;
                        }
                    }
                }
            }

            if (!isSupported)
                throw new UnsupportedObjectException();

            return commandBuilder.ToString();

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
}
