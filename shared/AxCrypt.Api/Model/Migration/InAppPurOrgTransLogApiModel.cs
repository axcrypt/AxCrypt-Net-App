using Newtonsoft.Json;
using System;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]
    public class InAppPurOrgTransLogApiModel : BaseApiModel
    {
        public InAppPurOrgTransLogApiModel()
        {
        }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("useremail")]
        public string UserEmail { get; set; }

        [JsonProperty("originaltransactionid")]
        public string OriginalTransactionId { get; set; }

        [JsonProperty("productid")]
        public string ProductId { get; set; }

        [JsonProperty("purchasedatems")]
        public string PurchaseDateMS { get; set; }

        [JsonProperty("expiresdatems")]
        public string ExpiresDateMS { get; set; }
    }
}