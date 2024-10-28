using System;
using Newtonsoft.Json;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PaymentTransLogApiModel 
    {
        public PaymentTransLogApiModel(long id,string subscriptionid,DateTime datetimeutc,string pricevatculture,
            string paidforemail,string paidbyemail,string paidforname,int days,int members,string item,string processor,
            string transactionid,string status,bool deleted,DateTime? deletedatetimeutc,string paymentproviderinvoiceid,string paymenterrormessage,
            string discountcode,decimal discountedamount,string purchaseorganizationtransactionid,string businesspaymenttype,DateTime created,DateTime updated)
        {
            Id = id;
            SubscriptionId = subscriptionid;
            DateTimeUTC = datetimeutc;
            PriceVatCulture= pricevatculture;
            PaidForEmail= paidforemail;
            PaidByEmail= paidbyemail;
            PaidForName= paidforname;
            Days= days;
            Members= members;
            Item= item;
            Processor= processor;
            TransactionId= transactionid;
            Status= status;
            Deleted = deleted;
            DeleteDateTimeUTC = deletedatetimeutc;
            PaymentProviderInvoiceId = paymentproviderinvoiceid;
            PaymentErrorMessage= paymenterrormessage;
            DiscountCode= discountcode;
            DiscountedAmount= discountedamount;
            PurchaseOrganizationTransactionId= purchaseorganizationtransactionid;
            BusinessPaymentType = businesspaymenttype;
            Created = created;
            Updated = updated;
        }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("subscription_id")]
        public string SubscriptionId { get; set; }

        [JsonProperty("datetime_utc")]
        public DateTime DateTimeUTC { get; set; }

        [JsonProperty("price_vat_culture")]
        public string PriceVatCulture { get; set; }

        [JsonProperty("paid_for_email")]
        public string PaidForEmail { get; set; }

        [JsonProperty("paid_by_email")]
        public string PaidByEmail { get; set; }

        [JsonProperty("paid_for_name")]
        public string PaidForName { get; set; }

        [JsonProperty("days")]
        public int Days { get; set; }

        [JsonProperty("members")]
        public int Members { get; set; }

        [JsonProperty("item")]
        public string Item { get; set; }

        [JsonProperty("processor")]
        public string Processor { get; set; }

        [JsonProperty("trans_id")]
        public string TransactionId { get; set; } 

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("deleted")]
        public bool Deleted { get; set; }

        [JsonProperty("delete_datetime_utc")]
        public DateTime? DeleteDateTimeUTC { get; set; }

        [JsonProperty("payment_provider_invoiceId")]
        public string PaymentProviderInvoiceId { get; set; }

        [JsonProperty("payment_Err_msg")]
        public string PaymentErrorMessage { get; set; }

        [JsonProperty("discount_code")]
        public string DiscountCode { get; set; }

        [JsonProperty("discounted_amt")]
        public Decimal DiscountedAmount { get; set; }

        [JsonProperty("purchase_org_transId")]
        public string PurchaseOrganizationTransactionId { get; set; }

        [JsonProperty("bus_payType")]
        public string BusinessPaymentType { get; set; }

        [JsonProperty("created_time")]
        public DateTime Created { get; set; }

        [JsonProperty("updated_time")]
        public DateTime Updated { get; set; }

        [JsonProperty("charge")]
        public Charge Charge { get; set; }

        [JsonProperty("balance")]
        public Balance Balance { get; set; }

        [JsonProperty("refund")]
        public Refund Refund { get; set; }
    }
}
