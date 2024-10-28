using Newtonsoft.Json;
using System;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SignInLogApiModel : BaseApiModel
    {
        public SignInLogApiModel()
        { }

        [JsonProperty("email")]
        public string? Email { get; set; }
    }
}