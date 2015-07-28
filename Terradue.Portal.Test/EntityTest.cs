using System;
using NUnit.Framework;
using System.Collections.Generic;

namespace Terradue.Portal.Test {

    [TestFixture]
    public class EntityTest : BaseTest {

        [Test]
        public void GlobalPrivilegeTest(){

            Series s = new Series(context);
            s.Identifier = "s1";
            s.Name = s.Identifier;
            s.Store();

            Assert.False(s.HasGlobalPrivilege());
            s.StoreGlobalPrivileges();
            Assert.True(s.HasGlobalPrivilege());

            s.Delete();
        }

        [Test]
        public void GroupPrivilegeTest(){
            Series s = new Series(context);
            s.Identifier = "s1";
            s.Name = s.Identifier;
            s.Store();

            Group g1 = new Group(context);
            g1.Identifier = "g1";
            g1.Name = g1.Identifier;
            g1.Store();

            List<int> idgrps = new List<int>();
            idgrps = s.GetGroupsWithPrivileges();
            Assert.That(idgrps.Count == 0);

            s.StorePrivilegesForGroups(new int[]{ g1.Id });
            idgrps = s.GetGroupsWithPrivileges();
            Assert.That(idgrps.Count == 1);

            s.Delete();
            g1.Delete();
        }

        [Test]
        public void UserPrivilegeTest(){
            Series s = new Series(context);
            s.Identifier = "s1";
            s.Name = s.Identifier;
            s.Store();

            User u1 = new User(context);
            u1.Identifier = "u1";
            u1.Name = u1.Identifier;
            u1.Store();

            List<int> idusrs = new List<int>();
            idusrs = s.GetUsersWithPrivileges();
            Assert.That(idusrs.Count == 1);

            s.StorePrivilegesForUsers(new int[]{ u1.Id });
            idusrs = s.GetUsersWithPrivileges();
            Assert.That(idusrs.Count == 2);

            s.Delete();
            u1.Delete();
        }
    }
}

