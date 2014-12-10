using System;
using NUnit.Framework;
using System.IO;

namespace Terradue.Portal.Test {
    public class BaseTest : AdminTool {

        protected IfyContext context;
        string connectionString = "Server=localhost; Port=3306; User Id=root; Database=TerraduePortalTest";

        [TestFixtureSetUp]
        public virtual void FixtureSetup() {

            create = true;
            AfterFailureCheckpoint = true;

            dbMainSchema = "TerraduePortalTest";
            currentSchema = dbMainSchema;
            Verbose = false;
            siteRootDir = "../..";

            try {
                OpenConnection(connectionString);
                schemaExists = true;
            } catch (Exception e) {
                if (!e.Message.Contains("Unknown database"))
                    throw e;
            }

            try {
                CreateSchemas();
                CheckState();
            } catch (Exception e) {
                Console.Error.WriteLine(e.Message);
                throw e;
            }

            CoreModule core = new CoreModule(this, String.Format("{0}/../../core", Directory.GetCurrentDirectory()));
            core.Install();

            CloseConnection();

            try {
                context = IfyContext.GetLocalContext(connectionString, false);
                context.Open();
            } catch (Exception e) {
                Console.Error.WriteLine(e.Message);
                throw e;
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

