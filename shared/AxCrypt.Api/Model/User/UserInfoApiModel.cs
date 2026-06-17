using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Api.Model.User
{
    [JsonObject(MemberSerialization.OptIn)]
    public class UserInfoApiModel : BaseApiModel
    {
        public UserInfoApiModel()
        {
        }

        public UserInfoApiModel(Guid apikey, bool activesubscription, string stripecustomerid, string usedtrailfrom, bool businesstrailused, string preferedculturename, bool isemailinvalid, string invitedby, bool unsubscribed, string pendingemailchangefrom, DateTime lastemailchangedate, bool ispasswordresetrequested, bool passwordreset, bool isnewunsubscribed)
        {
            ApiKey = apikey;
            ActiveSubscription = activesubscription;
            StripeCustomerId = stripecustomerid;
            UsedTrialFrom = stripecustomerid;
            BusinessTrialUsed = businesstrailused;
            PreferredCultureName = preferedculturename;
            IsEmailInvalid = isemailinvalid;
            InvitedBy = invitedby;
            IsNewsUnsubscribed = isnewunsubscribed;
        }

        [JsonProperty("userEmail")]
        public string UserEmail { get; set; }

        [JsonProperty("providerUserKey")]
        public string ProviderUserKey { get; set; }

        [JsonProperty("apikey")]
        public Guid ApiKey { get; set; }

        [JsonProperty("activesubscription")]
        public bool ActiveSubscription { get; set; }

        [JsonProperty("stripecustomerid")]
        public string StripeCustomerId { get; set; }

        [JsonProperty("usedtrialfrom")]
        public string UsedTrialFrom { get; set; }

        [JsonProperty("businesstrialused")]
        public bool BusinessTrialUsed { get; set; }

        [JsonProperty("preferredculturename")]
        public string PreferredCultureName { get; set; }

        [JsonProperty("isemailinvalid")]
        public bool IsEmailInvalid { get; set; }

        [JsonProperty("invitedby")]
        public string InvitedBy { get; set; }

        [JsonProperty("isnewsunsubscribed")]
        public bool IsNewsUnsubscribed { get; set; }
    }
}
