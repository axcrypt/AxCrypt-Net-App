using Newtonsoft.Json;

namespace AxCrypt.Api.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GooglePurchaseInfo
    {
        public GooglePurchaseInfo()
        {
        }

        public GooglePurchaseInfo(string productId, string paidBy, string paidFor, string transactionId, string currencyPaid, decimal amountPaid, string paymentStatus, string startTimeUtc, string expiryTimeUtc, string purchaseToken)
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
        }

        [JsonProperty("product_id")]
        public string ProductId { get; set; }

        [JsonProperty("paid_by")]
        public string PaidBy { get; set; }

        [JsonProperty("paid_for")]
        public string PaidFor { get; set; }

        [JsonProperty("txn_id")]
        public string TransactionId { get; set; }

        [JsonProperty("currency_paid")]
        public string CurrencyPaid { get; set; }

        [JsonProperty("amount_paid")]
        public decimal AmountPaid { get; set; }

        [JsonProperty("starttimeutc")]
        public string StartTimeUtc { get; set; }

        [JsonProperty("expirationtimeutc")]
        public string ExpirationTimeUtc { get; set; }

        [JsonProperty("payment_status")]
        public string PaymentStatus { get; set; }

        [JsonProperty("item_name")]
        public string ItemName { get; set; }

        [JsonProperty("country_code")]
        public string CountryCode { get; set; }

        [JsonProperty("amount_fee")]
        public decimal AmountFee { get; set; }

        [JsonProperty("amount_vat")]
        public decimal AmountVat { get; set; }

        [JsonProperty("discount_code")]
        public string AppliedDiscountCode { get; set; } = string.Empty;

        [JsonProperty("istrialperiod")]
        public bool IsTrialPeriod { get; set; }

        [JsonProperty("purchasetoken")]
        public string PurchaseToken { get; set; }
    }
}