using AxCrypt.Core;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using Microsoft.Maui.Storage;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Desktop.Data
{
    public class SecretSecureStorage
    {
        private readonly string AxC_Unique_Key;

        public SecretSecureStorage()
        {
            AxC_Unique_Key = "AxC_Secret-f69b9ca8-47e-";
            //Task.Run(async () => await Initialize()).Wait();
        }

        //private async Task Initialize()
        //{
        //    if (New<IdentityViewModel>().LogOnIdentity == Core.Crypto.LogOnIdentity.Empty)
        //    {
        //        return;
        //    }

        //    string? uniqueKeyVal = await SecureStorage.GetAsync("AppUserSecretKey");
        //    if (uniqueKeyVal == null)
        //    {
        //        uniqueKeyVal = GenerateUniqueKey(32);
        //        await SecureStorage.SetAsync("AppUserSecretKey", uniqueKeyVal);
        //    }

        //    _appUserSecretKey = Encoding.UTF8.GetBytes(uniqueKeyVal);
        //}

        //private byte[] _appUserSecretKey;

        public byte[] AppUserSecretKey
        {
            get
            {
                if (!Resolve.KnownIdentities.IsLoggedOn)
                {
                    return new byte[] { };
                }

                string? uniqueKeyVal = AxC_Unique_Key + New<KnownIdentities>().DefaultEncryptionIdentity.Passphrase.Text.Substring(0, 8);
                return Encoding.UTF8.GetBytes(uniqueKeyVal);
            }
        }

        //private static string GenerateUniqueKey(int length)
        //{
        //    using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        //    {
        //        byte[] key = new byte[length];
        //        rng.GetBytes(key);
        //        return Convert.ToBase64String(key).Substring(0, length);
        //    }
        //}
    }
}