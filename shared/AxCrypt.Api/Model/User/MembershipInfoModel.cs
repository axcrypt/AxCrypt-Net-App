using Newtonsoft.Json;

namespace AxCrypt.Api.Model.User
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MembershipInfoApiModel : BaseApiModel
    {
        public MembershipInfoApiModel()
        { }

        [JsonProperty("userEmail")]
        public string UserEmail { get; set; }

        [JsonProperty("userId")]
        public long UserId { get; set; }

        [JsonProperty("apikey")]
        public Guid Apikey { get; set; }

        [JsonProperty("isemailinvalid")]
        public bool Isemailinvalid { get; set; }

        [JsonProperty("invitedby")]
        public string Invitedby { get; set; }

        [JsonProperty("ispasswordresetrequested")]
        public bool Ispasswordresetrequested { get; set; }

        [JsonProperty("passwordreset")]
        public bool PasswordReset { get; set; }

        [JsonProperty("pendingemailchangefrom")]
        public string Pendingemailchangefrom { get; set; }

        [JsonProperty("lastemailchangedate")]
        public DateTime Lastemailchangedate { get; set; }

        [JsonProperty("activesubscription")]
        public bool Activesubscription { get; set; }

        [JsonProperty("usedtrialfrom")]
        public string Usedtrialfrom { get; set; }

        [JsonProperty("businesstrialused")]
        public string Businesstrialused { get; set; }

        [JsonProperty("stripecustomerid")]
        public string Stripecustomerid { get; set; }

        [JsonProperty("isnewunsubscribed")]
        public bool Isnewunsubscribed { get; set; }

        [JsonProperty("preferredcultureName")]
        public string? PreferredCultureName { get; set; }

        [JsonProperty("unsubscribed")]
        public bool Unsubscribed { get; set; }
    }
}