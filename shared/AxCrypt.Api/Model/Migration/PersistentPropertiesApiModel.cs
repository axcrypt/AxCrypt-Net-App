using Newtonsoft.Json;
using System;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PersistentPropertiesApiModel : BaseApiModel
    {
        public PersistentPropertiesApiModel()
        { }

        [JsonProperty("legacymailBatchcounter")]
        public int LegacyMailBatchCounter { get; set; }

        [JsonProperty("enumerationenabled")]
        public bool EnumerationEnabled { get; set; }

        [JsonProperty("legacyimportSkipToLine")]
        public int LegacyImportSkipToLine { get; set; }

        [JsonProperty("siterestart")]
        public int SiteRestart { get; set; }

        [JsonProperty("axcryptregistrationcounter")]
        public int AxCryptRegistrationCounter { get; set; }

        [JsonProperty("isemailenabled")]
        public bool IsEmailEnabled { get; set; }

        [JsonProperty("ismarkdownemailenabled")]
        public bool IsMarkdownEmailEnabled { get; set; }

        [JsonProperty("isautodeletionenabled")]
        public bool IsAutoDeletionEnabled { get; set; }

        [JsonProperty("insecuremacVersionlist")]
        public string? InsecureMacVersionList { get; set; }

        [JsonProperty("insecurewindowsdesktopversionlist")]
        public string? InsecureWindowsDesktopVersionList { get; set; }

        [JsonProperty("Privatecomputerformstimeout")]
        public TimeSpan PrivateComputerFormsTimeout { get; set; }

        [JsonProperty("unreliablemacversionlist")]
        public string? UnreliableMacVersionList { get; set; }

        [JsonProperty("unreliablewindowsdesktopversionlist")]
        public string? UnreliableWindowsDesktopVersionList { get; set; }
    }
}