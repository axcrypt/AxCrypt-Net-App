using Newtonsoft.Json;

namespace AxCrypt.Api.Model.MFA
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MultiFactorAuthApiModel : BaseApiModel
    {
        public MultiFactorAuthApiModel()
        { }

        [JsonProperty("userEmail")]
        public string UserEmail { get; set; }

        [JsonProperty("uniqueKey")]
        public string UniqueKey { get; set; }

        [JsonProperty("backupCodeSalt")]
        public string BackupCodeSalt { get; set; }

        [JsonProperty("backupCodeHash")]
        public string BackupCodeHash { get; set; }

        [JsonProperty("phoneNumber")]
        public string PhoneNumber { get; set; }

        [JsonProperty("mfaenabledtypes")]
        public string MfaEnabledTypes { get; set; }

        [JsonProperty("userdevice")]
        public string UserDevice { get; set; }

        [JsonProperty("rememberuntil")]
        public DateTime? RememberUntil { get; set; }
    }
}