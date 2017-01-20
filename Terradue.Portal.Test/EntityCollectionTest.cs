using System;
using System.Linq;
using NUnit.Framework;
using System.Collections.Generic;

namespace Terradue.Portal.Test {

    [TestFixture]
    public class EntityCollectionTest : BaseTest {

        [Test]
        public void LoadingTest() {
            context.AccessLevel = EntityAccessLevel.Administrator;
            context.Execute("DELETE FROM pubserver;");

            User user1 = new User(context);
            user1.Username = "user1";
            user1.Store();

            User user2 = new User(context);
            user2.Username = "user2";
            user2.Store();

            context.StartImpersonation(user1.Id);
            PublishServer p1 = new PublishServer(context);
            p1.Name = "p1";
            p1.Protocol = "ftp";
            p1.Hostname = "test.org";
            p1.Store();
            PublishServer p2 = new PublishServer(context);
            p2.Name = "p2";
            p2.Protocol = "ftp";
            p2.Hostname = "test.org";
            p2.Store();
            context.EndImpersonation();

            context.StartImpersonation(user2.Id);
            PublishServer p3 = new PublishServer(context);
            p3.Name = "p3";
            p3.Protocol = "ftp";
            p3.Hostname = "test.org";
            p3.Store();
            context.EndImpersonation();

            context.StartImpersonation(user1.Id);
            EntityDictionary<PublishServer> pd1 = new EntityDictionary<PublishServer>(context);
            pd1.ItemVisibility = ItemVisibilityMode.PrivateOnly;
            pd1.Load();
            Assert.IsTrue(pd1.Count == 2);
            context.EndImpersonation();

            context.StartImpersonation(user2.Id);
            EntityDictionary<PublishServer> pd2 = new EntityDictionary<PublishServer>(context);
            Assert.Throws<EntityNotFoundException>(delegate { 
                pd2.LoadFromSource(pd1, false);
            });

            pd2.LoadFromSource(pd1, true);
            Assert.IsTrue(pd2.Count == 2);

            context.EndImpersonation();
        }


        [Test]
        public void FilterTest() {
            context.AccessLevel = EntityAccessLevel.Administrator;
            context.Execute("DELETE FROM pubserver;");

            PublishServer p1 = new PublishServer(context);
            p1.Name = "pf1";
            p1.Protocol = "ftp";
            p1.Hostname = "test.org";
            p1.Port = 23;
            p1.Store();
            PublishServer p2 = new PublishServer(context);
            p2.Name = "pf2";
            p2.Protocol = "ftp";
            p2.Hostname = "anothertest.org";
            p2.Port = 123;
            p2.Store();
            PublishServer p3a = new PublishServer(context);
            p3a.Name = "pf3a";
            p3a.Protocol = "ftp";
            p3a.Hostname = "experiment.org";
            p3a.Port = 234;
            p3a.Store();
            PublishServer p3b = new PublishServer(context);
            p3b.Name = "pf3b";
            p3b.Protocol = "ftp";
            p3b.Hostname = "try.org";
            p3b.Port = 345;
            p3b.Store();

            EntityDictionary<PublishServer> pd1 = new EntityDictionary<PublishServer>(context);
            pd1.SetFilter("Hostname", "*test*.org");
            pd1.Load();
            Assert.IsTrue(pd1.Count == 2);
            Assert.IsTrue(pd1.Contains(p1.Id) && pd1.Contains(p2.Id));

            EntityDictionary<PublishServer> pd2 = new EntityDictionary<PublishServer>(context);
            pd2.SetFilter("Port", "123");
            pd2.Load();
            Assert.IsTrue(pd2.Count == 1);
            Assert.IsTrue(pd2.Contains(p2.Id));

            EntityDictionary<PublishServer> pd3 = new EntityDictionary<PublishServer>(context);
            pd3.SetFilter("Port", "[234");
            pd3.Load();
            Assert.IsTrue(pd3.Count == 2);
            Assert.IsTrue(pd3.Contains(p3a.Id) && pd3.Contains(p3b.Id));

            EntityDictionary<PublishServer> pd4 = new EntityDictionary<PublishServer>(context);
            pd4.SetFilter("Port", "]100,300[");
            pd4.Load();
            Assert.IsTrue(pd4.Count == 2);
            Assert.IsTrue(pd4.Contains(p2.Id) && pd4.Contains(p3a.Id));

            EntityDictionary<PublishServer> pd5 = new EntityDictionary<PublishServer>(context);
            pd5.SetFilter("Name", "pf3*");
            pd5.Load();
            Assert.IsTrue(pd5.Count == 2);
            Assert.IsTrue(pd5.Contains(p3a.Id) && pd3.Contains(p3b.Id));
        }

