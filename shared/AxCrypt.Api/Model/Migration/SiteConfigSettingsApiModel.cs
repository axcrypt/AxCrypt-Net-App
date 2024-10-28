using Newtonsoft.Json;
using System;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SiteConfigSettingsApiModel : BaseApiModel
    {
        public SiteConfigSettingsApiModel()
        { }

        [JsonProperty("key")]
        public string? Key { get; set; }

        [JsonProperty("value")]
        public string? Value { get; set; }
    }
}