using System;
using System.Linq;
using NUnit.Framework;
using System.Collections.Specialized;

namespace Terradue.Portal.Test {

    [TestFixture]
    public class EntityCollectionTest : BaseTest {

        [Test]
        public void LoadingTest() {
            context.AccessLevel = EntityAccessLevel.Administrator;
            context.Execute("DELETE FROM pubserver;");
            context.Execute("DELETE FROM usr WHERE username IN ('user1', 'user2');");

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
            pd1.ItemVisibility = EntityItemVisibility.OwnedOnly;
            pd1.Load();
            Assert.AreEqual(2, pd1.Count);
            context.EndImpersonation();

            context.StartImpersonation(user2.Id);
            EntityDictionary<PublishServer> pd2 = new EntityDictionary<PublishServer>(context);
            Assert.Throws<EntityNotFoundException>(delegate { 
                pd2.LoadFromSource(pd1, false);
            });

            pd2.LoadFromSource(pd1, true);
            Assert.AreEqual(2, pd2.Count);

            context.EndImpersonation();
        }


        [Test]
        public void AdminLoadingTest() {
            context.AccessLevel = EntityAccessLevel.Administrator;
            context.Execute("DELETE FROM series;");
            context.Execute("DELETE FROM usr WHERE username IN ('user1', 'user2');");

            User user1 = new User(context);
            user1.Username = "user1";
            user1.Store();

            context.StartImpersonation(user1.Id);
            Series series1 = new Series(context);
            series1.Identifier = "SERIES_PUBLIC";
            series1.Name = "Series public";
            series1.Store();
            series1.GrantPermissionsToAll();

            Series series2 = new Series(context);
            series2.Identifier = "SERIES_PRIVATE";
            series2.Name = "Series private";
            series2.Store();

            context.EndImpersonation();

            EntityDictionary<Series> sd1 = new EntityDictionary<Series>(context);
            sd1.AccessLevel = EntityAccessLevel.Privilege;
            sd1.ItemVisibility = EntityItemVisibility.All;
            sd1.Load();
            Assert.AreEqual(1, sd1.Count);

            EntityDictionary<Series> sd2 = new EntityDictionary<Series>(context);
            sd2.AccessLevel = EntityAccessLevel.Administrator;
            sd2.ItemVisibility = EntityItemVisibility.All;
            sd2.Load();
            Assert.AreEqual(2, sd2.Count);
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
            p1.FileRootDir = "/dir";
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
            Assert.AreEqual(2, pd1.TotalResults);
            Assert.AreEqual(2, pd1.Count);
            Assert.IsTrue(pd1.Contains(p1.Id) && pd1.Contains(p2.Id));

            EntityDictionary<PublishServer> pd2 = new EntityDictionary<PublishServer>(context);
            pd2.SetFilter("Port", "123");
            pd2.Load();
            Assert.AreEqual(1, pd2.TotalResults);
            Assert.AreEqual(1, pd2.Count);
            Assert.IsTrue(pd2.Contains(p2.Id));

            EntityDictionary<PublishServer> pd3 = new EntityDictionary<PublishServer>(context);
            pd3.SetFilter("Port", "[234");
            pd3.Load();
            Assert.AreEqual(2, pd3.TotalResults);
            Assert.AreEqual(2, pd3.Count);
            Assert.IsTrue(pd3.Contains(p3a.Id) && pd3.Contains(p3b.Id));

            EntityDictionary<PublishServer> pd4 = new EntityDictionary<PublishServer>(context);
            pd4.SetFilter("Port", "]100,300[");
            pd4.Load();
            Assert.AreEqual(2, pd4.TotalResults);
            Assert.AreEqual(2, pd4.Count);
            Assert.IsTrue(pd4.Contains(p2.Id) && pd4.Contains(p3a.Id));

            EntityDictionary<PublishServer> pd5 = new EntityDictionary<PublishServer>(context);
            pd5.SetFilter("Name", "pf3*");
            pd5.Load();
            Assert.AreEqual(2, pd5.TotalResults);
            Assert.AreEqual(2, pd5.Count);
            Assert.IsTrue(pd5.Contains(p3a.Id) && pd3.Contains(p3b.Id));

            context.ConsoleDebug = true;

            EntityDictionary<PublishServer> pd6 = new EntityDictionary<PublishServer>(context);
            pd6.SetFilter("FileRootDir", SpecialSearchValue.Null);
            pd6.Load();
            Assert.AreEqual(3, pd6.TotalResults);
            Assert.AreEqual(3, pd6.Count);
            Assert.IsTrue(pd6.Contains(p2.Id) && pd6.Contains(p3a.Id) && pd3.Contains(p3b.Id));

            EntityDictionary<PublishServer> pd7 = new EntityDictionary<PublishServer>(context);
            pd7.SetFilter("FileRootDir", SpecialSearchValue.NotNull);
            pd7.Load();
            Assert.AreEqual(1, pd7.TotalResults);
            Assert.AreEqual(1, pd7.Count);
            Assert.IsTrue(pd7.Contains(p1.Id));
        }

