using AxCrypt.Core;
using AxCrypt.Core.UI;
using System.Text;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Fake
{
    public class FakeSecretSecureStorage
    {
        private readonly string AxC_Unique_Key;

        public FakeSecretSecureStorage()
        {
            AxC_Unique_Key = "AxC_Secret-f69b9ca8-47e-";
            //Task.Run(async () => await Initialize()).Wait();
        }

        public byte[] AppUserSecretKey
        {
            get
            {
                string? uniqueKeyVal = AxC_Unique_Key;
                if (!Resolve.KnownIdentities.IsLoggedOn)
                {
                    return Encoding.UTF8.GetBytes(uniqueKeyVal);
                }

                uniqueKeyVal += New<KnownIdentities>().DefaultEncryptionIdentity.Passphrase.Text.Substring(0, 8);
                return Encoding.UTF8.GetBytes(uniqueKeyVal);
            }
        }
    }
}