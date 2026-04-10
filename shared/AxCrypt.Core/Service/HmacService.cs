using AxCrypt.Api.Model.Entitlement;
using AxCrypt.Core.UI;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.Service
{
    public static class HmacService
    {
        private static readonly string secretKey = "Axc_Hask_Key";

        public static string ComputeHmac(EntitlementApiModel model)
        {
            string raw = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
            byte[] dataBytes = Encoding.UTF8.GetBytes(raw);

            using HMACSHA256 hmac = new HMACSHA256(keyBytes);
            byte[] hashBytes = hmac.ComputeHash(dataBytes);

            return Convert.ToHexString(hashBytes).ToLower();
        }

        public static bool VerifyHmac(EntitlementApiModel model)
        {
            string expectedHash = ComputeHmac(model);
            string actualHash = New<UserSettings>().EntitlementHashKey;

            if (string.IsNullOrEmpty(actualHash))
                return true;

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedHash),
                Encoding.UTF8.GetBytes(actualHash)
            );
        }
    }
}