using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.UI.Xaml;

namespace CryptoAppUWP.Services
{
    public class EncryptionResult
    {
        public bool Result { get; set; }

        public string Error { get; set; }

        public string EncryptedString { get; set; }

        public byte[] EncryptedContents { get; set; }

        public string WritePath { get; set; }
    }

    public class EncryptionService
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

        public static byte[] GetEncryptedByteArray(byte[] encryptedBytes, byte[] password)
        {
            //the salt bytes must be at least 8 bytes
            byte[] saltBytes = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 };
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] encyptedbytes = null;
                using (RijndaelManaged aes = new RijndaelManaged())
                {
                    aes.KeySize = 256;
                    aes.BlockSize = 128;
                    var key = new Rfc2898DeriveBytes(password, saltBytes, 1000);
                    aes.Key = key.GetBytes(aes.KeySize / 8);
                    aes.IV = key.GetBytes(aes.BlockSize / 8);
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.Zeros;
                    string plainText = String.Empty;
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(encryptedBytes, 0, encryptedBytes.Length);
                        cs.Close();
                    }
                    encyptedbytes = ms.ToArray();
                }
                return encyptedbytes;
            }
        }

        public static EncryptionResult EncryptText(string inputText, string password)
        {
            EncryptionResult encryptionResult = new EncryptionResult() { Result = false };
            try
            {
                byte[] encryptedBytes = System.Text.Encoding.UTF8.GetBytes(inputText);
                byte[] passwordToByteArray = System.Text.Encoding.UTF8.GetBytes(password);
                passwordToByteArray = SHA256.Create().ComputeHash(passwordToByteArray);
                byte[] encryptedByteArray = EncryptionService.GetEncryptedByteArray(encryptedBytes, passwordToByteArray);
                try
                {
                    encryptionResult.EncryptedString = System.Text.Encoding.UTF8.GetString(encryptedByteArray);
                    encryptionResult.Error = null;
                    encryptionResult.Result = true;
                }
                catch (Exception)
                {
                    encryptionResult.Result = false;
                    encryptionResult.Error = "This string can not be enrypted";
                }
            }
            catch (Exception ex)
            {
                encryptionResult.Result = false;
                encryptionResult.Error = ex.Message;
            }
            return encryptionResult;
        }

        public async static Task<EncryptionResult> EncryptFile(Windows.Storage.StorageFile inputFile, string password)
        {
            EncryptionResult encryptionResult = null;
            try
            {
                byte[] encryptedBytes = null;
                using (Stream stream = await inputFile.OpenStreamForReadAsync())
                {
                    using (var memoryStream = new MemoryStream())
                    {

                        stream.CopyTo(memoryStream);
                        encryptedBytes = memoryStream.ToArray();
                    }
                }
                byte[] passwordToByteArray = System.Text.Encoding.ASCII.GetBytes(password);
                passwordToByteArray = SHA256.Create().ComputeHash(passwordToByteArray);
                byte[] SaltedHashedPassword = SHA256.Create().ComputeHash(System.Text.Encoding.ASCII.GetBytes(password + "CryptoApp"));
                int Blocks128000Size = encryptedBytes.Length / 128000;
                int LastBlockSize = encryptedBytes.Length % 128000;
                int ctr = 0;
                byte[] encryptedByteArray = new byte[encryptedBytes.Length];
                for (long i = 0; i < Blocks128000Size; i++)
                {
                    byte[] input = new byte[128000];
                    System.Buffer.BlockCopy(encryptedBytes, ctr, input, 0, 128000);
                    byte[] result = EncryptionService.GetEncryptedByteArray(input, passwordToByteArray);
                    System.Buffer.BlockCopy(result, 0, encryptedByteArray, ctr, 128000);
                    ctr = ctr + 128000;
                }
                if (LastBlockSize > 0)
                {
                    byte[] input = new byte[LastBlockSize];
                    System.Buffer.BlockCopy(encryptedBytes, ctr, input, 0, LastBlockSize);
                    byte[] result = EncryptionService.GetEncryptedByteArray(input, passwordToByteArray);
                    System.Buffer.BlockCopy(result, 0, encryptedByteArray, ctr, LastBlockSize);
                }
                string CurrentDirectoryPath = System.IO.Path.GetDirectoryName(inputFile.Path);
                string CurrentFileName = Path.GetFileNameWithoutExtension(inputFile.Path);
                string CurrentFileExtension = Path.GetExtension(inputFile.Path);
                byte[] FileNameBytes = System.Text.Encoding.ASCII.GetBytes(CurrentFileName);
                byte[] ExtensionBytes = System.Text.Encoding.ASCII.GetBytes(CurrentFileExtension);
                int FileNameBytesLength = FileNameBytes.Length;
                int ExtensionBytesLength = ExtensionBytes.Length;
                FileNameBytesLength = (FileNameBytesLength >= 1000) ? 999 : FileNameBytesLength;
                ExtensionBytesLength = (ExtensionBytesLength >= 1000) ? 999 : ExtensionBytesLength;
                byte[] FileContentBytes = new byte[1000 + 1000 + 32 + encryptedByteArray.Length];
                System.Buffer.BlockCopy(FileNameBytes, 0, FileContentBytes, 0, FileNameBytesLength);
                System.Buffer.BlockCopy(ExtensionBytes, 0, FileContentBytes, 1000, ExtensionBytesLength);
                System.Buffer.BlockCopy(SaltedHashedPassword, 0, FileContentBytes, 2000, 32);
                System.Buffer.BlockCopy(encryptedByteArray, 0, FileContentBytes, 2032, encryptedByteArray.Length);
                string WritePath = System.IO.Path.Combine(CurrentDirectoryPath, CurrentFileName + ".senc");
                encryptionResult = new EncryptionResult()
                {
                    Result = true,
                    Error = null,
                    EncryptedString = null,
                    EncryptedContents=FileContentBytes,
                    WritePath=WritePath
                };
            }
            catch (Exception ex)
            {
                encryptionResult = new EncryptionResult()
                {
                    Result = false,
                    Error = ex.Message,
                    EncryptedString = null,
                };
            }
            return encryptionResult;
        }
    }
}
