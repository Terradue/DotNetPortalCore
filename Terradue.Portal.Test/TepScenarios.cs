using System;
using System.IO;
using NUnit.Framework;

namespace Terradue.Portal.Test {

    [TestFixture]
    public class TepScenarios {
        
        bool rebuildData = true;
        IfyContext context;

        Domain moveDomain;
        User sarah, marco, jean, sofia, emma;
        Role expertRole, clusterProvisionRole;
        Series xstSeries;

        [TestFixtureSetUp]
        public void CreateEnvironment() {
            string connectionString = BaseTest.GetConnectionString("scenariodb");
            if (rebuildData) {
                AdminTool adminTool = new AdminTool(DataDefinitionMode.Create, Directory.GetCurrentDirectory() + "/../..", null, connectionString);
                adminTool.Process();
            }
            context = new IfyLocalContext(connectionString, false);
            context.Open();

            try {
                if (rebuildData) {

                    context.AccessLevel = EntityAccessLevel.Administrator;

                    moveDomain = new Domain(context);
                    moveDomain.Identifier = "MOVE";
                    moveDomain.Name = "MOVE domain";
                    moveDomain.Store();

                    sarah = new User(context);
                    sarah.Identifier = "sarah";
                    sarah.FirstName = "Sarah";
                    sarah.Store();

                    marco = new User(context);
                    marco.Identifier = "marco";
                    marco.FirstName = "Marco";
                    marco.LastName = "Rossi";
                    marco.Store();

                    jean = new User(context);
                    jean.Identifier = "jean";
                    jean.FirstName = "Jean";
                    jean.LastName = "Dupont";
                    jean.Store();

                    sofia = new User(context);
                    sofia.Identifier = "sofia";
                    sofia.FirstName = "Sofia";
                    sofia.LastName = "Rodriguez";
                    sofia.Store();

                    emma = new User(context);
                    emma.Identifier = "emma";
                    emma.FirstName = "Emma";
                    emma.LastName = "Muller";
                    emma.Store();

                    expertRole = new Role(context);
                    expertRole.Identifier = "expert-role";
                    expertRole.Name = "Expert role";
                    expertRole.Store();
                    //expertRole.AssignUsersOrGroups

                } else {
                    
                }
            } catch (Exception e) {
                Console.WriteLine("{0} - {1}", e.Message, e.StackTrace);
                throw;
            }
                    }

        [TestFixtureTearDown]
        public void DestroyEnvironment() {
            context.Close();
        }

    }
}

