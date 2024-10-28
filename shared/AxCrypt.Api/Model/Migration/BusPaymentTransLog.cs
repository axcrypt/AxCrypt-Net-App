using System;
using Newtonsoft.Json;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]

    public class BusPaymentTransLog : BaseApiModel
    {
        public BusPaymentTransLog(long id,string businesssubscriptionid,string businesspaymenttype,string paymenttransactionid,DateTime createdUtc,DateTime updatedUtc,DateTime? deletedUtc)
        {
            Id = id;
            BusinessSubscriptionId = businesssubscriptionid;
            BusinessPaymentType = businesspaymenttype;
            PaymentTransactionId = paymenttransactionid;
            CreatedUtc = createdUtc;
            UpdatedUtc = updatedUtc;
            DeletedUtc = deletedUtc;
        }

        [JsonProperty("bus_subs_id")]
        public string BusinessSubscriptionId { get; set; }

        [JsonProperty("bus_pymt_type")]
        public string BusinessPaymentType { get; set; }

        [JsonProperty("pymt_trans_id")]
        public string PaymentTransactionId { get; set; }

    }
}
