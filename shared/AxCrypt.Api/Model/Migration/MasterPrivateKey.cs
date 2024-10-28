using System;
using Newtonsoft.Json;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MasterPrivateKey :BaseApiModel
    {
        public MasterPrivateKey(long id,DateTime timestamp,string key,string user,string status,int masterkeyid,DateTime createdUtc,DateTime updatedUtc,DateTime? deletedUtc)
        {
            Id= id;
            TimeStamp = timestamp;
            Key = key;
            User= user;
            Status= status;
            MasterKeyId= masterkeyid;
            CreatedUtc = createdUtc;
            UpdatedUtc = updatedUtc;
            DeletedUtc = deletedUtc;
        }

        [JsonProperty("timestamp")]
        public DateTime TimeStamp { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("user")]
        public string User { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("masterkey_id")]
        public int MasterKeyId { get; set; }
    }
}