        [Test]
        public void PagingTest() {
            context.AccessLevel = EntityAccessLevel.Administrator;

            for (int i = 1; i <= 50; i++) {
                Series series = new Series(context);
                series.Identifier = String.Format("SERIES{0}", i);
                series.Name = String.Format("Series {0}", i);
                series.Store();
            }

            EntityDictionary<Series> sd1 = new EntityDictionary<Series>(context);
            sd1.Load();
            Assert.IsTrue(sd1.Count == 50);

            EntityDictionary<Series> sd2 = new EntityDictionary<Series>(context);
            sd2.Page = 1;
            sd2.Load();
            Assert.IsTrue(sd2.Count == 20);
            Series s2 = sd2.FirstOrDefault((s => true));
            Assert.IsTrue(s2.Identifier == "SERIES1");

            EntityDictionary<Series> sd3 = new EntityDictionary<Series>(context);
            sd3.Page = 2;
            sd3.Load();
            Assert.IsTrue(sd3.Count == 20);
            Series s3 = sd3.FirstOrDefault((s => true));
            Assert.IsTrue(s3.Identifier == "SERIES21");

            EntityDictionary<Series> sd4 = new EntityDictionary<Series>(context);
            sd4.StartIndex = 13;
            sd4.ItemsPerPage = 17;
            sd4.Load();
            Assert.IsTrue(sd4.Count == 17);
            Series s4 = sd4.FirstOrDefault((s => true));
            Assert.IsTrue(s4.Identifier == "SERIES14");
        }

        [Test]
        public void SortTest() {
            context.AccessLevel = EntityAccessLevel.Administrator;
            context.Execute("DELETE FROM pubserver;");

            PublishServer p1 = new PublishServer(context);
            p1.Name = "pf1";
            p1.Protocol = "ftp";
            p1.Hostname = "test.org";
            p1.Port = 23;
            p1.Store();
            PublishServer p2 = new PublishServer(context);
            p2.Name = "pf2";
            p2.Protocol = "ftp";
            p2.Hostname = "anothertest.org";
            p2.Port = 123;
            p2.Store();
            PublishServer p3 = new PublishServer(context);
            p3.Name = "pf3";
            p3.Protocol = "sftp";
            p3.Hostname = "experiment.org";
            p3.Port = 234;
            p3.Store();
            PublishServer p4 = new PublishServer(context);
            p4.Name = "pf4";
            p4.Protocol = "sftp";
            p4.Hostname = "try.org";
            p4.Port = 345;
            p4.Store();

            int index;
            int[] expectedIds;

            EntityDictionary<PublishServer> pd1 = new EntityDictionary<PublishServer>(context);
            pd1.AddSort("Name");
            pd1.Load();
            Assert.IsTrue(pd1.Count == 4);
            expectedIds = new int[] { p1.Id, p2.Id, p3.Id, p4.Id };
            index = 0;
            foreach (PublishServer ps in pd1) Assert.IsTrue(ps.Id == expectedIds[index++]);

            EntityDictionary<PublishServer> pd2 = new EntityDictionary<PublishServer>(context);
            pd2.AddSort("Name", SortDirection.Descending);
            pd2.Load();
            Assert.IsTrue(pd2.Count == 4);
            expectedIds = new int[] { p4.Id, p3.Id, p2.Id, p1.Id };
            index = 0;
            foreach (PublishServer ps in pd2) Assert.IsTrue(ps.Id == expectedIds[index++]);

            EntityDictionary<PublishServer> pd3 = new EntityDictionary<PublishServer>(context);
            pd3.AddSort("Port", SortDirection.Descending);
            pd3.Load();
            Assert.IsTrue(pd3.Count == 4);
            expectedIds = new int[] { p4.Id, p3.Id, p2.Id, p1.Id };
            index = 0;
            foreach (PublishServer ps in pd3) Assert.IsTrue(ps.Id == expectedIds[index++]);

            EntityDictionary<PublishServer> pd4 = new EntityDictionary<PublishServer>(context);
            pd4.AddSort("Protocol", SortDirection.Ascending);
            pd4.AddSort("Name", SortDirection.Descending);
            pd4.Load();
            Assert.IsTrue(pd4.Count == 4);
            expectedIds = new int[] { p2.Id, p1.Id, p4.Id, p3.Id };
            index = 0;
            foreach (PublishServer ps in pd4) Assert.IsTrue(ps.Id == expectedIds[index++]);

        }

