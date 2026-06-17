using Newtonsoft.Json;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]
    public class WebUserSettingsApiModel : BaseApiModel
    {
        [JsonProperty("CultureName")]
        public string CultureName { get; set; }

        [JsonProperty("MessageCulture")]
        public string MessageCulture { get; set; }

        [JsonProperty("RestApiBaseUrl")]
        public string RestApiBaseUrl { get; set; }

        [JsonProperty("UpdateUrl")]
        public string UpdateUrl { get; set; }

        [JsonProperty("AccountWebUrl")]
        public string AccountWebUrl { get; set; }

        [JsonProperty("UpdateLevel")]
        public string UpdateLevel { get; set; }

        [JsonProperty("ApiTimeout")]
        public DateTime ApiTimeout { get; set; }

        [JsonProperty("LastUpdateCheckUtc")]
        public DateTime LastUpdateCheckUtc { get; set; }

        [JsonProperty("NewestKnownVersion")]
        public string NewestKnownVersion { get; set; }

        [JsonProperty("MostRecentVersionInformed")]
        public string MostRecentVersionInformed { get; set; }

        [JsonProperty("ThisVersion")]
        public string ThisVersion { get; set; }

        [JsonProperty("HideRecentFiles")]
        public bool HideRecentFiles { get; set; }

        [JsonProperty("DebugMode")]
        public bool DebugMode { get; set; }

        [JsonProperty("FolderOperationMode")]
        public string FolderOperationMode { get; set; }

        [JsonProperty("RestoreFullWindow")]
        public bool RestoreFullWindow { get; set; }

        [JsonProperty("IsFileImportHelpMessageAlreadyDisplayed")]
        public bool IsFileImportHelpMessageAlreadyDisplayed { get; set; }

        [JsonProperty("AxCrypt2HelpUrl")]
        public string AxCrypt2HelpUrl { get; set; }

        [JsonProperty("DisplayenEryptPassphrase")]
        public bool DisplayEncryptPassphrase { get; set; }

        [JsonProperty("DisplayDecryptPassphrase")]
        public bool DisplayDecryptPassphrase { get; set; }

        [JsonProperty("ThumbprintSalt")]
        public string ThumbprintSalt { get; set; }

        [JsonProperty("SettingsVersion")]
        public int SettingsVersion { get; set; }

        [JsonProperty("AsymmetricKeyBits")]
        public int AsymmetricKeyBits { get; set; }

        [JsonProperty("UserEmail")]
        public string UserEmail { get; set; }

        [JsonProperty("LicenseAuthorityEmail")]
        public string LicenseAuthorityEmail { get; set; }

        [JsonProperty("CustomInvitationMessage")]
        public string CustomInvitationMessage { get; set; }

        [JsonProperty("IsFirstSignIn")]
        public bool IsFirstSignIn { get; set; }

        [JsonProperty("OfflineMode")]
        public bool OfflineMode { get; set; }

        [JsonProperty("EncryptionUpgradeMode")]
        public string EncryptionUpgradeMode { get; set; }

        [JsonProperty("ShouldDisplayHelpOverlayAutomatically")]
        public bool ShouldDisplayHelpOverlayAutomatically { get; set; }

        [JsonProperty("ShouldNnotifyUserAboutCleaningWorkflow")]
        public bool ShouldNotifyUserAboutCleaningWorkflow { get; set; }

        [JsonProperty("FewFilesThreshold")]
        public int FewFilesThreshold { get; set; }

        [JsonProperty("DoNotShowAgain")]
        public string DoNotShowAgain { get; set; }

        [JsonProperty("InactivitySignOutTime")]
        public DateTime InactivitySignOutTime { get; set; }

        [JsonProperty("SecretsSortOrder")]
        public int SecretsSortOrder { get; set; }

        [JsonProperty("SecretsFilter")]
        public int SecretsFilter { get; set; }

        [JsonProperty("LongOperationThreshold")]
        public DateTime LongOperationThreshold { get; set; }

        [JsonProperty("LastInApprRviewInitiated")]
        public DateTime LastInAppReviewInitiated { get; set; }

        [JsonProperty("EncryptFilePropertiesDateModified")]
        public bool EncryptFilePropertiesDateModified { get; set; }

        [JsonProperty("EncryptFilePropertiesFileName")]
        public bool EncryptFilePropertiesFileName { get; set; }
    }
}