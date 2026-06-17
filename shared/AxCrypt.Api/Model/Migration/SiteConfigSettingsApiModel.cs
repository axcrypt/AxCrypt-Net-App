using Newtonsoft.Json;
using System;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SiteConfigSettingsApiModel : BaseApiModel
    {
        public SiteConfigSettingsApiModel()
        { }


        [JsonProperty("siteconfigkey")]
        public string? SiteConfigKey { get; set; }

        [JsonProperty("siteconfigvalue")]
        public string? SiteConfigValue { get; set; }
    }
}