        [Test]
        public void TotalResultsTest() {
            context.AccessLevel = EntityAccessLevel.Administrator;
            context.Execute("DELETE FROM pubserver;");

            PublishServer p1 = new PublishServer(context);
            p1.Name = "pf1";
            p1.Protocol = "ftp";
            p1.Hostname = "test.org";
            p1.Port = 23;
            p1.Store();
            PublishServer p2 = new PublishServer(context);
            p2.Name = "pf2";
            p2.Protocol = "ftp";
            p2.Hostname = "anothertest.org";
            p2.Port = 123;
            p2.Store();
            PublishServer p3 = new PublishServer(context);
            p3.Name = "pf3";
            p3.Protocol = "sftp";
            p3.Hostname = "experiment.org";
            p3.Port = 234;
            p3.Store();
            PublishServer p4 = new PublishServer(context);
            p4.Name = "pf4";
            p4.Protocol = "sftp";
            p4.Hostname = "try.org";
            p4.Port = 345;
            p4.Store();

            EntityDictionary<PublishServer> pd1 = new EntityDictionary<PublishServer>(context);
            pd1.SetFilter("Protocol", "sftp");
            pd1.Load();
            Assert.IsTrue(pd1.Count == 2);
            Assert.IsTrue(pd1.TotalResults == 2); // filtered

            EntityDictionary<PublishServer> pd2 = new EntityDictionary<PublishServer>(context);
            pd2.ItemsPerPage = 3;
            pd2.Load();
            Assert.IsTrue(pd2.Count == 3);
            Assert.IsTrue(pd2.TotalResults == 4); // all
        }

