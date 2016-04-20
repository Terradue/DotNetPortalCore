using System;

//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;


namespace Terradue.Portal {

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Safe.
    /// </summary>
    [EntityTable("safe", EntityTableConfiguration.Custom, HasOwnerReference = true, HasPermissionManagement = true)]
    public class Safe : Entity {

        //private static string SALT = "salt";

        /// <summary>Gets the public key</summary>
        [EntityDataField("public_key")]
        public string PublicKey { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the private key</summary>
//        [EntityDataField("private_key")]
        public string PrivateKey { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the UTC date and time of the safe's creation.</summary>
        [EntityDataField("creation_time")]
        public DateTime CreationTime { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the UTC date and time of the safe's creation.</summary>
        [EntityDataField("update_time")]
        public DateTime UpdateTime { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------------------

        public Safe(IfyContext context) : base(context) {
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override string AlternativeIdentifyingCondition{
            get { 
                if (UserId != 0) return String.Format("t.id_usr={0}",UserId); 
                return null;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static Safe FromId(IfyContext context, int id) {
            Safe result = new Safe(context);
            result.Id = id;
            result.Load();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static Safe FromUserId(IfyContext context, int usrid) {
            Safe result = new Safe(context);
            result.UserId = usrid;
            result.Load();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Generates the private and public keys.
        /// </summary>
        public void GenerateKeys(){
            if (!string.IsNullOrEmpty(this.PublicKey) || !string.IsNullOrEmpty(this.PrivateKey)) throw new Exception("Keys already existing");
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();

            //public key (not encrypted)
            this.PublicKey = rsa.ToXmlString(false);

            //private key (not encrypted)
            this.PrivateKey = rsa.ToXmlString(true);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Clears the public and private keys.
        /// </summary>
        public void ClearKeys(){
            this.PublicKey = null;
            this.PrivateKey = null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the private key in base64.
        /// </summary>
        /// <returns>The private key in base64.</returns>
        /// <param name="password">Password.</param>
        public string GetBase64SSHPrivateKey(){
            if (PrivateKey == null) throw new Exception("Private key has not been generated");

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(this.PrivateKey);

            var privateBlob = System.Convert.ToBase64String(PrivateKeyBase64FromRSAParametersToSSH(rsa.ExportParameters(true)));

            var sb = new StringBuilder();
            var privateLines = SpliceText(privateBlob, 64);
            sb.AppendLine("-----BEGIN RSA PRIVATE KEY-----");
            foreach (var line in privateLines) sb.AppendLine(line);
            sb.AppendLine("-----END RSA PRIVATE KEY-----");
            return sb.ToString();
        }

        public string GetBase64SSHPrivateKeyOpenSSL(){
            var sb = new StringBuilder();

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(this.PrivateKey);

            if (rsa.PublicOnly) throw new ArgumentException("CSP does not contain a private key", "csp");
            var parameters = rsa.ExportParameters(true);
            using (var stream = new MemoryStream())
            {
                var writer = new BinaryWriter(stream);
                writer.Write((byte)0x30); // SEQUENCE
                using (var innerStream = new MemoryStream())
                {
                    var innerWriter = new BinaryWriter(innerStream);
                    EncodeIntegerBigEndian(innerWriter, new byte[] { 0x00 }); // Version
                    EncodeIntegerBigEndian(innerWriter, parameters.Modulus);
                    EncodeIntegerBigEndian(innerWriter, parameters.Exponent);
                    EncodeIntegerBigEndian(innerWriter, parameters.D);
                    EncodeIntegerBigEndian(innerWriter, parameters.P);
                    EncodeIntegerBigEndian(innerWriter, parameters.Q);
                    EncodeIntegerBigEndian(innerWriter, parameters.DP);
                    EncodeIntegerBigEndian(innerWriter, parameters.DQ);
                    EncodeIntegerBigEndian(innerWriter, parameters.InverseQ);
                    var length = (int)innerStream.Length;
                    EncodeLength(writer, length);
                    writer.Write(innerStream.GetBuffer(), 0, length);
                }

                var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length).ToCharArray();
                var privateLines = SpliceText(new string(base64), 64);
                sb.AppendLine("-----BEGIN RSA PRIVATE KEY-----");
                foreach (var line in privateLines) sb.AppendLine(line);
                sb.AppendLine("-----END RSA PRIVATE KEY-----");
            }

            return sb.ToString();
        }


        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the public key for SSH in base64.
        /// </summary>
        /// <returns>The public key for SSH in base64.</returns>
        public string GetBase64SSHPublicKey(){
            if (PublicKey == null) throw new Exception("Public key has not been generated");

            //get RSA
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(this.PublicKey);

            var publicblob = System.Convert.ToBase64String(PublicKeyBase64FromRSAParametersToSSH(rsa.ExportParameters(false)));
            return "ssh-rsa " + publicblob;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the keys for putty.
        /// </summary>
        /// <returns>The keys for putty.</returns>
        /// <param name="password">Password.</param>
        public string GetKeysForPutty(){
            if (PrivateKey == null) throw new Exception("Private key has not been generated");

            //get RSA
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(PrivateKey);

            return FromRSAParametersToPutty(rsa);
        }

        //---------------------------------------------------------------------------------------------------------------------

        private byte[] PublicKeyBase64FromRSAParametersToSSH(RSAParameters parameters){
            byte[] publicBuffer = new byte[3 + 7 + 4 + 1 + parameters.Exponent.Length + 4 + 1 + parameters.Modulus.Length + 1];

            using (var bw = new BinaryWriter(new MemoryStream(publicBuffer)))
            {
                bw.Write(new byte[] { 0x00, 0x00, 0x00 });
                bw.Write("ssh-rsa");
                PutPrefixed(bw, parameters.Exponent, true);
                PutPrefixed(bw, parameters.Modulus, true);
            }
            return publicBuffer;
        }

        //---------------------------------------------------------------------------------------------------------------------

        private byte[] PrivateKeyBase64FromRSAParametersToSSH(RSAParameters parameters){
            byte[] privateBuffer = new byte[4 + 1 + parameters.D.Length + 4 + 1 + parameters.P.Length + 4 + 1 + parameters.Q.Length + 4 + 1 + parameters.InverseQ.Length];

            using (var bw = new BinaryWriter(new MemoryStream(privateBuffer)))
            {
                PutPrefixed(bw, parameters.D, true);
                PutPrefixed(bw, parameters.P, true);
                PutPrefixed(bw, parameters.Q, true);
                PutPrefixed(bw, parameters.InverseQ, true);
            }

            return privateBuffer;
        }

        //---------------------------------------------------------------------------------------------------------------------

        //cf https://gist.github.com/canton7/5670788
        private string FromRSAParametersToPutty(RSACryptoServiceProvider rsa){
            
            var publicBuffer = PublicKeyBase64FromRSAParametersToSSH(rsa.ExportParameters(false));
            var privateBuffer = PrivateKeyBase64FromRSAParametersToSSH(rsa.ExportParameters(true));

            var publicBlob = System.Convert.ToBase64String(PublicKeyBase64FromRSAParametersToSSH(rsa.ExportParameters(false)));
            var privateBlob = System.Convert.ToBase64String(PrivateKeyBase64FromRSAParametersToSSH(rsa.ExportParameters(true)));

            HMACSHA1 hmacsha1 = new HMACSHA1(new SHA1CryptoServiceProvider().ComputeHash(Encoding.ASCII.GetBytes("putty-private-key-file-mac-key")));
            byte[] bytesToHash = new byte[4 + 7 + 4 + 4 + 4 + 4 + publicBuffer.Length + 4 + privateBuffer.Length];

            using (var bw = new BinaryWriter(new MemoryStream(bytesToHash)))
            {
                PutPrefixed(bw, Encoding.ASCII.GetBytes("ssh-rsa"));
                PutPrefixed(bw, Encoding.ASCII.GetBytes("none"));
                PutPrefixed(bw, publicBuffer);
                PutPrefixed(bw, privateBuffer);
            }

            var hash = string.Join("", hmacsha1.ComputeHash(bytesToHash).Select(x => string.Format("{0:x2}", x)));

            var sb = new StringBuilder();
            sb.AppendLine("PuTTY-User-Key-File-2: ssh-rsa");
            sb.AppendLine("Encryption: none");

            var publicLines = SpliceText(publicBlob, 64);
            sb.AppendLine("Public-Lines: " + publicLines.Length);
            foreach (var line in publicLines)
            {
                sb.AppendLine(line);
            }

            var privateLines = SpliceText(privateBlob, 64);
            sb.AppendLine("Private-Lines: " + privateLines.Length);
            foreach (var line in privateLines)
            {
                sb.AppendLine(line);
            }

            sb.AppendLine("Private-MAC: " + hash);

            return sb.ToString();
        }

        //---------------------------------------------------------------------------------------------------------------------

        private static void PutPrefixed(BinaryWriter bw, byte[] bytes, bool addLeadingNull = false)
        {
            bw.Write(BitConverter.GetBytes(bytes.Length + (addLeadingNull ? 1 : 0)).Reverse().ToArray());
            if (addLeadingNull)
                bw.Write(new byte[] { 0x00 });
            bw.Write(bytes);
        }

        //---------------------------------------------------------------------------------------------------------------------
            
        private static string[] SpliceText(string text, int lineLength)
        {
            return Regex.Matches(text, ".{1," + lineLength + "}").Cast<Match>().Select(m => m.Value).ToArray();
        }

        private static void EncodeLength(BinaryWriter stream, int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException("length", "Length must be non-negative");
            if (length < 0x80)
            {
                // Short form
                stream.Write((byte)length);
            }
            else
            {
                // Long form
                var temp = length;
                var bytesRequired = 0;
                while (temp > 0)
                {
                    temp >>= 8;
                    bytesRequired++;
                }
                stream.Write((byte)(bytesRequired | 0x80));
                for (var i = bytesRequired - 1; i >= 0; i--)
                {
                    stream.Write((byte)(length >> (8 * i) & 0xff));
                }
            }
        }

        private static void EncodeIntegerBigEndian(BinaryWriter stream, byte[] value, bool forceUnsigned = true)
        {
            stream.Write((byte)0x02); // INTEGER
            var prefixZeros = 0;
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] != 0) break;
                prefixZeros++;
            }
            if (value.Length - prefixZeros == 0)
            {
                EncodeLength(stream, 1);
                stream.Write((byte)0);
            }
            else
            {
                if (forceUnsigned && value[prefixZeros] > 0x7f)
                {
                    // Add a prefix zero to force unsigned if the MSB is 1
                    EncodeLength(stream, value.Length - prefixZeros + 1);
                    stream.Write((byte)0);
                }
                else
                {
                    EncodeLength(stream, value.Length - prefixZeros);
                }
                for (var i = prefixZeros; i < value.Length; i++)
                {
                    stream.Write(value[i]);
                }
            }
        }

    }
}

