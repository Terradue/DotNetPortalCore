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
            pd1.OwnedItemsOnly = true;
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
            Assert.IsTrue(pd2.TotalResults == 4);
        }

    }

}

