using System;
using Newtonsoft.Json;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]

    public class BusinessSubscriptionApiModel : BaseApiModel
    {
        public BusinessSubscriptionApiModel(long id, bool active, bool isreseller,DateTime expirationutc,string businesssubscriptionid,bool masterkeyenabled, int membercapacity,int subscriptionmonths, string payprovidedsubscriptionid,DateTime createdUtc,DateTime updatedUtc, DateTime? deletedUtc) 
        { 
             Id= id;
            Active= active;
            IsReseller= isreseller;
            ExpirationUtc= expirationutc;
            BusinessSubscriptionId= businesssubscriptionid;
            MasterKeyEnabled= masterkeyenabled;
            MemberCapacity= membercapacity;
            SubscriptionMonths= subscriptionmonths;
            PayProvidedSubscriptionId= payprovidedsubscriptionid;
            CreatedUtc= createdUtc;
            UpdatedUtc= updatedUtc;
            DeletedUtc= deletedUtc;
        }


        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("is_reseller")]
        public bool IsReseller { get; set; }

        [JsonProperty("exp_utc")]
        public DateTime ExpirationUtc { get; set; }

        [JsonProperty("bus_subs_id")]
        public string BusinessSubscriptionId { get; set; } 

        [JsonProperty("master_key_enabled")]
        public bool MasterKeyEnabled { get; set; }

        [JsonProperty("member_cpcty")]
        public int MemberCapacity { get; set; }

        [JsonProperty("subs_months")]
        public int SubscriptionMonths { get; set; }

        [JsonProperty("pay_prov_subId")]
        public string PayProvidedSubscriptionId { get; set; }

        [JsonProperty("businesssubscription")]
        public BusSubsInfo BusSubsInformation { get; set; }
    }
}
