using Newtonsoft.Json;
using System;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]
    public class KeyLookupLogApiModel : BaseApiModel
    {
        public KeyLookupLogApiModel()
        {
        }

        [JsonProperty("userLookedUp")]
        public string? UserLookedUp { get; set; }

        [JsonProperty("userLookingUp")]
        public string? UserLookingUp { get; set; }
    }
}