using Newtonsoft.Json;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]
    public class NewsLetterApiModel
    {
        public NewsLetterApiModel()
        {
        }

        public NewsLetterApiModel(int id, string uniqueName, string subject, string tag,
            string twoLetterisolanguagename, DateTime firstUtc, DateTime lastUtc, string userFilter,
            string newsletterLanguage, string subscriptionLevels, int emailLimit, string emailFilter,
            int accountsolderthanindaysfilter, bool isReseller, DateTime createddate, DateTime sentdate, bool excludeTrialOrPaidUsers)
        {
            Id = id;
            UniqueName = uniqueName;
            Subject = subject;
            Tag = tag;
            TwoLetterISOLanguageName = twoLetterisolanguagename;
            FirstUtc = firstUtc;
            LastUtc = lastUtc;
            UserFilter = userFilter;
            NewsletterLanguage = newsletterLanguage;
            SubscriptionLevels = subscriptionLevels;
            EmailLimit = emailLimit;
            EmailFilter = emailFilter;
            AccountsOlderThanInDaysFilter = accountsolderthanindaysfilter;
            IsReseller = isReseller;
            CreatedDate = createddate;
            SentDate = sentdate;
            ExcludeTrialOrPaidUsers = excludeTrialOrPaidUsers;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("uniquename")]
        public string UniqueName { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("TwoletterisolanguageName")]
        public string TwoLetterISOLanguageName { get; set; }

        [JsonProperty("firstutc")]
        public DateTime FirstUtc { get; set; }

        [JsonProperty("lastutc")]
        public DateTime LastUtc { get; set; }

        [JsonProperty("userfilter")]
        public string UserFilter { get; set; }

        [JsonProperty("newsletterlanguage")]
        public string NewsletterLanguage { get; set; }

        [JsonProperty("subscriptionlevels")]
        public string SubscriptionLevels { get; set; }

        [JsonProperty("emaillimit")]
        public int EmailLimit { get; set; }

        [JsonProperty("emailfilter")]
        public string EmailFilter { get; set; }

        [JsonProperty("accountsolderthanindaysfilter")]
        public int AccountsOlderThanInDaysFilter { get; set; }

        [JsonProperty("isreseller")]
        public bool IsReseller { get; set; }

        [JsonProperty("createddate")]
        public DateTime CreatedDate { get; set; }

        [JsonProperty("sentdate")]
        public DateTime SentDate { get; set; }

        [JsonProperty("excludetrialorpaidUsers")]
        public bool ExcludeTrialOrPaidUsers { get; set; }
    }
}