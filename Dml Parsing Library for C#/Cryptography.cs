using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Security.Cryptography;

namespace WileyBlack.Cryptography
{
    public class AES
    {
        private static byte[] _salt = Encoding.ASCII.GetBytes("o66a663zkbg7a5");

        /// <summary>
        /// Encrypt the given string using AES.  The string can be decrypted using 
        /// DecryptStringAES().  The sharedSecret parameters must match.
        /// </summary>
        /// <param name="plainText">The text to encrypt.</param>
        /// <param name="sharedSecret">A password used to generate a key for encryption.</param>
        public static string EncryptStringAES(string plainText, string sharedSecret)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException("plainText");
            if (string.IsNullOrEmpty(sharedSecret))
                throw new ArgumentNullException("sharedSecret");

            string outStr = null;                       // Encrypted string to return
            RijndaelManaged aesAlg = null;              // RijndaelManaged object used to encrypt the data.

            try
            {
                // generate the key from the shared secret and the salt
                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);

                // Create a RijndaelManaged object
                aesAlg = new RijndaelManaged();
                aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
                aesAlg.GenerateIV();

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    // prepend the IV
                    msEncrypt.Write(BitConverter.GetBytes(aesAlg.IV.Length), 0, sizeof(int));
                    msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                    }
                    outStr = Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
            finally
            {
                // Clear the RijndaelManaged object.
                if (aesAlg != null)
                    aesAlg.Clear();
            }

