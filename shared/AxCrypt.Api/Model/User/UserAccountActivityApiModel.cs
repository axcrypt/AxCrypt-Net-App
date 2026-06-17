using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Api.Model.User
{
    [JsonObject(MemberSerialization.OptIn)]
    public class UserAccountActivityApiModel : UserInfoApiModel
    {
        [JsonProperty("lastemailchangedate")]
        public DateTime LastEmailChangeDate { get; set; } = DateTime.MinValue;

        [JsonProperty("ispasswordresetrequested")]
        public bool IsPasswordResetRequested { get; set; } = false;

        [JsonProperty("passwordreset")]
        public bool PassowordReset { get; set; } = false;

        [JsonProperty("lastPwdResetTime")]
        public DateTime LastPwdResetTime { get; set; } = DateTime.MinValue;

        [JsonProperty("lastPwdChangedTime")]
        public DateTime LastPwdChangedTime { get; set; } = DateTime.MinValue;

        [JsonProperty("lastLogonTime")]
        public DateTime LastLogOnTime { get; set; } = DateTime.MinValue;

        [JsonProperty("lastLockedOutTime")]
        public DateTime LastLockedOutTime { get; set; } = DateTime.MinValue;

        [JsonProperty("failedPwdAtmptCount")]
        public int FailedPwdAtmptCount { get; set; }

        [JsonProperty("failedPwdAtmptWindStart")]
        public DateTime FailedPwdAtmptWindStart { get; set; }

        [JsonProperty("isloggedout")]
        public bool IsLoggedOut { get; set; } = false;
    }
}
