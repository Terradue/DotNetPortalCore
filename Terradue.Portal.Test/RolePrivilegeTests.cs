using System;
using System.IO;
using NUnit.Framework;

namespace Terradue.Portal.Test {

    [TestFixture]
    public class RolePrivilegeTests {

        bool rebuildData = true;

        string connectionString = "Server=localhost; Port=3306; User Id=root; Password=root; Database=TerraduePortalTest";
        Domain domain1, domain2;
        Group group1, group2;
        User user0, user11, user12, user21;
        Role role1, role2;

        IfyContext context;

        [TestFixtureSetUp]
        public void CreateEnvironment() {
            if (rebuildData) {
                AdminTool adminTool = new AdminTool(DataDefinitionMode.Create, Directory.GetCurrentDirectory() + "/../..", null, connectionString);
                adminTool.Process();
            }
            context = new IfyLocalContext(connectionString, false);
            context.Open();

            try {
                if (rebuildData) {
                    
                    domain1 = new Domain(context);
                    domain1.Identifier = "domain-1";
                    domain1.Name = "domain-1";
                    domain1.Store();

                    domain2 = new Domain(context);
                    domain2.Identifier = "domain-2";
                    domain2.Name = "domain-2";
                    domain2.Store();

                    group1 = new Group(context);
                    group1.Identifier = "domain-1_cr_viewers+changers";
                    group1.Store();

                    group2 = new Group(context);
                    group2.Identifier = "global_cr_deleters";
                    group2.Store();

                    user0 = new User(context);
                    user0.Identifier = "user-0";
                    user0.Store();

                    user11 = new User(context);
                    user11.Identifier = "domain-1_cr_viewer+changer";
                    user11.Store();

                    user12 = new User(context);
                    user12.Identifier = "domain-1_cr_viewer+changer+deleter";
                    user12.Store();

                    user21 = new User(context);
                    user21.Identifier = "global_cr_deleter";
                    user21.Store();

                    role1 = new Role(context);
                    role1.Identifier = "cr_view+change";
                    role1.Store();
                    context.Execute(String.Format("INSERT INTO role_priv (id_role, id_priv) SELECT {0}, id FROM priv WHERE identifier IN ('cr-v', 'cr-m');", role1.Id));

                    role2 = new Role(context);
                    role2.Identifier = "cr_delete";
                    role2.Store();
                    context.Execute(String.Format("INSERT INTO role_priv (id_role, id_priv) SELECT {0}, id FROM priv WHERE identifier IN ('cr-d');", role2.Id));

                    ComputingResource cr1 = new GenericComputingResource(context);
                    cr1.Identifier = "cr-1";
                    cr1.Name = "cr-1";
                    cr1.DomainId = domain1.Id;
                    cr1.Store();

                    PublishServer ps1 = new PublishServer(context);
                    ps1.Identifier = "ps-1";
                    ps1.Name = "ps-1";
                    ps1.Hostname = "mytest.host";
                    ps1.DomainId = domain1.Id;
                    ps1.Store();

                    context.Execute(String.Format("INSERT INTO usr_grp (id_usr, id_grp) VALUES ({0}, {2}), ({1}, {2});", user11.Id, user12.Id, group1.Id));
                    context.Execute(String.Format("INSERT INTO usr_grp (id_usr, id_grp) VALUES ({0}, {1});", user21.Id, group2.Id));
                    context.Execute(String.Format("INSERT INTO role_grant (id_grp, id_role, id_domain) VALUES ({0}, {2}, {3});", group1.Id, group2.Id, role1.Id, domain1.Id));
                    context.Execute(String.Format("INSERT INTO role_grant (id_usr, id_role, id_domain) VALUES ({0}, {1}, {2});", user12.Id, role2.Id, domain1.Id));
                    context.Execute(String.Format("INSERT INTO role_grant (id_grp, id_role, id_domain) VALUES ({0}, {1}, NULL);", group2.Id, role2.Id));

                } else {
                    
                    domain1 = Domain.GetInstance(context);
                    domain1.Load("domain-1");
                    domain2 = Domain.GetInstance(context);
                    domain2.Load("domain-2");
                    group1 = Group.FromIdentifier(context, "domain-1_cr_viewers+changers");
                    group2 = Group.FromIdentifier(context, "global_cr_deleters");
                    user0 = User.FromUsername(context, "user-0");
                    user11 = User.FromUsername(context, "domain-1_cr_viewer+changer");
                    user12 = User.FromUsername(context, "domain-1_cr_viewer+changer+deleter");
                    user21 = User.FromUsername(context, "global_cr_deleter");
                    role1 = Role.GetInstance(context);
                    role1.Load("cr_view+change");
                    role2 = Role.GetInstance(context);
                    role2.Load("cr_delete");
                }
            } catch (Exception e) {
                Console.WriteLine("{0} - {1}", e.Message, e.StackTrace);
                throw;
            }
        }

