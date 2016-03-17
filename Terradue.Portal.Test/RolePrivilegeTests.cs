using System;
using NUnit.Framework;

namespace Terradue.Portal.Test {

    [TestFixture]
    public class RolePrivilegeTests {

        [TestFixtureSetUp]
        public void CreateEnvironment() {

/*            string connectionString = "Server=localhost; Port=3306; User Id=root; Database=TerraduePortalTest";
            AdminTool adminTool = new AdminTool();
            adminTool.Process();*/

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

