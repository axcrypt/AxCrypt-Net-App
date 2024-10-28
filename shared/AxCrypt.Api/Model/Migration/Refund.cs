using System;
using Newtonsoft.Json;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Refund : BaseApiModel
    {
        public Refund(long id,string refundinfo,string paymenttransactionid,DateTime createdUtc,DateTime updatedUtc,DateTime? deletedUtc) 
        {
            Id = id;
            RefundInfo = refundinfo;
            PaymentTransactionId = paymenttransactionid;
            CreatedUtc = createdUtc;
            UpdatedUtc = updatedUtc;
            DeletedUtc = deletedUtc;

        }
        
        [JsonProperty("refund_info")]
        public string RefundInfo { get; set; }

        [JsonProperty("pymt_trans_id")]
        public string PaymentTransactionId { get; set; }
    }
}
