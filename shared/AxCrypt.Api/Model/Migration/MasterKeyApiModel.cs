using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MasterKeyApiModel : BaseApiModel
    {
        public MasterKeyApiModel(long id, string businesssubscriptionid, string businessgroupid, DateTime timestamp, string thumbprint, string publickey, DateTime createdUtc, DateTime updatedUtc, DateTime? deletedUtc)
        {
            Id = id;
            BusinessSubscriptionId = businesssubscriptionid;
            BusinessGroupId = businessgroupid;
            TimeStamp = timestamp;
            ThumbPrint = thumbprint;
            PublicKey = publickey;
            CreatedUtc = createdUtc;
            UpdatedUtc = updatedUtc;
            DeletedUtc = deletedUtc;
        }

        [JsonProperty("bus_subs_id")]
        public string BusinessSubscriptionId { get; set; }

        [JsonProperty("bus_grp_Id")]
        public string BusinessGroupId { get; set; }

        [JsonProperty("time_stamp")]
        public DateTime TimeStamp { get; set; } = DateTime.MinValue;

        [JsonProperty("thumb_print")]
        public string ThumbPrint { get; set; }

        [JsonProperty("public_key")]
        public string PublicKey { get; set; }

        [JsonProperty("masterprivatekey")]
        public MasterPrivateKey MasterPrivateKey { get; set; }
    }
}