using System;
using Newtonsoft.Json;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PayPalPurchaseSettingsApiModel : BaseApiModel
    {
        [JsonConstructor]
        public PayPalPurchaseSettingsApiModel()
        {
            
        }

       
        [JsonProperty("userEmail")]
        public string? UserEmail { get; set; }

        [JsonProperty("subscriptionId")]
        public string? SubscriptionId { get; set; }

        [JsonProperty("subscriptionStatus")]
        public object? SubscriptionStatus { get; set; }

        [JsonProperty("vatRate")]
        public decimal VatRate { get; set; }

        [JsonProperty("subscriptionMonths")]
        public int SubscriptionMonths { get; set; }

        [JsonProperty("culture")]
        public string? Culture { get; set; }
    }
    
}