        [Test]
        public void TestDomainRoleGrant() {
            try {
                // Users "domain-1_cr_viewer+changer" and "domain-1_cr_viewer+changer+deleter" have privilege to view computing resources in domain-1
                Assert.IsTrue(Role.DoesUserHavePrivilege(context, user11, domain1, "cr-v"));
                Assert.IsTrue(Role.DoesUserHavePrivilege(context, user12, domain1, "cr-v"));

                // Users "domain-1_cr_viewer+changer" and "domain-1_cr_viewer+changer+deleter" have privilege to change computing resources in domain-1
                Assert.IsTrue(Role.DoesUserHavePrivilege(context, user11, domain1, "cr-m"));
                Assert.IsTrue(Role.DoesUserHavePrivilege(context, user12, domain1, "cr-m"));

                // User "domain-1_cr_viewer+changer" DOES NOT have privilege to delete computing resources in domain-1
                Assert.IsFalse(Role.DoesUserHavePrivilege(context, user11, domain1, "cr-d"));

                // User "domain-1_cr_viewer+changer+deleter" DOES have privilege to delete computing resources in domain-1
                Assert.IsTrue(Role.DoesUserHavePrivilege(context, user12, domain1, "cr-d"));

                // Users "domain-1_cr_viewer+changer" and "domain-1_cr_viewer+changer+deleter" DO NOT have privilege to view, change or delete global computing resources
                Assert.IsFalse(Role.DoesUserHavePrivilege(context, user11, null, "cr-v"));
                Assert.IsFalse(Role.DoesUserHavePrivilege(context, user12, null, "cr-v"));
                Assert.IsFalse(Role.DoesUserHavePrivilege(context, user11, null, "cr-m"));
                Assert.IsFalse(Role.DoesUserHavePrivilege(context, user12, null, "cr-m"));
                Assert.IsFalse(Role.DoesUserHavePrivilege(context, user11, null, "cr-d"));
                Assert.IsFalse(Role.DoesUserHavePrivilege(context, user12, null, "cr-d"));

            } catch (Exception e) {
                Console.WriteLine("{0} - {1}", e.Message, e.StackTrace);
                throw;
            }
        }

        [Test]
        public void TestGlobalRoleGrant() {
            try {
                // User "global_cr_deleter" DOES NOT have privilege to view or change computing resources
                // (global privilege does not allow it)
                Assert.IsFalse(Role.DoesUserHavePrivilege(context, user21, domain1, "cr-v"));
                Assert.IsFalse(Role.DoesUserHavePrivilege(context, user21, domain2, "cr-v"));
                Assert.IsFalse(Role.DoesUserHavePrivilege(context, user21, null, "cr-v"));
                Assert.IsFalse(Role.DoesUserHavePrivilege(context, user21, domain1, "cr-m"));
                Assert.IsFalse(Role.DoesUserHavePrivilege(context, user21, domain2, "cr-m"));
                Assert.IsFalse(Role.DoesUserHavePrivilege(context, user21, null, "cr-m"));

                // User "global_cr_deleter" DOES have privilege to delete computing resources globally
                Assert.IsTrue(Role.DoesUserHavePrivilege(context, user21, domain1, "cr-d"));
                Assert.IsTrue(Role.DoesUserHavePrivilege(context, user21, domain2, "cr-d"));
                Assert.IsTrue(Role.DoesUserHavePrivilege(context, user21, null, "cr-d"));

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
