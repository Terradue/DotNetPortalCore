using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Terradue.Portal.Test {
    
    public class BaseTest : AdminTool {

        protected IfyContext context;
        private string connectionString;

        public static string GetConnectionString(string databaseName) {
            StreamReader sr = new StreamReader(Directory.GetCurrentDirectory() + "/../../db-conn.txt");
            string result = sr.ReadToEnd().Trim();
            if (databaseName != null) {
                Match match = Regex.Match(result, "Database=[^;]+");
                if (match.Success) result = result.Replace(match.Value, "Database=" + databaseName);
                else result += "; Database=" + databaseName;
            }
            sr.Close();
            return result;
        }

        [TestFixtureSetUp]
        public virtual void FixtureSetup() {
            connectionString = GetConnectionString(null);

            AdminTool adminTool = new AdminTool(DataDefinitionMode.Create, Directory.GetCurrentDirectory() + "/../..", null, connectionString);
            adminTool.Process();

            try {
                context = IfyContext.GetLocalContext(connectionString, false);
                context.Open();
            } catch (Exception e) {
                Console.Error.WriteLine(e.Message);
                throw;
            }
        }

        [TestFixtureTearDown]
        public virtual void TestFixtureTearDown() {
            context.Close();
            OpenConnection(connectionString);
            Execute("DROP DATABASE $MAIN$;");
            CloseConnection();
        }
    }
}