            // Return the encrypted bytes from the memory stream.
            return outStr;
        }

        /// <summary>
        /// Decrypt the given string.  Assumes the string was encrypted using 
        /// EncryptStringAES(), using an identical sharedSecret.
        /// </summary>
        /// <param name="cipherText">The text to decrypt.</param>
        /// <param name="sharedSecret">A password used to generate a key for decryption.</param>
        public static string DecryptStringAES(string cipherText, string sharedSecret)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentNullException("cipherText");
            if (string.IsNullOrEmpty(sharedSecret))
                throw new ArgumentNullException("sharedSecret");

            // Declare the RijndaelManaged object
            // used to decrypt the data.
            RijndaelManaged aesAlg = null;

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            try
            {
                // generate the key from the shared secret and the salt
                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);

                // Create the streams used for decryption.                
                byte[] bytes = Convert.FromBase64String(cipherText);
                using (MemoryStream msDecrypt = new MemoryStream(bytes))
                {
                    // Create a RijndaelManaged object
                    // with the specified key and IV.
                    aesAlg = new RijndaelManaged();
                    aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
                    // Get the initialization vector from the encrypted stream
                    aesAlg.IV = ReadByteArray(msDecrypt);
                    // Create a decrytor to perform the stream transform.
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                    }
                }
            }
            finally
            {
                // Clear the RijndaelManaged object.
                if (aesAlg != null)
                    aesAlg.Clear();
            }

            return plaintext;
        }

        private static byte[] ReadByteArray(Stream s)
        {
            byte[] rawLength = new byte[sizeof(int)];
            if (s.Read(rawLength, 0, rawLength.Length) != rawLength.Length)
            {
                throw new SystemException("Stream did not contain properly formatted byte array");
            }

            byte[] buffer = new byte[BitConverter.ToInt32(rawLength, 0)];
            if (s.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                throw new SystemException("Did not read byte array properly");
            }

            return buffer;
        }
    }

    #region "Digital Signature"
    
    [XmlRoot("RSA-Public-Key")]
    public class RSAPublicKey
    {
        public string Modulus;
        public string Exponent;

        public RSAPublicKey()
        {
        }

        public RSAPublicKey(RSAParameters Params)
        {
            Modulus = Convert.ToBase64String(Params.Modulus);
            Exponent = Convert.ToBase64String(Params.Exponent);
        }

        public RSAPublicKey(RSAPrivateKey Key)
        {
            Modulus = Key.Modulus;
            Exponent = Key.Exponent;
        }

        public RSAParameters ToRSAParameters()
        {
            RSAParameters ret = new RSAParameters();
            ret.Modulus = Convert.FromBase64String(Modulus);
            ret.Exponent = Convert.FromBase64String(Exponent);
            return ret;
        }

        public override string ToString()
        {
            return "RSAPublicKey (Modulus = " + Modulus + ", Exponent = " + Exponent + ")";
        }

        public string ToParsableString()
        {
            // The vertical bar character (|) is not part of the base-64 set and makes a good separator.
            return Modulus + "|" + Exponent;
        }

        public static bool TryParse(string text, out RSAPublicKey key)
        {
            int iSep = text.IndexOf('|');
            if (iSep < 0) { key = null; return false; }
            string Modulus = text.Substring(0, iSep);
            string Exponent = text.Substring(iSep + 1);
            key = new RSAPublicKey();
            key.Modulus = Modulus;
            key.Exponent = Exponent;
            return true;
        }

        public static RSAPublicKey Parse(string text)
        {
            RSAPublicKey ret;
            if (!TryParse(text, out ret))
                throw new FormatException("Expected RSAPublicKey formatted string.");
            return ret;
        }
    }

    [XmlRoot("RSA-Private-Key")]
    public class RSAPrivateKey
    {
        public string D;
        public string DP;
        public string DQ;
        public string Exponent;
        public string InverseQ;
        public string Modulus;
        public string P;
        public string Q;

        private void From(RSAParameters Params)
        {
            D = Convert.ToBase64String(Params.D);
            DP = Convert.ToBase64String(Params.DP);
            DQ = Convert.ToBase64String(Params.DQ);
            Exponent = Convert.ToBase64String(Params.Exponent);
            InverseQ = Convert.ToBase64String(Params.InverseQ);
            Modulus = Convert.ToBase64String(Params.Modulus);
            P = Convert.ToBase64String(Params.P);
            Q = Convert.ToBase64String(Params.Q);
        }

        public RSAPrivateKey(RSAParameters Params) { From(Params); }

        public RSAPrivateKey()
        {
            RSACryptoServiceProvider RSAAlg = new RSACryptoServiceProvider(2048);
            From(RSAAlg.ExportParameters(true));
        }

        public RSAPrivateKey(int KeyLength)
        {
            RSACryptoServiceProvider RSAAlg = new RSACryptoServiceProvider(KeyLength);
            From(RSAAlg.ExportParameters(true));
        }

        public RSAPublicKey PublicKey
        {
            get
            {
                return new RSAPublicKey(this);
            }
        }

        public RSAParameters ToRSAParameters()
        {
            RSAParameters ret = new RSAParameters();
            ret.D = Convert.FromBase64String(D);
            ret.DP = Convert.FromBase64String(DP);
            ret.DQ = Convert.FromBase64String(DQ);
            ret.Exponent = Convert.FromBase64String(Exponent);
            ret.InverseQ = Convert.FromBase64String(InverseQ);
            ret.Modulus = Convert.FromBase64String(Modulus);
            ret.P = Convert.FromBase64String(P);
            ret.Q = Convert.FromBase64String(Q);
            return ret;
        }
    }

    [XmlRoot("Digital-Signature")]
    public class DigitalSignature
    {
        public RSAPublicKey PublicKey;
        public string DigestValue;
        public string HashMethod = "SHA512";

        public DigitalSignature() { }
    }

    [XmlRoot("Signed-XML-Message")]
    public class SignedXMLMessage
    {
        public string Payload;
        public DigitalSignature Signature;

        public SignedXMLMessage() { }

        #region "Outer XML Serialization"

        private static XmlSerializer SignedXMLMessageSerializer = new XmlSerializer(typeof(SignedXMLMessage));

        public string Serialize()
        {
            using (StringWriter sw = new StringWriter())
            {
                SignedXMLMessageSerializer.Serialize(sw, this);
                return sw.ToString();
            }
        }

        public void SerializeTo(Stream stream)
        {
            SignedXMLMessageSerializer.Serialize(stream, this);
        }

        public static SignedXMLMessage Deserialize(string Text)
        {
            using (StringReader sr = new StringReader(Text))
                return SignedXMLMessageSerializer.Deserialize(sr) as SignedXMLMessage;
        }

        public static SignedXMLMessage Deserialize(Stream stream)
        {
            return SignedXMLMessageSerializer.Deserialize(stream) as SignedXMLMessage;
        }

        #endregion

        #region "Payload packing and extraction"

        private void DoWrap(string Payload)
        {
            Signature = new DigitalSignature();

            // Generate new public-private key pair...
            RSACryptoServiceProvider RSAAlg = new RSACryptoServiceProvider(2048);

            byte[] RawMessage = Encoding.UTF8.GetBytes(Payload);
            this.Payload = Convert.ToBase64String(RawMessage);
            string OID = CryptoConfig.MapNameToOID(Signature.HashMethod);
            byte[] RawSignature = RSAAlg.SignData(RawMessage, OID);
            Signature.PublicKey = new RSAPublicKey(RSAAlg.ExportParameters(false));
            Signature.DigestValue = Convert.ToBase64String(RawSignature);
        }

        private void DoWrapKnownKey(string Payload, RSAParameters Keys)
        {
            Signature = new DigitalSignature();

            RSACryptoServiceProvider RSAAlg = new RSACryptoServiceProvider();
            RSAAlg.ImportParameters(Keys);

            byte[] RawMessage = Encoding.UTF8.GetBytes(Payload);
            this.Payload = Convert.ToBase64String(RawMessage);
            byte[] RawSignature = RSAAlg.SignData(RawMessage, CryptoConfig.MapNameToOID(Signature.HashMethod));
            Signature.DigestValue = Convert.ToBase64String(RawSignature);
        }

        /** Versions that embed the public key into the message **/
        public SignedXMLMessage(string Payload) { DoWrap(Payload); }
        public SignedXMLMessage(XmlDocument Document) { DoWrap(Document.OuterXml); }

        /** Versions that require the receiver to have a priori knowledge of the public key **/
        public SignedXMLMessage(string Payload, RSAParameters Keys) { DoWrapKnownKey(Payload, Keys); }
        public SignedXMLMessage(XmlDocument Document, RSAParameters Keys) { DoWrap(Document.OuterXml); }

        public string ExtractPayload(RSAPublicKey Key)
        {
            byte[] RawMessage = Convert.FromBase64String(this.Payload);
            Payload = Encoding.UTF8.GetString(RawMessage);

            using (RSACryptoServiceProvider RSAAlg = new RSACryptoServiceProvider())
            {
                RSAAlg.ImportParameters(Key.ToRSAParameters());

                if (!RSAAlg.VerifyData(RawMessage, CryptoConfig.MapNameToOID(Signature.HashMethod), Convert.FromBase64String(Signature.DigestValue)))
                    throw new Exception("Digital signature is not valid.");
            }

            return Payload;
        }

        public string ExtractPayload() { return ExtractPayload(Signature.PublicKey); }

        /// <summary>
        /// Extract the signed document contained inside the SignedXMLMessage.  If the authenticity of the
        /// signed document does not verify, then an exception is thrown.
        /// </summary>
        /// <returns></returns>
        public XmlDocument ExtractDocument()
        {
            string DocAsString = ExtractPayload();
            XmlDocument ret = new XmlDocument();
            ret.LoadXml(DocAsString);
            return ret;
        }

        /// <summary>
        /// Extract the signed document contained inside the SignedXMLMessage using public key known a priori.  If 
        /// the authenticity of the signed document does not verify, then an exception is thrown.
        /// </summary>
        /// <returns></returns>
        public XmlDocument ExtractDocument(RSAPublicKey Key)
        {
            string DocAsString = ExtractPayload(Key);
            XmlDocument ret = new XmlDocument();
            ret.LoadXml(DocAsString);
            return ret;
        }

        #endregion
    }

    #endregion
}
