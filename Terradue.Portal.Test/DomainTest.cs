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
    }
}

