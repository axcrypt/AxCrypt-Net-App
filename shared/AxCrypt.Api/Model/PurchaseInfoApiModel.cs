using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AxCrypt.Api.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PurchaseInfoApiModel
    {
        [JsonProperty("useremail")]
        public string UserEmail { get; set; } = string.Empty;

        [JsonProperty("filleddiscountcode")]
        public string FilledDiscountCode { get; set; } = string.Empty;

        [JsonProperty("eligibleforfreetrial")]
        public bool EligibleForFreeTrial { get; set; }

        [JsonProperty("subscriptionlevel")]
        public SubscriptionLevel SubscriptionLevel { get; set; }

        [JsonProperty("euvatnumber")]
        public string EuVatNumber { get; set; } = string.Empty;

        [JsonProperty("organizationname")]
        public string OrganizationName { get; set; } = string.Empty;

        [JsonProperty("address")]
        public string Address { get; set; } = string.Empty;

        [JsonProperty("country")]
        public string Country { get; set; } = string.Empty;

        [JsonProperty("members")]
        public int Members { get; set; }

        [JsonProperty("businesssubscriptionmembershipid")]
        public string BusinessSubscriptionMembershipId { get; set; } = string.Empty;

        [JsonProperty("subscriptionmonths")]
        public int SubscriptionMonths { get; set; }

        [JsonProperty("phonenumber")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonProperty("invoiceemail")]
        public string InvoiceEmail { get; set; } = string.Empty;

        [JsonProperty("businesssubscriptionid")]
        public string BusinessSubscriptionId { get; set; } = string.Empty;
    }
}
