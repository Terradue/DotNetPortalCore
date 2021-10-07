using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Terradue.Portal.Test {
    
    public class BaseTest {

        protected IfyContext context;
        private string connectionString;

        protected string DatabaseName { get; set; }
        protected string BaseDirectory { get; set; }

        public string GetConnectionString() {
            StreamReader sr = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), "db-conn.txt"));
            string result = sr.ReadToEnd().Trim();
            sr.Close();
            bool replaceDatabaseName = (DatabaseName != null);
            Match match = Regex.Match(result, "Database=([^;]+)");
            if (replaceDatabaseName) {
                if (match.Success) result = result.Replace(match.Value, "Database=" + DatabaseName);
                else result += "; Database=" + DatabaseName;
            } else {
                if (match.Success) DatabaseName = match.Groups[1].Value;
                else throw new Exception("No database name specified");
            }
            return result;
        }

        [OneTimeSetUp]
        public virtual void FixtureSetup() {
            connectionString = GetConnectionString();
            if (BaseDirectory == null) BaseDirectory = Directory.GetCurrentDirectory() + "/../..";

            AdminTool adminTool = new AdminTool(DataDefinitionMode.Create, BaseDirectory, null, connectionString);

            try {
                adminTool.Process();
                context = IfyContext.GetLocalContext(connectionString, false);
                context.Open();
                context.LoadConfiguration();
            } catch (Exception e) {
                Console.Error.WriteLine(e.Message);
                throw;
            }
        }

        [TearDown]
        public virtual void FixtureTearDown() {

            context.Execute(String.Format("DROP DATABASE {0};", DatabaseName));
            context.Close();
        }
    }
}

