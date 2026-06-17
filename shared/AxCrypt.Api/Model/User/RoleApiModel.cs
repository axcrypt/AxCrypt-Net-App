using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AxCrypt.Api.Model.User
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RoleApiModel
    {
        [JsonProperty("userEmail")]
        public string UserEmail { get; set; } = string.Empty;

        [JsonProperty("rolename")]
        public string RoleName { get; set; } = string.Empty;
    }
}