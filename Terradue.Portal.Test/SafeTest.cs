using System;
using NUnit.Framework;

namespace Terradue.Portal.Test {

    [TestFixture]
    public class SafeTest : BaseTest {

        private const string password = "Test99++";
        private string privatekey = null, publickey = null;

        public Safe safe {get; set;}

        public void InitSafe(){
            User admin = User.FromId(context, context.UserId);
            safe = new Safe(context);
            safe.OwnerId = admin.Id;
            try{
                safe.Store();
            }catch(Exception e){
                throw e;
            }
        }

        [Test]
        public void SafeCreationTest(){

            if (safe == null) InitSafe();

            //if public key is not generated, we get an exception
            bool exceptionpub = false;
            try{
                publickey = safe.GetBase64SSHPublicKey();
            }catch(Exception e){
                exceptionpub = true;
            }
            Assert.IsTrue(exceptionpub);

            //if private key is not generated, we get an exception
            bool exceptionpriv = false;
            try{
                privatekey = safe.GetBase64SSHPrivateKey(password);
            }catch(Exception e){
                exceptionpriv = true;
            }
            Assert.IsTrue(exceptionpriv);
        }

        [Test]
        public void SafeKeyGenerationTest(){
            if (safe == null) InitSafe();

            safe.GenerateKeys(password);
            safe.Store();

            //if public key is not generated, we get an exception
            bool exceptionpub = false;
            try{
                publickey = safe.GetBase64SSHPublicKey();
            }catch(Exception e){
                exceptionpub = true;
            }
            Assert.IsFalse(exceptionpub);

            //if private key is generated, we don't get an exception
            bool exception = false;
            try{
                privatekey = safe.GetBase64SSHPrivateKey(password);
            }catch(Exception e){
                exception = true;
            }
            Assert.IsFalse(exception);

            safe.ClearKeys();
            safe.Store();
        }

        [Test]
        public void SafeKeyClearTest(){
            if (safe == null) InitSafe();

            safe.GenerateKeys(password);
            safe.Store();

            safe.ClearKeys();
            safe.Store();

            SafeCreationTest();
        }

        [Test]
        public void SafeKeyPuttyTest(){
            if (safe == null) InitSafe();

            safe.GenerateKeys(password);
            safe.Store();

            string putty = safe.GetKeysForPutty(password);

            safe.ClearKeys();
            safe.Store();
        }

        [Test]
        public void SafeKeySSHTest(){
            if (safe == null) InitSafe();

            safe.GenerateKeys(password);
            safe.Store();

            string sshpub64 = safe.GetBase64SSHPublicKey();
            Assert.That(sshpub64.Contains("ssh-rsa"));
            string sshpriv64 = safe.GetBase64SSHPrivateKey(password);
            Assert.That(sshpriv64.Contains("-----BEGIN RSA PRIVATE KEY-----"));
            Assert.That(sshpriv64.Contains("-----END RSA PRIVATE KEY-----"));                        

            safe.ClearKeys();
            safe.Store();
        }

    }
}

