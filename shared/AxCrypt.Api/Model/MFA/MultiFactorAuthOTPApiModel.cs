using Newtonsoft.Json;

namespace AxCrypt.Api.Model.MFA
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MultiFactorAuthOTPApiModel : BaseApiModel
    {
        public MultiFactorAuthOTPApiModel()
        {
        }

        [JsonProperty("userEmail")]
        public string UserEmail { get; set; }

        [JsonProperty("Type")]
        public string Type { get; set; }

        [JsonProperty("otp")]
        public string Otp { get; set; }

        [JsonProperty("expiration")]
        public DateTime Expiration { get; set; }
    }
}
