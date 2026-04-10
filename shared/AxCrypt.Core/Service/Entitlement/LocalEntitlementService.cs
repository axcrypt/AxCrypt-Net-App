using AxCrypt.Abstractions;
using AxCrypt.Api.Model;
using AxCrypt.Api.Model.Entitlement;
using AxCrypt.Api.Model.Secret;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using System.Text.Json;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.Service.Entitlement
{
    public class LocalEntitlementService : IEntitlementService
    {
        private IDataContainer _workContainer;

        public static readonly string EntitlementFileName = "AxEntitlement.txt";

        public LocalEntitlementService(LogOnIdentity identity, IDataContainer workContainer)
        {
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }
            if (workContainer == null)
            {
                throw new ArgumentNullException(nameof(workContainer));
            }

            Identity = identity;
            _workContainer = workContainer;
        }

        public IEntitlementService Refresh()
        {
            return this;
        }

        private IDataStore AxEntitlementStore
        {
            get
            {
                return _workContainer.FileItemInfo(EntitlementFileName);
            }
        }

        public LogOnIdentity Identity
        {
            get;
        }

        /// <summary>
        /// Fetches the free user usage count.
        /// </summary>
        /// <returns>
        /// The free user usage count.
        /// </returns>
        public Task<EntitlementApiModel> GetUserUsageCountAsync(string subsLevel)
        {
            if (Identity.UserEmail == EmailAddress.Empty)
            {
                throw new InvalidOperationException("The account service requires a user.");
            }

            EntitlementApiModel entitlementModel = LoadEnititlement();
            return Task.FromResult(entitlementModel);
        }

        public Task<bool> InsertUserUsageCount(EntitlementRequestOptions requestModel)
        {
            if (Identity.UserEmail == EmailAddress.Empty)
            {
                throw new InvalidOperationException("The account service requies a user.");
            }

            EntitlementApiModel entitlementModel = LoadEnititlement();

            if (!IncrementUserUsageCount(entitlementModel, requestModel.Feature))
                Task.FromResult(false);

            return Task.FromResult(InternalSaveSecrets(entitlementModel));
        }

        public Task<bool> SyncUserUsageCount(EntitlementApiModel entitlementModel)
        {
            if (Identity.UserEmail == EmailAddress.Empty)
            {
                throw new InvalidOperationException("The account service requies a user.");
            }

            return Task.FromResult(InternalSaveSecrets(entitlementModel));
        }

        private bool InternalSaveSecrets(EntitlementApiModel entitlementApiModel)
        {
            string hashValue = HmacService.ComputeHmac(entitlementApiModel);
            string entitlementValue = JsonSerializer.Serialize(entitlementApiModel);

            New<UserSettings>().EntitlementHashKey = hashValue;

            using (StreamWriter writer = new StreamWriter(AxEntitlementStore.OpenWrite()))
            {
                SerializeTo(writer, entitlementApiModel);
            }

            return true;
        }

        #region internal helper methods

        private EntitlementApiModel LoadEnititlement()
        {
            if (!AxEntitlementStore.IsAvailable)
            {
                return EntitlementApiModel.Empty;
            }

            using (StreamReader reader = new StreamReader(AxEntitlementStore.OpenRead()))
            {
                EntitlementApiModel entitlementModel = DeserializeFrom(reader);

                if (entitlementModel == null)
                {
                    entitlementModel = EntitlementApiModel.Empty;
                }

                if (!HmacService.VerifyHmac(entitlementModel))
                {
                    entitlementModel = EntitlementApiModel.Empty;
                }

                return entitlementModel;
            }
        }

        private bool IncrementUserUsageCount(EntitlementApiModel userUageLimitInfo, string usedFeature)
        {
            if (!Enum.TryParse<LimitedCapability>(usedFeature, true, out LimitedCapability Feature))
            {
                return false;
            }

            switch (Feature)
            {
                case LimitedCapability.StrongerEncryption:
                    if (userUageLimitInfo.EncryptedFiles >= userUageLimitInfo.MaxEncryptFileLimit)
                        return false;

                    userUageLimitInfo.EncryptedFiles += 1;
                    break;

                case LimitedCapability.SecureWipe:
                    if (userUageLimitInfo.DeletedFiles >= userUageLimitInfo.MaxDeleteFileLimit)
                        return false;

                    userUageLimitInfo.DeletedFiles += 1;
                    break;

                case LimitedCapability.SecureFolders:
                    if (userUageLimitInfo.SecuredFolders >= userUageLimitInfo.MaxSecureFolderLimit)
                        return false;

                    userUageLimitInfo.SecuredFolders += 1;
                    break;

                case LimitedCapability.KeySharing:
                    if (userUageLimitInfo.KeySharingCount >= userUageLimitInfo.MaxKeySharingLimit)
                        return false;

                    userUageLimitInfo.KeySharingCount += 1;
                    break;

                case LimitedCapability.SendSecuredMessages:
                    if (userUageLimitInfo.SecureMessageCount >= userUageLimitInfo.MaxSecureMessageLimit)
                        return false;

                    userUageLimitInfo.SecureMessageCount += 1;
                    break;

                case LimitedCapability.CreateSecret:
                    if (userUageLimitInfo.SecretCreateCount >= userUageLimitInfo.MaxSecretCreateLimit)
                        return false;

                    userUageLimitInfo.SecretCreateCount += 1;
                    break;

                default:
                    return false;
            }

            return true;
        }

        #endregion internal helper methods

        #region Text Serializers

        public static EntitlementApiModel DeserializeFrom(TextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            string serialized = reader.ReadToEnd();
            return New<IStringSerializer>().Deserialize<EntitlementApiModel>(serialized);
        }

        public void SerializeTo(TextWriter writer, EntitlementApiModel entitlementApiModel)
        {
            string serializedString = New<IStringSerializer>().Serialize(entitlementApiModel);
            WriteToFile(writer, serializedString);
        }

        public void SerializeShareSecretsTo(TextWriter writer, IEnumerable<ShareSecretApiModel> encryptedSharedSecretsModel)
        {
            string serializedString = New<IStringSerializer>().Serialize(encryptedSharedSecretsModel);
            WriteToFile(writer, serializedString);
        }

        private static void WriteToFile(TextWriter writer, string serializedString)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            writer.Write(serializedString);
        }

        #endregion Text Serializers
    }
}