using System;
using NUnit.Framework;

namespace Terradue.Portal.Test {

    [TestFixture]
    public class RemoteResourceSetTest : BaseTest {

        [Test]
        public void LoadSaveTest(){

            RemoteResourceSet rrs = new RemoteResourceSet(context);
            rrs.AccessKey = "secret";
            rrs.Identifier = "test";

            rrs.Store();

            RemoteResourceSet rrs1 = RemoteResourceSet.FromId(context, rrs.Id);

            Assert.That(rrs.AccessKey == rrs1.AccessKey);

        }


    }
}

