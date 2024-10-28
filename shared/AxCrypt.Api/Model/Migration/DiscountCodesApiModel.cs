using Newtonsoft.Json;
using System;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DiscountCodesApiModel : BaseApiModel
    {
        public DiscountCodesApiModel()
        {
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("discountcodetype")]
        public string DiscountCodeType { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("expirationUtc")]
        public DateTime ExpirationUtc { get; set; }

        [JsonProperty("internalcomment")]
        public string InternalComment { get; set; }

        //[JsonProperty("created_time_utc")]
        //public DateTime CreatedUtc { get;  }

        [JsonProperty("subscriptionlevel")]
        public string SubscriptionLevel { get; set; }

        [JsonProperty("autoenable")]
        public bool AutoEnable { get; set; }

        [JsonProperty("isreseller")]
        public bool IsReseller { get; set; }

        [JsonProperty("deleted")]
        public bool Deleted { get; set; }

        //[JsonProperty("deletedUtc")]
        //public DateTime? DeletedUtc { get; set; } = null;
    }
}