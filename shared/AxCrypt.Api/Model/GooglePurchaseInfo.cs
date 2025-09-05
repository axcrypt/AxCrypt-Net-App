using Newtonsoft.Json;

namespace AxCrypt.Api.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GooglePurchaseInfo
    {
        public GooglePurchaseInfo()
        {
        }

        public GooglePurchaseInfo(string productId, string paidBy, string paidFor, string transactionId, string currencyPaid, decimal amountPaid, GooglePaymentState paymentStatus, string startTimeUtc, string expiryTimeUtc, string purchaseToken, string countryCode)
        {
            ProductId = productId;
            PaidBy = paidBy;
            PaidFor = paidFor;
            TransactionId = transactionId;
            CurrencyPaid = currencyPaid;
            AmountPaid = amountPaid;
            PaymentStatus = paymentStatus;
            StartTimeUtc = startTimeUtc;
            ExpirationTimeUtc = expiryTimeUtc;
            PurchaseToken = purchaseToken;
            CountryCode = countryCode;
        }

        [JsonProperty("productId")]
        public string ProductId { get; set; }

        [JsonProperty("paidBy")]
        public string PaidBy { get; set; }

        [JsonProperty("paidFor")]
        public string PaidFor { get; set; }

        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }

        [JsonProperty("currencyPaid")]
        public string CurrencyPaid { get; set; }

        [JsonProperty("amountPaid")]
        public decimal AmountPaid { get; set; }

        [JsonProperty("starttimeUtc")]
        public string StartTimeUtc { get; set; }

        [JsonProperty("expirationtimeUtc")]
        public string ExpirationTimeUtc { get; set; }

        [JsonProperty("paymentStatus")]
        public GooglePaymentState PaymentStatus { get; set; }

        [JsonProperty("itemName")]
        public string ItemName { get; set; }

        [JsonProperty("countryCode")]
        public string CountryCode { get; set; }

        [JsonProperty("amountFee")]
        public decimal AmountFee { get; set; }

        [JsonProperty("amountVat")]
        public decimal AmountVat { get; set; }

        [JsonProperty("discountCode")]
        public string AppliedDiscountCode { get; set; } = string.Empty;

        [JsonProperty("istrialPeriod")]
        public bool IsTrialPeriod { get; set; }

        [JsonProperty("purchaseToken")]
        public string PurchaseToken { get; set; }
    }

    public enum GooglePaymentState
    {
        SUBSCRIPTION_STATE_PENDING,
        SUBSCRIPTION_STATE_IN_GRACE_PERIOD,
        SUBSCRIPTION_STATE_ON_HOLD,
        SUBSCRIPTION_STATE_ACTIVE,
    }
}