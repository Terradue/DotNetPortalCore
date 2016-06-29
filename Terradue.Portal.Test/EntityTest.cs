using System;
using NUnit.Framework;
using System.Collections.Generic;

namespace Terradue.Portal.Test {

    [TestFixture]
    public class EntityTest : BaseTest {

        [Test]
        public void GlobalPrivilegeTest() {
            context.AccessLevel = EntityAccessLevel.Administrator;

            Series s = new Series(context);
            s.Identifier = "s1";
            s.Name = s.Identifier;
            s.Store();

            Assert.False(s.DoesGrantGlobalPermission());
            s.GrantGlobalPermissions();
            Assert.True(s.DoesGrantGlobalPermission());

            s.Delete();
        }

        [Test]
        public void GroupPrivilegeTest(){
            context.AccessLevel = EntityAccessLevel.Administrator;

            Series s = new Series(context);
            s.Identifier = "s1";
            s.Name = s.Identifier;
            s.Store();

            Group g1 = new Group(context);
            g1.Identifier = "g1";
            g1.Name = g1.Identifier;
            g1.Store();

            int[] idgrps = s.GetAuthorizedGroupIds();
            Assert.That(idgrps.Length == 0);

            s.GrantPermissionsToGroups(new int[]{ g1.Id });
            idgrps = s.GetAuthorizedGroupIds();
            Assert.That(idgrps.Length == 1);

            s.Delete();
            g1.Delete();
        }

        [Test]
        public void UserPrivilegeTest(){
            context.AccessLevel = EntityAccessLevel.Administrator;

            Series s = new Series(context);
            s.Identifier = "s1";
            s.Name = s.Identifier;
            s.Store();

            User u1 = new User(context);
            u1.Identifier = "u1";
            u1.Name = u1.Identifier;
            u1.Store();

            int[] idusrs = s.GetAuthorizedUserIds();
            Assert.That(idusrs.Length == 0);

            s.GrantPermissionsToUsers(new int[]{ u1.Id });
            idusrs = s.GetAuthorizedUserIds();
            Assert.That(idusrs.Length == 1);

            s.Delete();
            u1.Delete();
        }
    }
}

