using System;

//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
using System.Security.Cryptography;
using System.Text;
using System.IO;


namespace Terradue.Portal {

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Safe.
    /// </summary>
    [EntityTable("safe", EntityTableConfiguration.Custom, HasOwnerReference = true, HasPrivilegeManagement = true)]
    public class Safe : Entity {

        private static string SALT = "salt";

        /// <summary>Gets the public key</summary>
        [EntityDataField("public_key")]
        public string PublicKey { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the private key</summary>
        [EntityDataField("private_key")]
        protected string PrivateKey { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the UTC date and time of the safe's creation.</summary>
        [EntityDataField("creation_time")]
        public DateTime CreationTime { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the UTC date and time of the safe's creation.</summary>
        [EntityDataField("update_time")]
        public DateTime UpdateTime { get; protected set; }

        public Safe(IfyContext context) : base(context) {
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static Safe FromId(IfyContext context, int id) {
            Safe result = new Safe(context);
            result.Id = id;
            result.Load();
            return result;
        }

        /// <summary>
        /// Generates the private and public keys.
        /// </summary>
        public void GenerateKeys(string password){
            if (!string.IsNullOrEmpty(this.PublicKey) || !string.IsNullOrEmpty(this.PrivateKey)) throw new Exception("Keys already existing");
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();

            //public key (not encrypted)
            this.PublicKey = rsa.ToXmlString(false);

            //private key (encrypted)
            this.PrivateKey = CipherUtility.Encrypt<AesManaged>(rsa.ToXmlString(true), password, SALT);
        }

        /// <summary>
        /// Clears the public and private keys.
        /// </summary>
        public void ClearKeys(){
            this.PublicKey = null;
            this.PrivateKey = null;
        }

        public string GetPrivateKey(string password){
            if (PrivateKey == null) throw new Exception("Private key has not been generated");
            //decrypt private key
            string decrypted = CipherUtility.Decrypt<AesManaged>(PrivateKey, password, SALT);

            //get RSA
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(decrypted);

            return FromRSAParametersToSSH(rsa.ExportParameters(true));
        }

        public string GetPublicKeyForSSH(){
            if (PublicKey == null) throw new Exception("Public key has not been generated");
            //get RSA
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(this.PublicKey);

            return FromRSAParametersToSSH(rsa.ExportParameters(false));
        }

        public string GetPublicKeyForPutty(){
            if (PublicKey == null) throw new Exception("Public key has not been generated");
            //get RSA
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(this.PublicKey);

            return FromRSAParametersToPutty(rsa.ExportParameters(false));
        }

        private string FromRSAParametersToSSH(RSAParameters parameters){
            return "";
        }

        private string FromRSAParametersToPutty(RSAParameters parameters){
            return "";
        }

    }

    public class CipherUtility
    {
        public static string Encrypt<T>(string value, string password, string salt)
            where T : SymmetricAlgorithm, new()
        {
            DeriveBytes rgb = new Rfc2898DeriveBytes(password, Encoding.Unicode.GetBytes(salt));

            SymmetricAlgorithm algorithm = new T();

            byte[] rgbKey = rgb.GetBytes(algorithm.KeySize >> 3);
            byte[] rgbIV = rgb.GetBytes(algorithm.BlockSize >> 3);

            ICryptoTransform transform = algorithm.CreateEncryptor(rgbKey, rgbIV);

            using (MemoryStream buffer = new MemoryStream())
            {
                using (CryptoStream stream = new CryptoStream(buffer, transform, CryptoStreamMode.Write))
                {
                    using (StreamWriter writer = new StreamWriter(stream, Encoding.Unicode))
                    {
                        writer.Write(value);
                    }
                }

                return Convert.ToBase64String(buffer.ToArray());
            }
        }

        public static string Decrypt<T>(string text, string password, string salt)
            where T : SymmetricAlgorithm, new()
        {
            DeriveBytes rgb = new Rfc2898DeriveBytes(password, Encoding.Unicode.GetBytes(salt));

            SymmetricAlgorithm algorithm = new T();

            byte[] rgbKey = rgb.GetBytes(algorithm.KeySize >> 3);
            byte[] rgbIV = rgb.GetBytes(algorithm.BlockSize >> 3);

            ICryptoTransform transform = algorithm.CreateDecryptor(rgbKey, rgbIV);

            using (MemoryStream buffer = new MemoryStream(Convert.FromBase64String(text)))
            {
                using (CryptoStream stream = new CryptoStream(buffer, transform, CryptoStreamMode.Read))
                {
                    using (StreamReader reader = new StreamReader(stream, Encoding.Unicode))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }
    }
}

