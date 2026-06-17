using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Api.Model.InfoSite
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class DemoApplicationModel
    {
        [JsonConstructor]
        public DemoApplicationModel()
        {
        }

        [JsonProperty("fullName")]
        public string FullName { get; set; }

        [JsonProperty("companyName")]
        public string CompanyName { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

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