        [Test]
        public void KeywordFilterTest() {
            context.AccessLevel = EntityAccessLevel.Administrator;
            context.Execute("DELETE FROM pubserver;");

            PublishServer p1 = new PublishServer(context);
            p1.Name = "pf1 abc def ghi";
            p1.Protocol = "ftp";
            p1.Hostname = "test.org";
            p1.Store();
            PublishServer p2 = new PublishServer(context);
            p2.Name = "pf2";
            p2.Protocol = "ftp";
            p2.Hostname = "def.org";
            p2.Store();
            PublishServer p3 = new PublishServer(context);
            p3.Name = "pf3";
            p3.Protocol = "ftp";
            p3.Hostname = "test.org";
            p3.Path = "/ghi/jkl";
            p3.Store();

            EntityDictionary<PublishServer> pd;

            pd = new EntityDictionary<PublishServer>(context);
            pd.SearchKeyword = "abc";
            pd.Load();
            Assert.AreEqual(1, pd.TotalResults);
            Assert.AreEqual(1, pd.Count);
            Assert.IsTrue(pd.Contains(p1.Id));

            pd = new EntityDictionary<PublishServer>(context);
            pd.SearchKeyword = "def";
            pd.Load();
            Assert.AreEqual(2, pd.TotalResults);
            Assert.AreEqual(2, pd.Count);
            Assert.IsTrue(pd.Contains(p1.Id) && pd.Contains(p2.Id));

            pd = new EntityDictionary<PublishServer>(context);
            pd.SearchKeyword = "ghi";
            pd.Load();
            Assert.AreEqual(2, pd.TotalResults);
            Assert.AreEqual(2, pd.Count);
            Assert.IsTrue(pd.Contains(p1.Id) && pd.Contains(p3.Id));

            pd = new EntityDictionary<PublishServer>(context);
            pd.SearchKeyword = "de";
            pd.FindWholeWords = true;
            pd.Load();
            Assert.AreEqual(0, pd.TotalResults);
            Assert.AreEqual(0, pd.Count);

            pd = new EntityDictionary<PublishServer>(context);
            pd.SearchKeyword = "de";
            pd.FindWholeWords = false;
            pd.Load();
            Assert.AreEqual(2, pd.TotalResults);
            Assert.AreEqual(2, pd.Count);
            Assert.IsTrue(pd.Contains(p1.Id) && pd.Contains(p2.Id));

            pd = new EntityDictionary<PublishServer>(context);
            pd.SearchKeyword = "*";
            pd.FindWholeWords = false;
            context.ConsoleDebug = true;
            pd.Load();
            Console.WriteLine("TotalResults={0}, Count={1}", pd.TotalResults, pd.Count);
            Assert.AreEqual(3, pd.TotalResults);
            Assert.AreEqual(3, pd.Count);
            Assert.IsTrue(pd.Contains(p1.Id) && pd.Contains(p2.Id) && pd.Contains(p3.Id));

            EntityList<WpsProcessOffering> el = new EntityList<WpsProcessOffering>(context);
            el.SearchKeyword = "bla";
            context.ConsoleDebug = true;
            el.Load();


        }

