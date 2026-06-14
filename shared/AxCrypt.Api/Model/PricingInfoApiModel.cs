using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AxCrypt.Api.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PricingInfoApiModel
    {
        [JsonProperty("subscriptionlevel")]
        public SubscriptionLevel SubscriptionLevel { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; } = string.Empty;

        [JsonProperty("amountvat")]
        public string AmountVat { get; set; } = string.Empty;

        [JsonProperty("subscriptiomMonths")]
        public int SubscriptionMonths { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonProperty("eligibleforfreetrial")]
        public bool EligibleForFreeTrial { get; set; }

        [JsonProperty("paypalurl")]
        public string PayPalUrl { get; set; } = string.Empty;

        [JsonProperty("businessInfo")]
        public IEnumerable<KeyValuePair<string, string>> BusinessInfo { get; set; }
    }
}
