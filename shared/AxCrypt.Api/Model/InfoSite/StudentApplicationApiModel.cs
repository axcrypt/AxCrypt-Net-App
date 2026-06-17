using Newtonsoft.Json;

namespace AxCrypt.Api.Model.InfoSite
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class StudentApplicationApiModel
    {
        [JsonConstructor]
        public StudentApplicationApiModel()
        {
        }

        [JsonProperty("fullName")]
        public string FullName { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("universityName")]
        public string UniversityName { get; set; }

        [JsonProperty("course")]
        public string Course { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("createdUtc")]
        public DateTime CreatedUtc { get; set; }

        [JsonProperty("updatedUtc")]
        public DateTime UpdatedUtc { get; set; }

        [JsonProperty("deletedUtc")]
        public DateTime? DeletedUtc { get; set; }

        [JsonProperty("greCaptchaResponse")]
        public string GreCaptchaResponse { get; set; }
    }
}