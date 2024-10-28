using Newtonsoft.Json;
using System;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]
    public class XecretServiceApiModel : BaseApiModel
    {
        public XecretServiceApiModel()
        { }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("eventid")]
        public string EventId { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("level")]
        public string Level { get; set; }

        [JsonProperty("amountPaid")]
        public decimal AmountPaid { get; set; }

        [JsonProperty("amountVat")]
        public decimal AmountVat { get; set; }

        [JsonProperty("currencyPaid")]
        public string CurrencyPaid { get; set; }

        [JsonProperty("starttimeutc")]
        public DateTime StartTimeUTC { get; set; }

        [JsonProperty("accountingCurrencyRate")]
        public decimal AccountingCurrencyRate { get; set; }

        [JsonProperty("accountingCurrency")]
        public string AccountingCurrency { get; set; }

        [JsonProperty("expirationtimeutc")]
        public DateTime ExpirationTimeUTC { get; set; }

        [JsonProperty("eventType")]
        public string EventType { get; set; }

        [JsonProperty("systemcomment")]
        public string SystemComment { get; set; }

        [JsonProperty("usercomment")]
        public string UserComment { get; set; }

        [JsonProperty("payeeid")]
        public string PayeeId { get; set; }

        [JsonProperty("beneficiaryid")]
        public string BeneficiaryId { get; set; }

        [JsonProperty("pymtref")]
        public string PaymentReference { get; set; }

        [JsonProperty("purchaseOriginalTransactionId")]
        public string PurchaseOriginalTransactionId { get; set; }

        [JsonProperty("paymenttype")]
        public string PaymentType { get; set; }

        [JsonProperty("isdeleted")]
        public bool IsDeleted { get; set; }
    }
}