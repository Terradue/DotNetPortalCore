using System;
using System.IO;
using NUnit.Framework;

namespace Terradue.Portal.Test {

    [TestFixture]
    public class RolePrivilegeTests {

        string connectionString = "Server=localhost; Port=3306; User Id=root; Database=TerraduePortalTest";

        [TestFixtureSetUp]
        public void CreateEnvironment() {

            AdminTool adminTool = new AdminTool(DataDefinitionMode.Create, Directory.GetCurrentDirectory() + "/../..", null, connectionString);
            try {
                adminTool.Process();
            } catch (Exception e) {
                Console.WriteLine(e.Message + " " + e.StackTrace);
            }

        }


        [Test]
        public void Test() {
            Assert.That(true);
        }

        [TestFixtureTearDown]
        public void DestroyEnvironment() {
        }

    }

}