        [Test]
        public void PagingTest() {
            context.AccessLevel = EntityAccessLevel.Administrator;
            context.Execute("DELETE FROM series;");

            for (int i = 1; i <= 50; i++) {
                Series series = new Series(context);
                series.Identifier = String.Format("SERIES{0}", i);
                series.Name = String.Format("Series {0}", i);
                series.Store();
            }

            EntityDictionary<Series> sd1 = new EntityDictionary<Series>(context);
            sd1.Load();
            Assert.AreEqual(50, sd1.TotalResults);
            Assert.AreEqual(50, sd1.Count);

            EntityDictionary<Series> sd2 = new EntityDictionary<Series>(context);
            sd2.Page = 1;
            sd2.Load();
            Assert.AreEqual(50, sd2.TotalResults);
            Assert.AreEqual(20, sd2.Count);
            Series s2 = sd2.FirstOrDefault((s => true));
            Assert.AreEqual("SERIES1", s2.Identifier);

            EntityDictionary<Series> sd3 = new EntityDictionary<Series>(context);
            sd3.Page = 2;
            sd3.Load();
            Assert.AreEqual(50, sd3.TotalResults);
            Assert.AreEqual(20, sd3.Count);
            Series s3 = sd3.FirstOrDefault((s => true));
            Assert.AreEqual("SERIES21", s3.Identifier);

            EntityDictionary<Series> sd4 = new EntityDictionary<Series>(context);
            sd4.StartIndex = 13;
            sd4.ItemsPerPage = 17;
            sd4.Load();
            Assert.AreEqual(50, sd4.TotalResults);
            Assert.AreEqual(17, sd4.Count);
            Series s4 = sd4.FirstOrDefault((s => true));
            Assert.AreEqual("SERIES13", s4.Identifier);
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
            Assert.AreEqual(4, pd1.Count);
            expectedIds = new int[] { p1.Id, p2.Id, p3.Id, p4.Id };
            index = 0;
            foreach (PublishServer ps in pd1) Assert.AreEqual(expectedIds[index++], ps.Id);

            EntityDictionary<PublishServer> pd2 = new EntityDictionary<PublishServer>(context);
            pd2.AddSort("Name", SortDirection.Descending);
            pd2.Load();
            Assert.AreEqual(4, pd2.Count);
            expectedIds = new int[] { p4.Id, p3.Id, p2.Id, p1.Id };
            index = 0;
            foreach (PublishServer ps in pd2) Assert.AreEqual(expectedIds[index++], ps.Id);

            EntityDictionary<PublishServer> pd3 = new EntityDictionary<PublishServer>(context);
            pd3.AddSort("Port", SortDirection.Descending);
            pd3.Load();
            Assert.AreEqual(4, pd3.Count);
            expectedIds = new int[] { p4.Id, p3.Id, p2.Id, p1.Id };
            index = 0;
            foreach (PublishServer ps in pd3) Assert.AreEqual(expectedIds[index++], ps.Id);

            EntityDictionary<PublishServer> pd4 = new EntityDictionary<PublishServer>(context);
            pd4.AddSort("Protocol", SortDirection.Ascending);
            pd4.AddSort("Name", SortDirection.Descending);
            pd4.Load();
            Assert.AreEqual(4, pd4.Count);
            expectedIds = new int[] { p2.Id, p1.Id, p4.Id, p3.Id };
            index = 0;
            foreach (PublishServer ps in pd4) Assert.AreEqual(expectedIds[index++], ps.Id);

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
            Assert.AreEqual(2, pd1.Count);
            Assert.AreEqual(2, pd1.TotalResults); // filtered

            EntityDictionary<PublishServer> pd2 = new EntityDictionary<PublishServer>(context);
            pd2.ItemsPerPage = 3;
            pd2.Load();
            Assert.AreEqual(3, pd2.Count);
            Assert.AreEqual(4, pd2.TotalResults); // all
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
            ppub.GrantPermissionsToAll();

            PublishServer ppubresg = new PublishServer(context);
            ppubresg.Name = "public and assigned to group";
            ppubresg.Protocol = "ftp";
            ppubresg.Hostname = "test.org";
            ppubresg.Store();
            ppubresg.GrantPermissionsToAll();
            ppubresg.GrantPermissionsToGroups(new Group[] { group });

            PublishServer ppubresg2 = new PublishServer(context);
            ppubresg2.Name = "public and assigned to other group";
            ppubresg2.Protocol = "ftp";
            ppubresg2.Hostname = "test.org";
            ppubresg2.Store();
            ppubresg2.GrantPermissionsToAll();
            ppubresg2.GrantPermissionsToGroups(new Group[] { group2 });

            PublishServer ppubresu = new PublishServer(context);
            ppubresu.Name = "public and assigned to user";
            ppubresu.Protocol = "ftp";
            ppubresu.Hostname = "test.org";
            ppubresu.Store();
            ppubresu.GrantPermissionsToAll();
            ppubresu.GrantPermissionsToUsers(new User[] { user });

            PublishServer ppubresu2 = new PublishServer(context);
            ppubresu2.Name = "public and assigned to other user";
            ppubresu2.Protocol = "ftp";
            ppubresu2.Hostname = "test.org";
            ppubresu2.Store();
            ppubresu2.GrantPermissionsToAll();
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
            powna.Name = "owned by user (shared with all)";
            powna.Protocol = "ftp";
            powna.Hostname = "test.org";
            powna.Store();
            powna.GrantPermissionsToAll();

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
            pownu.GrantPermissionsToUsers(new User[] { user2 });

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
            EntityDictionary<PublishServer> pd = new EntityDictionary<PublishServer>(context);
            pd.ItemVisibility = EntityItemVisibility.All;
            pd.Load();
            Assert.AreEqual(11, pd.Count);
            Assert.IsTrue(pd.Contains(ppub.Id));
            Assert.AreEqual(EntityItemVisibility.Public, pd[ppub.Id].Visibility);
            Assert.IsTrue(pd.Contains(ppubresg.Id));
            Assert.AreEqual(EntityItemVisibility.Public, pd[ppubresg.Id].Visibility);
            Assert.IsTrue(pd.Contains(ppubresg2.Id));
            Assert.AreEqual(EntityItemVisibility.Public, pd[ppubresg2.Id].Visibility);
            Assert.IsTrue(pd.Contains(ppubresu.Id));
            Assert.AreEqual(EntityItemVisibility.Public, pd[ppubresu.Id].Visibility);
            Assert.IsTrue(pd.Contains(ppubresu2.Id));
            Assert.AreEqual(EntityItemVisibility.Public, pd[ppubresu2.Id].Visibility);
            Assert.IsTrue(pd.Contains(presg.Id));
            Assert.AreEqual(EntityItemVisibility.Restricted, pd[presg.Id].Visibility);
            Assert.IsTrue(pd.Contains(presu.Id));
            Assert.AreEqual(EntityItemVisibility.Private, pd[presu.Id].Visibility);
            Assert.IsTrue(pd.Contains(powna.Id));
            Assert.AreEqual(EntityItemVisibility.Public, pd[powna.Id].Visibility);
            Assert.IsTrue(pd.Contains(powng.Id));
            Assert.AreEqual(EntityItemVisibility.Restricted, pd[powng.Id].Visibility);
            Assert.IsTrue(pd.Contains(pownu.Id));
            Assert.AreEqual(EntityItemVisibility.Restricted, pd[pownu.Id].Visibility);
            Assert.IsTrue(pd.Contains(pown.Id));
            Assert.AreEqual(EntityItemVisibility.Private, pd[pown.Id].Visibility);

            pd.Clear();
            pd.ItemVisibility = EntityItemVisibility.Public;
            pd.Load();
            Assert.AreEqual(6, pd.Count);
            Assert.IsTrue(pd.Contains(ppub.Id));
            Assert.IsTrue(pd.Contains(ppubresg.Id));
            Assert.IsTrue(pd.Contains(ppubresg2.Id));
            Assert.IsTrue(pd.Contains(ppubresu.Id));
            Assert.IsTrue(pd.Contains(ppubresu2.Id));
            Assert.IsTrue(pd.Contains(powna.Id));

            pd.Clear();
            pd.ItemVisibility = EntityItemVisibility.Restricted;
            pd.Load();
            Assert.AreEqual(3, pd.Count);
            Assert.IsTrue(pd.Contains(presg.Id));
            Assert.IsTrue(pd.Contains(powng.Id));
            Assert.IsTrue(pd.Contains(pownu.Id));

            pd.Clear();
            pd.ItemVisibility = EntityItemVisibility.Private;
            pd.Load();
            Assert.AreEqual(2, pd.Count);
            Assert.IsTrue(pd.Contains(presu.Id));
            Assert.IsTrue(pd.Contains(pown.Id));

            pd.Clear();
            pd.ItemVisibility = EntityItemVisibility.Public | EntityItemVisibility.Restricted;
            pd.Load();
            Assert.AreEqual(9, pd.Count);
            Assert.IsTrue(pd.Contains(ppub.Id));
            Assert.IsTrue(pd.Contains(ppubresg.Id));
            Assert.IsTrue(pd.Contains(ppubresg2.Id));
            Assert.IsTrue(pd.Contains(ppubresu.Id));
            Assert.IsTrue(pd.Contains(ppubresu2.Id));
            Assert.IsTrue(pd.Contains(presg.Id));
            Assert.IsTrue(pd.Contains(powna.Id));
            Assert.IsTrue(pd.Contains(powng.Id));
            Assert.IsTrue(pd.Contains(pownu.Id));

            pd.Clear();
            pd.ItemVisibility = EntityItemVisibility.Public | EntityItemVisibility.Private;
            pd.Load();
            Assert.AreEqual(8, pd.Count);
            Assert.IsTrue(pd.Contains(ppub.Id));
            Assert.IsTrue(pd.Contains(ppubresg.Id));
            Assert.IsTrue(pd.Contains(ppubresg2.Id));
            Assert.IsTrue(pd.Contains(ppubresu.Id));
            Assert.IsTrue(pd.Contains(ppubresu2.Id));
            Assert.IsTrue(pd.Contains(presu.Id));
            Assert.IsTrue(pd.Contains(powna.Id));
            Assert.IsTrue(pd.Contains(pown.Id));

            pd.Clear();
            pd.ItemVisibility = EntityItemVisibility.All | EntityItemVisibility.OwnedOnly;
            pd.Load();
            Assert.AreEqual(4, pd.Count);
            Assert.IsTrue(pd.Contains(powna.Id));
            Assert.IsTrue(pd.Contains(powng.Id));
            Assert.IsTrue(pd.Contains(pownu.Id));
            Assert.IsTrue(pd.Contains(pown.Id));

            pd.Clear();
            pd.ItemVisibility = EntityItemVisibility.Public | EntityItemVisibility.OwnedOnly;
            pd.Load();
            Assert.AreEqual(1, pd.Count);
            Assert.IsTrue(pd.Contains(powna.Id));

            pd.Clear();
            pd.ItemVisibility = EntityItemVisibility.Restricted | EntityItemVisibility.OwnedOnly;
            pd.Load();
            Assert.AreEqual(2, pd.Count);
            Assert.IsTrue(pd.Contains(powng.Id));
            Assert.IsTrue(pd.Contains(pownu.Id));

            pd.Clear();
            pd.ItemVisibility = EntityItemVisibility.Private | EntityItemVisibility.OwnedOnly;
            pd.Load();
            Assert.AreEqual(1, pd.Count);
            Assert.IsTrue(pd.Contains(pown.Id));

            context.EndImpersonation();
        }

