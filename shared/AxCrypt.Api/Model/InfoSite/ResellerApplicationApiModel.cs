using Newtonsoft.Json;

namespace AxCrypt.Api.Model.InfoSite
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ResellerApplicationApiModel
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("firstName")]
        public string FirstName { get; set; }

        [JsonProperty("lastName")]
        public string LastName { get; set; }

        [JsonProperty("companyName")]
        public string CompanyName { get; set; }

        [JsonProperty("registrationNumber")]
        public string RegistrationNumber { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("companySize")]
        public string CompanySize { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("servingCountry")]
        public string ServingCountry { get; set; }

        [JsonProperty("expectedSales")]
        public string ExpectedSales { get; set; }

        [JsonProperty("otherInformation")]
        public string OtherInformation { get; set; }

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