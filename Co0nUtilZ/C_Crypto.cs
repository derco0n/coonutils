using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace Co0nUtilZ
{
    /// <summary>
    /// Verschlüsselunghelfer-Klasse.
    /// Erstellt:           10/2017
    /// Autor:              D. Marx 
    /// </summary>
    public class C_Crypto
    {
        /// <summary>
        /// Verschlüsselt (mit AES) ein Bytearray
        /// </summary>
        /// <param name="bytesToBeEncrypted">Eingabe</param>
        /// <param name="passwordBytes">Schlüssel</param>
        /// <param name="saltBytes">Salt als Bytearray mit mindestens 8 Werten</param>
        /// <returns>verschlüsselte Ausgabe</returns>
        public byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes, byte[] saltBytes)
        {
            byte[] encryptedBytes = null;

            // Set your salt here, change it to meet your flavor:
            // The salt bytes must be at least 8 bytes.
            if (saltBytes.Length < 8)
            {
                saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }

            return encryptedBytes;
        }

        /// <summary>
        /// Entschlüsselt ein mit AES verschlüsseltes ByteArray
        /// </summary>
        /// <param name="bytesToBeDecrypted">Eingabe</param>
        /// <param name="passwordBytes">Entschlüsselungskey</param>
        /// <param name="saltBytes">Salt als Bytearray mit mindestens 8 Werten</param>
        /// <returns>Entschlüsseltes Bytearray</returns>
        public byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes, byte[] saltBytes)
        {
            byte[] decryptedBytes = null;

            // Set your salt here, change it to meet your flavor:
            // The salt bytes must be at least 8 bytes.
            if (saltBytes.Length < 8)
            {
                saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }

            return decryptedBytes;
        }


        /// <summary>
        /// Verschlüsselt einen String
        /// </summary>
        /// <param name="input">String zum Verschlüsseln</param>
        /// <param name="password">Verschlüsselungspasswort</param>
        /// <param name="saltBytes">Salt als Bytearray mit mindestens 8 Werten</param>
        /// <returns>Verschlüsselter String</returns>
        public string EncryptText(string input, string password, byte[] saltBytes, string error = "Fehler beim Verschlüsseln.")
        {
            string result = error;
            try
            {
                // Get the bytes of the string
                byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes(input);
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

                // Hash the password with SHA256
                passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

                byte[] bytesEncrypted = AES_Encrypt(bytesToBeEncrypted, passwordBytes, saltBytes);

                result = Convert.ToBase64String(bytesEncrypted);
            }
            catch (Exception ex)
            {
                // result += "\r\n" + ex.ToString();
            }

            return result;
        }


        /// <summary>
        /// Entschlüsselt einen String
        /// </summary>
        /// <param name="input">Verschlüsselter String</param>
        /// <param name="password">Passwort zum Entschlüsseln</param>
        /// <param name="saltBytes">Salt als Bytearray mit mindestens 8 Werten</param>
        /// <returns>Entschlüsselter String</returns>
        public string DecryptText(string input, string password, byte[] saltBytes, string error = "Fehler beim Entschlüsseln")
        {

            string result = error;
            try
            {
                // Get the bytes of the string
                byte[] bytesToBeDecrypted = Convert.FromBase64String(input);
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

                byte[] bytesDecrypted = AES_Decrypt(bytesToBeDecrypted, passwordBytes, saltBytes);

                result = Encoding.UTF8.GetString(bytesDecrypted);
            }
            catch (Exception ex)
            {
                // result += "\r\n" + ex.ToString();
            }

            return result;
        }
    }
}
