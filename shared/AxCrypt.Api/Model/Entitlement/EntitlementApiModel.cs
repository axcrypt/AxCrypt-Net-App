using Newtonsoft.Json;

namespace AxCrypt.Api.Model.Entitlement
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class EntitlementApiModel
    {
        public static EntitlementApiModel Empty = new EntitlementApiModel();

        public EntitlementApiModel()
        {
        }

        [JsonProperty("userId")]
        public int UserId { get; set; }

        [JsonProperty("encryptedFiles")]
        public int EncryptedFiles { get; set; }

        [JsonProperty("maxEncryptFileLimit")]
        public int MaxEncryptFileLimit { get; set; }

        [JsonProperty("deletedFiles")]
        public int DeletedFiles { get; set; }

        [JsonProperty("maxDeleteFileLimit")]
        public int MaxDeleteFileLimit { get; set; }

        [JsonProperty("secretCreateCount")]
        public int SecretCreateCount { get; set; }

        [JsonProperty("maxSecretCreateLimit")]
        public int MaxSecretCreateLimit { get; set; }

        [JsonProperty("secretShareCount")]
        public int SecretShareCount { get; set; }

        [JsonProperty("maxSecretShareLimit")]
        public int MaxSecretShareLimit { get; set; }

        [JsonProperty("secureMessageCount")]
        public int SecureMessageCount { get; set; }

        [JsonProperty("maxSecureMessageLimit")]
        public int MaxSecureMessageLimit { get; set; }

        [JsonProperty("secureMessageRecipients")]
        public int SecureMessageRecipients { get; set; }

        [JsonProperty("maxSecureMessageRecipientLimit")]
        public int MaxSecureMessageRecipientLimit { get; set; }

        [JsonProperty("securedFolders")]
        public int SecuredFolders { get; set; }

        [JsonProperty("maxSecureFolderLimit")]
        public int MaxSecureFolderLimit { get; set; }

        [JsonProperty("textEncryptionLimit")]
        public int TextEncryptionLimit { get; set; }

        [JsonProperty("maxTextEncryptionLimit")]
        public int MaxTextEncryptionLimit { get; set; }

        [JsonProperty("textEncryptionShared")]
        public int TextEncryptionShared { get; set; }

        [JsonProperty("maxTextEncryptionSharingLimit")]
        public int MaxTextEncryptionSharingLimit { get; set; }

        [JsonProperty("KeySharingCount")]
        public int KeySharingCount { get; set; }

        [JsonProperty("maxKeySharingLimit")]
        public int MaxKeySharingLimit { get; set; }
    }
}