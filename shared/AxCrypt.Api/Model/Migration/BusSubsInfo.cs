using System;
using Newtonsoft.Json;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]

    public class BusSubsInfo : BaseApiModel
    {
        public BusSubsInfo(long id,string subscriptioninfo,string euvatnumber,string purchasestyle,string businesssubscriptionid,DateTime createdUtc, DateTime updatedUtc,DateTime? deletedUtc) 
        {
            Id= id;
            SubscriptionInfo= subscriptioninfo;
            EuVatNumber= euvatnumber;
            PurchaseStyle= purchasestyle;
            BusinessSubscriptionId= businesssubscriptionid;
            CreatedUtc= createdUtc;
            UpdatedUtc= updatedUtc;
            DeletedUtc= deletedUtc;
        }

        [JsonProperty("subscr_info")]
        public string SubscriptionInfo { get; set; }

        [JsonProperty("eu_vat_num")]
        public string EuVatNumber { get; set; }

        [JsonProperty("purchase_style")]
        public string PurchaseStyle { get; set; }

        [JsonProperty("bus_subs_id")]
        public string BusinessSubscriptionId { get; set; }
    }
}
