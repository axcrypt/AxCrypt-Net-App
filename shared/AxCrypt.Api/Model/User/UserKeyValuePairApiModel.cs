using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Api.Model.User
{

    [JsonObject(MemberSerialization.OptIn)]
    public class UserKeyValuePairApiModel : BaseApiModel
    {
        [JsonProperty("username")]
        public string UserName { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
