using Newtonsoft.Json;

namespace AxCrypt.Api.Model
{
    public class EntitlementRequestOptions
    {
        public EntitlementRequestOptions(string userEmail, string feature)
        {
            UserEmail = userEmail;
            Feature = feature;
        }

        [JsonProperty("userEmail")]
        public string UserEmail { get; set; } = string.Empty;

        [JsonProperty("feature")]
        public string Feature { get; set; } = string.Empty;

        [JsonProperty("subscriptionlevel")]
        public string SubscriptionLevel { get; set; }
    }
}