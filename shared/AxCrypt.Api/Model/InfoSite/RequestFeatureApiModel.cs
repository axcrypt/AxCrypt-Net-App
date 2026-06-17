using Newtonsoft.Json;

namespace AxCrypt.Api.Model.InfoSite
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RequestFeatureApiModel
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; } = "";

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("createdUtc")]
        public DateTime CreatedUtc { get; set; }

        [JsonProperty("updatedUtc")]
        public DateTime UpdatedUtc { get; set; }

        [JsonProperty("deletedUtc")]
        public DateTime? DeletedUtc { get; set; }

        [JsonProperty("grecaptcharesponse")]
        public string GreCaptchaResponse { get; set; }
    }
}