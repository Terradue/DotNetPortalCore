using System;
using NUnit.Framework;

namespace Terradue.Portal.Test {

    [TestFixture]
    public class FeatureTest : BaseTest {

        [Test]
        public void FeatureCreationTest(){
            
            Feature feat1 = new Feature(context);
            feat1.Title = "feat1";
            feat1.Position = 1;
            feat1.Store();

            Feature feat2 = new Feature(context);
            feat2.Title = "feat2";
            feat2.Position = 2;
            feat2.Store();

            Feature feat3 = new Feature(context);
            feat3.Title = "feat3";
            feat3.Position = 3;
            feat3.Store();
        }

        [Test]
        public void PositionTest(){
            EntityList<Feature> features = new EntityList<Feature>(context);
            features.Load();

            System.Collections.Generic.List<Terradue.Portal.Feature> fs = features.GetItemsAsList();
            fs.Sort();

            Assert.That(fs[0].Title.Equals("feat1"));
            Assert.That(fs[1].Title.Equals("feat2"));
            Assert.That(fs[2].Title.Equals("feat3"));

            //change order
            fs[0].Position = 3;
            fs[0].Store();
            fs[2].Position = 1;
            fs[2].Store();

            features.Load();
            fs = features.GetItemsAsList();
            fs.Sort();

            Assert.That(fs[0].Title.Equals("feat3"));
            Assert.That(fs[1].Title.Equals("feat2"));
            Assert.That(fs[2].Title.Equals("feat1"));

            //insert new feature in the middle
//            Feature feat4 = new Feature(context);
//            feat4.Title = "feat4";
//            feat4.Position = 2;
//            feat4.Store();
//
//            features.Load();
//            fs = features.GetItemsAsList();
//            fs.Sort();
//
//            Assert.That(fs[0].Title.Equals("feat3"));
//            Assert.That(fs[1].Title.Equals("feat4"));
//            Assert.That(fs[2].Title.Equals("feat2"));
//            Assert.That(fs[3].Title.Equals("feat1"));
        }
    }
}

