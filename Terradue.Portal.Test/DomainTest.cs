using System;
using NUnit.Framework;
using Terradue.Portal;

namespace Terradue.Portal.Test {

    [TestFixture]
    public class DomainTest : BaseTest {

        [Test]
        public void DomainCreationTest(){
            
            Domain domainpub = new Domain(context);
            domainpub.Identifier = "domainPublic";
            domainpub.Kind = DomainKind.Public;
            domainpub.Store();

            Domain domainpriv = new Domain(context);
            domainpriv.Identifier = "domainPrivate";
            domainpriv.Kind = DomainKind.Private;
            domainpriv.Store();

            Domain domainusr = new Domain(context);
            domainusr.Identifier = "domainUser";
            domainusr.Kind = DomainKind.User;
            domainusr.Store();

            Domain domaingrp = new Domain(context);
            domaingrp.Identifier = "domainGroup";
            domaingrp.Kind = DomainKind.Group;
            domaingrp.Store();

            Domain domainnone = new Domain(context);
            domainnone.Identifier = "domainNone";
            domainnone.Kind = DomainKind.None;
            domainnone.Store();
        }

        [Test]
        public void DomainListByKindTest(){
            //Load all
            EntityList<Domain> domains = new EntityList<Domain>(context);
            domains.Load();
            var items = domains.GetItemsAsList();
            Assert.That(items.Count == 5);

            //Load only None
            domains = new EntityList<Domain>(context);
            domains.Template.Kind = DomainKind.None;
            domains.Load();
            items = domains.GetItemsAsList();
            Assert.That(items.Count == 5);

            //Load only Public
            domains = new EntityList<Domain>(context);
            domains.Template.Kind = DomainKind.Public;
            domains.Load();
            items = domains.GetItemsAsList();
            Assert.That(items.Count == 1);
            Assert.That(items[0].Identifier == "domainPublic");

            //Load only Private
            domains = new EntityList<Domain>(context);
            domains.Template.Kind = DomainKind.Private;
            domains.Load();
            items = domains.GetItemsAsList();
            Assert.That(items.Count == 1);
            Assert.That(items[0].Identifier == "domainPrivate");

            //Load only Group
            domains = new EntityList<Domain>(context);
            domains.Template.Kind = DomainKind.Group;
            domains.Load();
            items = domains.GetItemsAsList();
            Assert.That(items.Count == 1);
            Assert.That(items[0].Identifier == "domainGroup");

            //Load only User
            domains = new EntityList<Domain>(context);
            domains.Template.Kind = DomainKind.User;
            domains.Load();
            items = domains.GetItemsAsList();
            Assert.That(items.Count == 1);
            Assert.That(items[0].Identifier == "domainUser");

        }

        [Test]
        public void DomainListByKindTest2() {
            context.AccessLevel = EntityAccessLevel.Administrator;
            context.Execute("DELETE FROM domain;");

            User user1 = new User(context);
            user1.Username = "user1";
            user1.Store();

            User user2 = new User(context);
            user2.Username = "user2";
            user2.Store();

            User user3 = new User(context);
            user3.Username = "user3";
            user3.Store();

            Domain domain1 = new Domain(context);
            domain1.Identifier = "domain1";
            domain1.Kind = DomainKind.Private;
            domain1.Store();

            Domain domain2 = new Domain(context);
            domain2.Identifier = "domain2";
            domain2.Kind = DomainKind.Private;
            domain2.Store();

            Domain publicDomain = new Domain(context);
            publicDomain.Identifier = "publicDomain";
            publicDomain.Kind = DomainKind.Public;
            publicDomain.Store();

            Role privateUsageRole = new Role(context);
            privateUsageRole.Identifier = "private";
            privateUsageRole.Store();
            privateUsageRole.GrantToUser(user1, domain1);
            privateUsageRole.GrantToUser(user2, domain2);

            Role grantedRole = new Role(context);
            grantedRole.Identifier = "granted";
            grantedRole.Store();
            grantedRole.GrantToUser(user1, domain2);

            context.StartImpersonation(user1.Id);

            context.ConsoleDebug = true;

            DomainCollection domains = new DomainCollection(context);
            domains.Load();
            Assert.AreEqual(3, domains.Count);
            domains.LoadRestricted();
            Assert.AreEqual(3, domains.Count);
            context.EndImpersonation();

            context.StartImpersonation(user2.Id);
            domains = new DomainCollection(context);
            domains.LoadRestricted();
            Assert.AreEqual(2, domains.Count);
            context.EndImpersonation();

            context.StartImpersonation(user3.Id);
            domains = new DomainCollection(context);
            domains.LoadRestricted();
            Assert.AreEqual(1, domains.Count);
            domains.LoadRestricted(new DomainKind[] { } );
            Assert.AreEqual(0, domains.Count);
            context.EndImpersonation();

        }
    
    }


}

