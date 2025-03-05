using Microsoft.Maui.Storage;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.App.Desktop.Data
{
    public class SecretSecureStorage
    {
        public SecretSecureStorage()
        {
            Task.Run(async () => await Initialize()).Wait();
        }

        private async Task Initialize()
        {
            string? uniqueKeyVal = await SecureStorage.GetAsync("AppUserSecretKey");
            if (uniqueKeyVal == null)
            {
                uniqueKeyVal = GenerateUniqueKey(32);
                await SecureStorage.SetAsync("AppUserSecretKey", uniqueKeyVal);
            }

            _appUserSecretKey = Encoding.UTF8.GetBytes(uniqueKeyVal);
        }

        private byte[] _appUserSecretKey;

        public byte[] AppUserSecretKey
        {
            get
            {
                return _appUserSecretKey;
            }
        }

        private static string GenerateUniqueKey(int length)
        {
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] key = new byte[length];
                rng.GetBytes(key);
                return Convert.ToBase64String(key).Substring(0, length);
            }
        }
    }
}