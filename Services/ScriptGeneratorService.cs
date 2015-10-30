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

        private const string DefaultDatabaseSchema = "dbo";

        private const string BatchDelimiter = "\r\nGO\r\n";

        /// <summary>
        /// Function creates DROP and CREATE script for object specified
        /// </summary>
        /// <param name="sql">original create script of object</param>
        public List<string> GenerateDropAndCreateScripts(string sql)
        {
            List<string> ret = new List<string>();

            //using parser -- issue with more CREATE tokens

            //TSql120Parser parser = new TSql120Parser(true);
            //IList<ParseError> errors = new List<ParseError>();
            //TextReader reader = new StringReader(sql);
            //TSqlScript script = parser.Parse(reader, out errors) as TSqlScript;
            //int tokenCount = 1;


            //foreach (TSqlBatch batch in script.Batches)
            //{
            //    foreach (TSqlStatement statement in batch.Statements)
            //    {
            //        foreach (var it in statement.ScriptTokenStream)
            //        {
            //            if (it.TokenType == TSqlTokenType.Create && tokenCount == 1)
            //            {
            //                commandBuilder.Append("ALTER");
            //                tokenCount++;
            //            }
            //            else
            //            {
            //                commandBuilder.Append(it.Text);
            //            }

            //            if (it.TokenType == TSqlTokenType.Procedure
            //                || it.TokenType == TSqlTokenType.Function
            //                || it.TokenType == TSqlTokenType.View
            //                )
            //            {
            //                isSupported = true;
            //            }
            //        }
            //    }
            //}

            //using TSqlModel

            TSqlModelOptions options = new TSqlModelOptions();
            TSqlModel model = new TSqlModel(SqlServerVersion.Sql120, options);
            model.AddObjects(sql);
            string dropScript = "";

            foreach (var obj in model.GetObjects(DacQueryScopes.UserDefined, null))
            {
                string script;
                if (obj.TryGetScript(out script))
                {
                    //change drop script to EXISTS(SELECT OBJECT_ID('"+obj.Name+"'))???
                    
                    if (obj.ObjectType.Name == View.TypeClass.Name)
                    {
                        dropScript = string.Format("IF EXISTS(SELECT * FROM INFORMATION_SCHEMA.VIEWS WHERE TABLE_SCHEMA = '{0}' AND TABLE_NAME = '{1}') \r\nBEGIN\r\n DROP VIEW [{0}].[{1}]; \r\nEND\r\n",
                            GetSchemaName(obj.Name),
                            GetObjectName(obj.Name)
                            );
                    }
                    else if (obj.ObjectType.Name == ScalarFunction.TypeClass.Name || obj.ObjectType.Name == TableValuedFunction.TypeClass.Name)
                    {
                        dropScript = string.Format("IF EXISTS(SELECT * FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_SCHEMA = '{0}' AND ROUTINE_NAME = '{1}' AND ROUTINE_TYPE = 'FUNCTION') \r\nBEGIN\r\n DROP FUNCTION [{0}].[{1}]; \r\nEND\r\n",
                            GetSchemaName(obj.Name),
                            GetObjectName(obj.Name)
                            );
                    }
                    else if (obj.ObjectType.Name == Procedure.TypeClass.Name)
                    {
                        dropScript = string.Format("IF EXISTS(SELECT * FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_SCHEMA = '{0}' AND ROUTINE_NAME = '{1}' AND ROUTINE_TYPE = 'PROCEDURE') \r\nBEGIN\r\n DROP PROCEDURE [{0}].[{1}]; \r\nEND\r\n",
                            GetSchemaName(obj.Name),
                            GetObjectName(obj.Name)
                            );
                    }
                    else if (obj.ObjectType.Name == ExtendedProperty.TypeClass.Name)
                    {
                        dropScript = "";
                    }
                    else
                        throw new UnsupportedObjectException();
                    if (!string.IsNullOrEmpty(dropScript))
                        ret.Add(dropScript);
                    ret.Add(script);
                }
            }

            return  ret;
            
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

            //using REGEX
            //    //string ret = System.Text.RegularExpressions.Regex.Replace(text, "CREATE\\s+(PROCEDURE|FUNCTION|VIEW)\\s+([^ (@\n]+)", "IF EXISTS");
            //    string pattern = "CREATE\\s+(PROCEDURE|FUNCTION|VIEW)\\s+((\\[[^\\]]+\\]|[^.]+))?[.]?((\\[[^\\]]+\\]|[^\\s]+))";

            //    if (!Regex.IsMatch(text, pattern))
            //        outputWindow.OutputString(string.Format("Processed command does not contain pattern \"{0}\". Executing anyway.",pattern));

            //    string ret = Regex.Replace(text, pattern, "ALTER $1");
        }

        private string GetSchemaName(ObjectIdentifier identifier)
        {
            if (identifier.Parts.Count > 1)
            {
                return identifier.Parts[0];
            }
            else
                return DefaultDatabaseSchema;
        }

        private string GetObjectName(ObjectIdentifier identifier)
        {
            if (identifier.Parts.Count > 1)
            {
                return identifier.Parts[1];
            }
            else
                return identifier.Parts[0];
        }
    }
}