        [Test]
        public void GetIds() {
            context.AccessLevel = EntityAccessLevel.Administrator;
            context.Execute("DELETE FROM series;");

            Series s1 = new Series(context);
            s1.Identifier = "SERIES1";
            s1.Name = "Test series 1";
            s1.Store();

            Series s2 = new Series(context);
            s2.Identifier = "SERIES2";
            s2.Name = "Test series 2";
            s2.Store();

            Series s3 = new Series(context);
            s3.Identifier = "SERIES3";
            s3.Name = "Test series 3";
            s3.Store();

            EntityType seriesType = EntityType.GetEntityType(typeof(Series));

            int[] result1 = seriesType.GetIds(context, new string[] { "SERIES1", "SERIES2", "SERIES3" }, true, false);
            Assert.AreEqual(3, result1.Length);
            Assert.AreEqual(s1.Id, result1[0]);
            Assert.AreEqual(s2.Id, result1[1]);
            Assert.AreEqual(s3.Id, result1[2]);

            int[] result2 = seriesType.GetIds(context, new string[] { "Test series 1", "SERIES2" }, true, false);
            Assert.AreEqual(1, result2.Length);
            Assert.AreEqual(s2.Id, result2[0]);

            int[] result3 = seriesType.GetIds(context, new string[] { "Test series 1", "SERIES2" }, false, true);
            Assert.AreEqual(1, result3.Length);
            Assert.AreEqual(s1.Id, result3[0]);

            int[] result4 = seriesType.GetIds(context, new string[] { "Test series 1", "SERIES2" }, true, true);
            Assert.AreEqual(2, result4.Length);
            Assert.AreEqual(s1.Id, result4[0]);
            Assert.AreEqual(s2.Id, result4[1]);

            NameValueCollection nv4 = new NameValueCollection();
            nv4.Add("series", "Test series 1");
            nv4.Add("series", "SERIES2");
            nv4.Add("other", "SERIES3");
            string sresult4 = seriesType.GetIdFilterString(context, nv4, "series", true, true);
            Assert.IsTrue(sresult4 == String.Format("{0},{1}", s1.Id, s2.Id));

        }

    }

}

