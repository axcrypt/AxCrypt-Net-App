using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AxCrypt.Api.Model.InfoSite
{
    [JsonObject(MemberSerialization.OptIn)]
    public class NIS2ApiModel
    {
        public NIS2ApiModel(long id, string fullname, string workemail, string companyname, string companysize, string jobtitle, string phone, DateTime createdUtc, DateTime updatedUtc, DateTime? deletedUtc)
        {
            Id = id;
            FullName = fullname;
            WorkEmail = workemail;
            CompanyName = companyname;
            CompanySize = companysize;
            JobTitle = jobtitle;
            Phone = phone;
            CreatedUtc = createdUtc;
            UpdatedUtc = updatedUtc;
            DeletedUtc = deletedUtc;
        }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("fullname")]
        public string FullName { get; set; }

        [JsonProperty("workemail")]
        public string WorkEmail { get; set; }

        [JsonProperty("companyname")]
        public string CompanyName { get; set; }

        [JsonProperty("companysize")]
        public string CompanySize { get; set; }

        [JsonProperty("jobtitle")]
        public string JobTitle { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

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