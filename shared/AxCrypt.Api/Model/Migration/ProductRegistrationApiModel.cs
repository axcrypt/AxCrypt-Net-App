using Newtonsoft.Json;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ProductRegistrationApiModel : BaseApiModel
    {
        [JsonProperty("email")]
        public string Email { get; set; } = string.Empty;

        [JsonProperty("ipaddress")]
        public string IPAddress { get; set; } = string.Empty;

        [JsonProperty("datetimeutc")]
        public DateTime DateTimeUtc { get; set; }

        [JsonProperty("useragent")]
        public string UserAgent { get; set; } = string.Empty;

        [JsonProperty("method")]
        public string Method { get; set; } = string.Empty;

        [JsonProperty("previousversion")]
        public string PreviousVersion { get; set; } = string.Empty;

        [JsonProperty("productversion")]
        public string ProductVersion { get; set; } = string.Empty;

        [JsonProperty("productname")]
        public string ProductName { get; set; }

        [JsonProperty("language")]
        public string? Language { get; set; }

        [JsonProperty("preference")]
        public string Preference { get; set; } = string.Empty;

        [JsonProperty("platform")]
        public string Platform { get; set; }
    }
}