        [Test]
        public void VisibilityTest() {
            context.AccessLevel = EntityAccessLevel.Administrator;
            context.Execute("DELETE FROM pubserver;");

            User user = new User(context);
            user.Username = "userVis";
            user.Store();

            User user2 = new User(context);
            user2.Username = "userVis2";
            user2.Store();

            Group group = new Group(context);
            group.Name = "groupVis";
            group.Identifier = "groupVis";
            group.Store();

            Group group2 = new Group(context);
            group2.Name = "groupVis2";
            group2.Identifier = "groupVis2";
            group2.Store();

            group.AssignUser(user);

            PublishServer ppub = new PublishServer(context);
            ppub.Name = "public";
            ppub.Protocol = "ftp";
            ppub.Hostname = "test.org";
            ppub.Store();
            ppub.GrantGlobalPermissions();

            PublishServer ppubresg = new PublishServer(context);
            ppubresg.Name = "public and assigned to group";
            ppubresg.Protocol = "ftp";
            ppubresg.Hostname = "test.org";
            ppubresg.Store();
            ppubresg.GrantGlobalPermissions();
            ppubresg.GrantPermissionsToGroups(new Group[] { group });

            PublishServer ppubresg2 = new PublishServer(context);
            ppubresg2.Name = "public and assigned to other group";
            ppubresg2.Protocol = "ftp";
            ppubresg2.Hostname = "test.org";
            ppubresg2.Store();
            ppubresg2.GrantGlobalPermissions();
            ppubresg2.GrantPermissionsToGroups(new Group[] { group2 });

            PublishServer ppubresu = new PublishServer(context);
            ppubresu.Name = "public and assigned to user";
            ppubresu.Protocol = "ftp";
            ppubresu.Hostname = "test.org";
            ppubresu.Store();
            ppubresu.GrantGlobalPermissions();
            ppubresu.GrantPermissionsToUsers(new User[] { user });

            PublishServer ppubresu2 = new PublishServer(context);
            ppubresu2.Name = "public and assigned to other user";
            ppubresu2.Protocol = "ftp";
            ppubresu2.Hostname = "test.org";
            ppubresu2.Store();
            ppubresu2.GrantGlobalPermissions();
            ppubresu2.GrantPermissionsToUsers(new User[] { user2 });

            PublishServer presg = new PublishServer(context);
            presg.Name = "restricted to group";
            presg.Protocol = "ftp";
            presg.Hostname = "test.org";
            presg.Store();
            presg.GrantPermissionsToGroups(new Group[] { group });

            PublishServer presu = new PublishServer(context);
            presu.Name = "restricted to user";
            presu.Protocol = "ftp";
            presu.Hostname = "test.org";
            presu.Store();
            presu.GrantPermissionsToUsers(new User[] { user });

            PublishServer powna = new PublishServer(context);
            powna.OwnerId = user.Id;
            powna.Name = "owned by user (shared with group)";
            powna.Protocol = "ftp";
            powna.Hostname = "test.org";
            powna.Store();
            powna.GrantGlobalPermissions();

            PublishServer powng = new PublishServer(context);
            powng.OwnerId = user.Id;
            powng.Name = "owned by user (shared with group)";
            powng.Protocol = "ftp";
            powng.Hostname = "test.org";
            powng.Store();
            powng.GrantPermissionsToGroups(new Group[] { group });

            PublishServer pownu = new PublishServer(context);
            pownu.OwnerId = user.Id;
            pownu.Name = "owned by user (shared with other user)";
            pownu.Protocol = "ftp";
            pownu.Hostname = "test.org";
            pownu.Store();
            pownu.GrantPermissionsToUsers(new int[] { context.UserId });

            PublishServer pown = new PublishServer(context);
            pown.OwnerId = user.Id;
            pown.Name = "owned by user (exclusive)";
            pown.Protocol = "ftp";
            pown.Hostname = "test.org";
            pown.Store();

            PublishServer pn = new PublishServer(context);
            pn.Name = "not accessible";
            pn.Protocol = "ftp";
            pn.Hostname = "test.org";
            pn.Store();

            context.StartImpersonation(user.Id);
            context.AccessLevel = EntityAccessLevel.Privilege;
            context.ConsoleDebug = true;
            EntityDictionary<PublishServer> pd1 = new EntityDictionary<PublishServer>(context);
            pd1.ItemVisibility = ItemVisibilityMode.All;
            pd1.Load();
            foreach (PublishServer p in pd1) Console.WriteLine("* PD1: \"{0}\"", p.Name);
            Assert.IsTrue(pd1.Count == 11);
            Assert.IsTrue(pd1.Contains(ppub.Id));
            Assert.IsTrue(pd1.Contains(ppubresg.Id));
            Assert.IsTrue(pd1.Contains(ppubresg2.Id));
            Assert.IsTrue(pd1.Contains(ppubresu.Id));
            Assert.IsTrue(pd1.Contains(ppubresu2.Id));
            Assert.IsTrue(pd1.Contains(presg.Id));
            Assert.IsTrue(pd1.Contains(presu.Id));
            Assert.IsTrue(pd1.Contains(powna.Id));
            Assert.IsTrue(pd1.Contains(powng.Id));
            Assert.IsTrue(pd1.Contains(pownu.Id));
            Assert.IsTrue(pd1.Contains(pown.Id));

            EntityDictionary<PublishServer> pd2 = new EntityDictionary<PublishServer>(context);
            pd2.ItemVisibility = ItemVisibilityMode.Public;
            pd2.Load();
            foreach (PublishServer p in pd2) Console.WriteLine("* PD2: \"{0}\"", p.Name);
            Assert.IsTrue(pd2.Count == 6);
            Assert.IsTrue(pd2.Contains(ppub.Id));
            Assert.IsTrue(pd2.Contains(ppubresg.Id));
            Assert.IsTrue(pd2.Contains(ppubresg2.Id));
            Assert.IsTrue(pd2.Contains(ppubresu.Id));
            Assert.IsTrue(pd2.Contains(ppubresu2.Id));
            Assert.IsTrue(pd2.Contains(powna.Id));

            EntityDictionary<PublishServer> pd3 = new EntityDictionary<PublishServer>(context);
            pd3.ItemVisibility = ItemVisibilityMode.Restricted;
            pd3.Load();
            foreach (PublishServer p in pd3) Console.WriteLine("* PD3: \"{0}\"", p.Name);
            Assert.IsTrue(pd3.Count == 8);
            Assert.IsTrue(pd3.Contains(ppubresg.Id));
            Assert.IsTrue(pd3.Contains(ppubresu.Id));
            Assert.IsTrue(pd3.Contains(presg.Id));
            Assert.IsTrue(pd3.Contains(presu.Id));
            Assert.IsTrue(pd3.Contains(powna.Id));
            Assert.IsTrue(pd3.Contains(powng.Id));
            Assert.IsTrue(pd3.Contains(pownu.Id));

            context.EndImpersonation();
        }

    }

}

