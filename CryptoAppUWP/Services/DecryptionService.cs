using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoAppUWP.Services
{
    public class DecryptionResult
    {
        public bool Result { get; set; }

        public string Error { get; set; }

        public string DecryptedString { get; set; }

        public byte[] DecryptedContents { get; set; }

        public string WritePath { get; set; }
    }

    public class DecryptionService
    {
        public static Tuple<decimal, string> GetFormatedSize(Int64 bytes)
        {
            string[] suffixes = { "Bytes", "KB", "MB", "GB", "TB", "PB" };
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return new Tuple<decimal, string>(number, suffixes[counter]);
        }

        public static byte[] GetDecryptedByteArray(byte[] bytesDecrypted, byte[] password)
        {
            byte[] decrypted = null;
            byte[] saltBytes = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 };
            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged aes = new RijndaelManaged())
                {
                    aes.KeySize = 256;
                    aes.BlockSize = 128;
                    var key = new Rfc2898DeriveBytes(password, saltBytes, 1000);
                    aes.Key = key.GetBytes(aes.KeySize / 8);
                    aes.IV = key.GetBytes(aes.BlockSize / 8);
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.Zeros;
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesDecrypted, 0, bytesDecrypted.Length);
                        cs.Close();
                    }
                    decrypted = ms.ToArray();
                }
            }
            return decrypted;
        }

        public static DecryptionResult DecryptText(string inputText, string password)
        {
            DecryptionResult decryptionResult = null;
            try
            {
                byte[] EncryptedBytes = null;
                try
                {
                    EncryptedBytes = System.Text.Encoding.UTF8.GetBytes(inputText);
                }
                catch (Exception)
                {
                    decryptionResult = new DecryptionResult()
                    {
                        Result = false,
                        Error = "Text is not a valid encrypted message",
                        DecryptedString = null
                    };
                    return decryptionResult;
                }
                byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
                passwordBytes = SHA256.Create().ComputeHash(passwordBytes);
                byte[] DecryptedBytes = DecryptionService.GetDecryptedByteArray(EncryptedBytes, passwordBytes);
                try
                {
                    decryptionResult = new DecryptionResult()
                    {
                        DecryptedString = System.Text.Encoding.UTF8.GetString(DecryptedBytes),
                        Result = true,
                        Error = null
                    };
                }
                catch (Exception)
                {
                    decryptionResult = new DecryptionResult()
                    {
                        Result = false,
                        Error = "Text is not a valid encrypted message",
                        DecryptedString = null
                    };
                    return decryptionResult;
                }
            }
            catch (Exception ex)
            {
                decryptionResult.Result = false;
                decryptionResult.Error = ex.Message;
            }
            return decryptionResult;
        }

        public async static Task<DecryptionResult> DecryptFile(Windows.Storage.StorageFile inputFile, string password)
        {
            DecryptionResult decryptionResult = null;
            try
            {
                byte[] FileContentBytes = null;
                using (Stream stream = await inputFile.OpenStreamForReadAsync())
                {
                    using (var memoryStream = new MemoryStream())
                    {

                        stream.CopyTo(memoryStream);
                        FileContentBytes = memoryStream.ToArray();
                    }
                }
                int ctr = 0;
                for (int i = 999; i >= 0; i--)
                {
                    if (FileContentBytes[i] != 0)
                    {
                        ctr = i;
                        break;
                    }
                }
                int ctr2 = 1000;
                for (int i = 1999; i >= 1000; i--)
                {
                    if (FileContentBytes[i] != 0)
                    {
                        ctr2 = i;
                        break;
                    }
                }
                int FileNameBytesLength = ctr + 1;
                int ExtensionBytesLength = ctr2 + 1 - 1000;
                byte[] passwordBytes = SHA256.Create().ComputeHash(System.Text.Encoding.ASCII.GetBytes(password));
                byte[] SaltedHashedPassword = SHA256.Create().ComputeHash(System.Text.Encoding.ASCII.GetBytes(password + "CryptoApp"));
                byte[] FileNameBytes = new byte[FileNameBytesLength];
                byte[] ExtensionBytes = new byte[ExtensionBytesLength];
                byte[] StoredHashedPassword = new byte[32];
                byte[] EncryptedBytes = new byte[FileContentBytes.Length - 2032];
                System.Buffer.BlockCopy(FileContentBytes, 2000, StoredHashedPassword, 0, 32);
                bool ArePasswordsSame = true;
                for (int i = 0; i < 32; i++)
                {
                    if (SaltedHashedPassword[i] != StoredHashedPassword[i])
                    {
                        ArePasswordsSame = false;
                        break;
                    }
                }
                if (ArePasswordsSame == false)
                {
                    decryptionResult = new DecryptionResult()
                    {
                        Result = false,
                        Error = "Password",
                        DecryptedString = null
                    };
                    return decryptionResult;
                }
                System.Buffer.BlockCopy(FileContentBytes, 0, FileNameBytes, 0, FileNameBytesLength);
                System.Buffer.BlockCopy(FileContentBytes, 1000, ExtensionBytes, 0, ExtensionBytesLength);
                System.Buffer.BlockCopy(FileContentBytes, 2032, EncryptedBytes, 0, EncryptedBytes.Length);

                int Blocks128000Size = EncryptedBytes.Length / 128000;
                int LastBlockSize = EncryptedBytes.Length % 128000;
                ctr = 0;
                byte[] DecryptedBytes = new byte[EncryptedBytes.Length];
                for (long i = 0; i < Blocks128000Size; i++)
                {
                    byte[] input = new byte[128000];
                    System.Buffer.BlockCopy(EncryptedBytes, ctr, input, 0, 128000);
                    byte[] result = DecryptionService.GetDecryptedByteArray(input, passwordBytes);
                    System.Buffer.BlockCopy(result, 0, DecryptedBytes, ctr, 128000);
                    ctr = ctr + 128000;
                }
                if (LastBlockSize > 0)
                {
                    byte[] input = new byte[LastBlockSize];
                    System.Buffer.BlockCopy(EncryptedBytes, ctr, input, 0, LastBlockSize);
                    byte[] result = DecryptionService.GetDecryptedByteArray(input, passwordBytes);
                    System.Buffer.BlockCopy(result, 0, DecryptedBytes, ctr, LastBlockSize);
                }
                string CurrentDirectoryPath = System.IO.Path.GetDirectoryName(inputFile.Path);
                string CurrentFileName = System.Text.Encoding.ASCII.GetString(FileNameBytes) + "_1";
                string CurrentFileExtension = System.Text.Encoding.ASCII.GetString(ExtensionBytes);
                string WritePath = System.IO.Path.Combine(CurrentDirectoryPath, CurrentFileName + CurrentFileExtension);
                decryptionResult = new DecryptionResult()
                {
                    Result = true,
                    Error = null,
                    DecryptedString = null,
                    DecryptedContents = DecryptedBytes,
                    WritePath = WritePath
                };
            }
            catch (Exception ex)
            {
                decryptionResult = new DecryptionResult()
                {
                    Result = false,
                    Error = ex.Message,
                    DecryptedString = null
                };
            }
            return decryptionResult;
        }
    }
}
