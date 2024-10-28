using System;
using Newtonsoft.Json;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]

    public class SubscriptionIndexApiModel : BaseApiModel
    {
          
        public SubscriptionIndexApiModel(long id, string businesssubscriptionid,int memberstate,string email,DateTime createdUtc,DateTime updatedUtc,DateTime? deletedUtc)
        {
            Id = id;
            BusinessSubscriptionId = businesssubscriptionid;
            MemberState = memberstate;
            Email = email;
            CreatedUtc = createdUtc;
            UpdatedUtc = updatedUtc;
            DeletedUtc = deletedUtc;

        }

        [JsonProperty("bus_subs_id")]
        public string BusinessSubscriptionId { get; set; }

        [JsonProperty("member_state")]
        public int MemberState { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

    }
}
