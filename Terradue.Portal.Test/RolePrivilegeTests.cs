using System;
using System.IO;
using NUnit.Framework;

namespace Terradue.Portal.Test {

    [TestFixture]
    public class RolePrivilegeTests {

        bool rebuildData = true;
        string connectionString;

        IfyContext context;

        Domain domain1, domain2;
        Group group1, group2;
        User user0, user11, user12, user21, user31;
        Role role1, role2, role3;

        Series sharedSeries, unsharedSeries;
        User shareCreator, shareReceiver;
        Role seriesShareRole;
        Domain seriesShareDomain;

        [TestFixtureSetUp]
        public void CreateEnvironment() {
            connectionString = BaseTest.GetConnectionString(null);
            if (rebuildData) {
                AdminTool adminTool = new AdminTool(DataDefinitionMode.Create, Directory.GetCurrentDirectory() + "/../..", null, connectionString);
                adminTool.Process();
            }
            context = new IfyLocalContext(connectionString, false);
            context.Open();

            try {
                if (rebuildData) {

                    context.AccessLevel = EntityAccessLevel.Administrator;

                    domain1 = new Domain(context);
                    domain1.Identifier = "domain-1";
                    domain1.Name = "domain-1";
                    domain1.Store();

                    domain2 = new Domain(context);
                    domain2.Identifier = "domain-2";
                    domain2.Name = "domain-2";
                    domain2.Store();

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

                    group1 = new Group(context);
                    group1.Identifier = "domain-1_cr_viewers+changers";
                    group1.Store();
                    group1.AssignUsers(new int[] {user11.Id, user12.Id});

                    group2 = new Group(context);
                    group2.Identifier = "global_cr_deleters";
                    group2.Store();
                    group2.AssignUsers(new int[] {user21.Id});

                    user31 = new User(context);
                    user31.Identifier = "global_service_admin";
                    user31.Store();

                    role1 = new Role(context);
                    role1.Identifier = "cr_view+change";
                    role1.Store();
                    role1.IncludePrivileges(Privilege.Get(EntityType.GetEntityType(typeof(ComputingResource)), new EntityOperationType[] {EntityOperationType.Search, EntityOperationType.View, EntityOperationType.Change})); 
                    role1.GrantToGroups(new int[] {group1.Id}, domain1.Id);

                    role2 = new Role(context);
                    role2.Identifier = "cr_delete";
                    role2.Store();
                    role2.IncludePrivilege(Privilege.Get(EntityType.GetEntityType(typeof(ComputingResource)), EntityOperationType.Delete)); 
                    role2.GrantToUsers(new int[] {user12.Id}, domain1.Id);
                    role2.GrantToGroups(new int[] {group2.Id}, 0);

                    role3 = new Role(context);
                    role3.Identifier = "service_all";
                    role3.Store();
                    role3.IncludePrivileges(Privilege.Get(EntityType.GetEntityType(typeof(Service)))); 
                    role3.GrantToUsers(new int[] {user31.Id}, 0);

                    ComputingResource cr1 = new GenericComputingResource(context);
                    cr1.Identifier = "cr-1";
                    cr1.Name = "cr-1";
                    cr1.Domain = domain1;
                    cr1.Store();
                    cr1.GrantPermissionsToUsers(new int[] {user12.Id});

                    PublishServer ps1 = new PublishServer(context);
                    ps1.Identifier = "ps-1";
                    ps1.Name = "ps-1";
                    ps1.Hostname = "mytest.host";
                    ps1.Domain = domain1;
                    ps1.Store();

                    seriesShareDomain = new Domain(context);
                    seriesShareDomain.Identifier = "series-share";
                    seriesShareDomain.Name = "series-share";
                    seriesShareDomain.Store();

                    shareCreator = new User(context);
                    shareCreator.Identifier = "share-creator";
                    shareCreator.Store();
                    shareReceiver = new User(context);
                    shareReceiver.Identifier = "share-receiver";
                    shareReceiver.Store();

                    context.StartImpersonation(shareCreator.Id);
                    sharedSeries = new Series(context);
                    sharedSeries.Identifier = "shared-series";
                    sharedSeries.Store();
                    sharedSeries.GrantPermissionsToUsers(new int[] {shareReceiver.Id});
                    unsharedSeries = new Series(context);
                    unsharedSeries.Domain = seriesShareDomain;
                    unsharedSeries.Identifier = "unshared-series";
                    unsharedSeries.Store();
                    context.EndImpersonation();

                    seriesShareRole = new Role(context);
                    seriesShareRole.Identifier = "series_all";
                    seriesShareRole.Store();
                    seriesShareRole.IncludePrivileges(Privilege.Get(EntityType.GetEntityType(typeof(Series))));
                    seriesShareRole.GrantToUsers(new int[]{shareCreator.Id}, seriesShareDomain.Id);

                    context.AccessLevel = EntityAccessLevel.Permission;

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
                    user31 = User.FromUsername(context, "global_service_admin");
                    role1 = Role.GetInstance(context);
                    role1.Load("cr_view+change");
                    role2 = Role.GetInstance(context);
                    role2.Load("cr_delete");
                    role2 = Role.GetInstance(context);
                    role2.Load("service_all");
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
                //Assert.IsTrue(Role.DoesUserHavePrivilege(context, user11.Id, domain1.Id, entityType, EntityOperationType.View);
                Assert.IsTrue(Role.DoesUserHavePrivilege(context, user12, domain1, "cr-v"));

                // Users "domain-1_cr_viewer+changer" and "domain-1_cr_viewer+changer+deleter" have privilege to change computing resources in domain-1
                Assert.IsTrue(Role.DoesUserHavePrivilege(context, user11, domain1, "cr-m"));
                Assert.IsTrue(Role.DoesUserHavePrivilege(context, user12, domain1, "cr-m"));

                // User "domain-1_cr_viewer+changer" DOES NOT have privilege to delete computing resources in domain-1
                Assert.IsFalse(Role.DoesUserHavePrivilege(context, user11, domain1, "cr-d"));

                // User "domain-1_cr_viewer+changer+deleter" DOES have privilege to delete computing resources in domain-1
                Assert.IsTrue(Role.DoesUserHavePrivilege(context, user12, domain1, "cr-d"));

                // Users "domain-1_cr_viewer+changer" and "domain-1_cr_viewer+changer+deleter" DO NOT have privilege to change or delete global computing resources
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

        [Test]
        public void TestLoadItemLists() {
            try {
                //EntityType crEntityType = EntityType.GetEntityType(typeof(ComputingResource));
                //EntityType psEntityType = EntityType.GetEntityType(typeof(PublishServer));

                Console.WriteLine("(1)");
                context.AccessLevel = EntityAccessLevel.Privilege;
                EntityList<ComputingResource> crs = new EntityList<ComputingResource>(context);
                crs.UserId = user11.Id;
                crs.Load();
                foreach (ComputingResource cr in crs) Console.WriteLine("CR {0}", cr.Identifier);

                Console.WriteLine("(2)");
                context.AccessLevel = EntityAccessLevel.Administrator;
                EntityList<ComputingResource> crs2 = new EntityList<ComputingResource>(context);
                crs2.Load();
                foreach (ComputingResource cr in crs2) Console.WriteLine("CR-2 {0}", cr.Identifier);
                context.AccessLevel = EntityAccessLevel.Permission;

                Console.WriteLine("(3)");
                EntityList<PublishServer> pss = new EntityList<PublishServer>(context);
                pss.Load();
                foreach (PublishServer ps in pss) Console.WriteLine("PS {0}", ps.Identifier);

                Console.WriteLine("(4)");
                context.AccessLevel = EntityAccessLevel.Administrator;
                EntityList<PublishServer> pss2 = new EntityList<PublishServer>(context);
                pss2.Load();
                foreach (PublishServer ps in pss2) Console.WriteLine("PS-2 {0}/{1}", ps.Identifier, ps.Name);
                context.AccessLevel = EntityAccessLevel.Permission;

            } catch (Exception e) {
                Console.WriteLine("{0} - {1}", e.Message, e.StackTrace);
                throw;
            }
        }

        [Test]
        public void TestLoadItems() {
            try {
                EntityType crEntityType = EntityType.GetEntityType(typeof(ComputingResource));
                EntityType psEntityType = EntityType.GetEntityType(typeof(PublishServer));

                //Console.WriteLine("CR EXISTS {0} {1} {2} {3}", crEntityType.DoesItemExist(context, 3), crEntityType.DoesItemExist(context, 1), crEntityType.DoesItemExist(context, "cr-3"), crEntityType.DoesItemExist(context, "cr-1"));
                //Console.WriteLine("PS EXISTS {0} {1} {2} {3}", psEntityType.DoesItemExist(context, 3), psEntityType.DoesItemExist(context, 1), psEntityType.DoesItemExist(context, "ps-3"), psEntityType.DoesItemExist(context, "ps-1"));

                //Console.WriteLine(crEntityType.GetQuery(context, null, 1, null, false, EntityAccessLevel.Permission));
                //Console.WriteLine(psEntityType.GetQuery(context, null, 1, null, false, EntityAccessLevel.Permission));

                ComputingResource cr1;

                context.AccessLevel = EntityAccessLevel.Privilege;

                Console.WriteLine("Privilege-based: user 'domain-1_cr_viewer+changer' (can view, cannot delete)");
                cr1 = new GenericComputingResource(context);
                cr1.UserId = user11.Id;
                cr1.Load("cr-1");
                Assert.IsTrue(cr1.CanView);
                Assert.IsTrue(cr1.CanChange);
                Assert.IsFalse(cr1.CanDelete);

                Console.WriteLine("Privilege-based: user 'domain-1_cr_viewer+changer' (can view, can delete)");
                cr1.UserId = user12.Id;
                cr1.Load("cr-1");
                Assert.IsTrue(cr1.CanView);
                Assert.IsTrue(cr1.CanChange);
                Assert.IsTrue(cr1.CanDelete);

            } catch (Exception e) {
                Console.WriteLine("{0} - {1}", e.Message, e.StackTrace);
                throw;
            }
        }

        [Test]
        public void SharingTest() {
            try {
                EntityList<Series> serieses;

                context.AccessLevel = EntityAccessLevel.Permission;
                serieses = new EntityList<Series>(context);
                serieses.UserId = shareCreator.Id;
                serieses.Load();
                Assert.IsTrue(serieses.Count == 2);
                serieses.UserId = shareReceiver.Id;
                serieses.Load();
                Assert.IsTrue(serieses.Count == 1);
                foreach (Series s in serieses) Assert.IsTrue(s.Identifier == sharedSeries.Identifier);

                context.AccessLevel = EntityAccessLevel.Privilege;
                serieses = new EntityList<Series>(context);
                serieses.UserId = shareCreator.Id;
                serieses.Load();
                Assert.IsTrue(serieses.Count == 2);
                serieses.UserId = shareReceiver.Id;
                serieses.Load();
                Assert.IsTrue(serieses.Count == 1);
                foreach (Series s in serieses) Assert.IsTrue(s.Identifier == sharedSeries.Identifier);

                Series series = Series.GetInstance(context);
                series.UserId = shareCreator.Id;
                series.Load("shared-series");
                Assert.IsTrue(series.AccessLevel == EntityAccessLevel.Permission);
                series.Load("unshared-series");
                Assert.IsTrue(series.AccessLevel == EntityAccessLevel.Privilege);
                series.Store();
                    
                series.UserId = shareReceiver.Id;
                series.Load("shared-series");
                Assert.IsTrue(series.AccessLevel == EntityAccessLevel.Permission);
                try {
                    series.Load("unshared-series");
                    Assert.IsTrue(false); // force failure (we should never arrive here)
                } catch (Exception e) {
                    Assert.IsTrue(e is EntityUnauthorizedException);
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
