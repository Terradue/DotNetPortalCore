using System;
using System.IO;
using NUnit.Framework;

namespace Terradue.Portal.Test {

    [TestFixture]
    public class TepScenarios {
        
        IfyContext context;

        [TestFixtureSetUp]
        public void CreateEnvironment() {
        }

        [TestFixtureTearDown]
        public void DestroyEnvironment() {
            context.Close();
        }


    }
}

