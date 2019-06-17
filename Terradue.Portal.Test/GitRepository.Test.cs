using System;
using NUnit.Framework;
using System.Linq;

namespace Terradue.Portal.Test {

    [TestFixture]
    public class GitRepositoryTest : BaseTest {

        [Test]
        public void GitRepositoryCreationTest() {
            GitRepository repo1 = new GitRepository(context);
            repo1.Url = "https://git.terradue.com/fruit/tep.banners";
            repo1.Store();

            EntityList<GitRepository> repos = new EntityList<GitRepository>(context);
            repos.Load();
            Assert.AreEqual(1, repos.Items.Count());

            repo1.Delete();
            repos.Load();
            Assert.AreEqual(0, repos.Items.Count());
        }

        [Test]
        public void GitRepositoryAddForUser() {

            context.AccessLevel = EntityAccessLevel.Administrator;

            User usr1 = new User(context);
            usr1.Username = "testusr1";
            usr1.Store();

            User usr2 = new User(context);
            usr2.Username = "testusr2";
            usr2.Store();

            GitRepository repo1 = new GitRepository(context);
            repo1.Url = "https://git.terradue.com/fruit/tep.banners";
            repo1.Store();

            context.AccessLevel = EntityAccessLevel.Permission;

            //usr1 should not have access to the git repo
            EntityList<GitRepository> repos = new EntityList<GitRepository>(context);
            context.StartImpersonation(usr1.Id);
            repos.Load();
            Assert.AreEqual(0, repos.Items.Count());
            context.EndImpersonation();

            //we grant access to usr1
            repo1.GrantPermissionsToUsers(new int[]{ usr1.Id});

            Assert.True(repo1.DoesGrantPermissionsToUser(usr1.Id));

            //usr1 should have access to the git repo
            repos = new EntityList<GitRepository>(context);
            context.StartImpersonation(usr1.Id);
            repos.Load();
            Assert.AreEqual(1, repos.Items.Count());
            context.EndImpersonation();

            //usr2 should not have access to the git repo
            repos = new EntityList<GitRepository>(context);
            context.StartImpersonation(usr2.Id);
            repos.Load();
            Assert.AreEqual(0, repos.Items.Count());
            context.EndImpersonation();

            Group grp1 = new Group(context);
            grp1.Identifier = "testgrp1";
            grp1.Name = "Group Test 1";
            grp1.Store();
            grp1.AssignUser(usr2);

            //we grant access to grp1
            repo1.GrantPermissionsToGroups(new int[] { grp1.Id });

            //usr2 should have access to the git repo
            repos = new EntityList<GitRepository>(context);
            context.StartImpersonation(usr2.Id);
            repos.Load();
            Assert.AreEqual(1, repos.Items.Count());
            context.EndImpersonation();
        }

    }
}

