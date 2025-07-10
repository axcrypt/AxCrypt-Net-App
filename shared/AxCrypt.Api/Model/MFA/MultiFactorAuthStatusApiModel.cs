using Newtonsoft.Json;

namespace AxCrypt.Api.Model.MFA
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MultiFactorAuthStatusApiModel : BaseApiModel
    {
        public MultiFactorAuthStatusApiModel()
        {
        }

        [JsonProperty("userEmail")]
        public string UserEmail { get; set; }

        [JsonProperty("multiFactorType")]
        public string MultiFactorType { get; set; }
    }
}
