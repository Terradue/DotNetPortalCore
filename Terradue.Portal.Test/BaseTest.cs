using System;
using NUnit.Framework;
using System.IO;

namespace Terradue.Portal.Test {
    public class BaseTest : AdminTool {

        protected IfyContext context;
        string connectionString = "Server=localhost; Port=3306; User Id=root; Database=TerraduePortalTest";

        [TestFixtureSetUp]
        public virtual void FixtureSetup() {

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

