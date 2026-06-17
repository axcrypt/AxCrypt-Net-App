using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AxCrypt.Api
{
    public class RequestOptions
    {
        [JsonProperty("userName")]
        public string UserName { get; set; } = string.Empty;

        [JsonProperty("id")]
        public long Id { get; set; } = 0;

        [JsonProperty("queryString")]
        public string QueryString { get; set; } = string.Empty;

        [JsonProperty("pageCount")]
        public int PageCount { get; set; } = 0;

        [JsonProperty("pageNumber")]
        public int PageNumber { get; set; } = 0;

        [JsonProperty("allitem")]
        public bool AllItem { get; set; }

        [JsonProperty("startDate")]
        public DateTime? StartDate { get; set; }

        [JsonProperty("endDate")]
        public DateTime? EndDate { get; set; }

        [JsonProperty("isOnlyPaidOrTrialUsers")]
        public bool IsOnlyPaidOrTrialUsers { get; set; }

        [JsonProperty("subscriptionlevel")]
        public string SubscriptionLevel { get; set; } = string.Empty;

        [JsonProperty("ispushnotification")]
        public bool IsPushNotification { get; set; }

        [JsonProperty("preferredculturename")]
        public string PreferredCultureName { get; set; } = string.Empty;

        [JsonProperty("accountsOlderThanInDaysFilter")]
        public int AccountsOlderThanInDaysFilter { get; set; }

        [JsonProperty("maxuserlimit")]
        public int MaxUserLimit { get; set; } = 200000;
    }
}