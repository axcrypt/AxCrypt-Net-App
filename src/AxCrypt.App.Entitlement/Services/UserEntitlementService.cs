using AxCrypt.Api.Model;
using AxCrypt.Api.Model.Entitlement;
using AxCrypt.App.Entitlement.Models;
using AxCrypt.Content;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Service.Entitlement;
using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Entitlement.Services
{
    public class UserEntitlementService
    {
        private IDictionary<LimitedCapability, UsageLimit> _userEntitlementMap = null!;

        public async Task InitializeUserUsageLimit(SubscriptionLevel subLevel)
        {
            EntitlementApiModel entitleModel = await New<LogOnIdentity, IEntitlementService>(Identity()).GetUserUsageCountAsync(subLevel.ToString());
            UpdateUserEntitlementCount(entitleModel);
        }

        private void UpdateUserEntitlementCount(EntitlementApiModel entitleModel)
        {
            if (entitleModel == null || entitleModel == EntitlementApiModel.Empty)
            {
                _userEntitlementMap = null!;
                return;
            }

            _userEntitlementMap = new Dictionary<LimitedCapability, UsageLimit>()
            {
                [LimitedCapability.StrongerEncryption] = new UsageLimit
                {
                    UsedCount = entitleModel.EncryptedFiles,
                    MaxCount = entitleModel.MaxEncryptFileLimit,
                    ValidationMessage = $"File Encryption limit exceeded. Maximum allowed is {entitleModel.MaxEncryptFileLimit} files."
                },
                [LimitedCapability.SecureFolders] = new UsageLimit
                {
                    UsedCount = entitleModel.SecuredFolders,
                    MaxCount = entitleModel.MaxSecureFolderLimit,
                    ValidationMessage = $"Secured Folder limit exceeded. Maximum allowed is {entitleModel.MaxSecureFolderLimit} folders."
                },
                [LimitedCapability.CreateSecret] = new UsageLimit
                {
                    UsedCount = entitleModel.SecretCreateCount,
                    MaxCount = entitleModel.MaxSecretCreateLimit,
                    ValidationMessage = $"Create Password limit exceeded. Maximum allowed is {entitleModel.MaxSecretCreateLimit} to create secrets."
                },
                [LimitedCapability.ShareSecret] = new UsageLimit
                {
                    UsedCount = entitleModel.SecretShareCount,
                    MaxCount = entitleModel.MaxSecretShareLimit,
                    ValidationMessage = $"Share Password limit exceeded. Maximum allowed is {entitleModel.MaxSecretShareLimit} to create secrets."
                },
                [LimitedCapability.TextEncryption] = new UsageLimit
                {
                    UsedCount = entitleModel.TextEncryptionLimit,
                    MaxCount = entitleModel.MaxTextEncryptionLimit,
                    ValidationMessage = $"Text Encryption limit exceeded. Maximum allowed is {entitleModel.MaxTextEncryptionLimit} letters."
                },
                [LimitedCapability.ShareEncryptedText] = new UsageLimit
                {
                    UsedCount = entitleModel.TextEncryptionShared,
                    MaxCount = entitleModel.MaxTextEncryptionSharingLimit,
                    ValidationMessage = $"Text Encryption limit exceeded. Maximum allowed {entitleModel.MaxTextEncryptionSharingLimit} recipients to share."
                },
                [LimitedCapability.KeySharing] = new UsageLimit
                {
                    UsedCount = entitleModel.KeySharingCount,
                    MaxCount = entitleModel.MaxKeySharingLimit,
                    ValidationMessage = $"Key Sharing limit exceeded. Maximum allowed is {entitleModel.MaxKeySharingLimit} share key."
                },
                [LimitedCapability.SendSecuredMessages] = new UsageLimit
                {
                    UsedCount = entitleModel.SecureMessageCount,
                    MaxCount = entitleModel.MaxSecureMessageLimit,
                    ValidationMessage = $"Secure Message limit exceeded. Maximum allowed is {entitleModel.MaxSecureMessageLimit} messages."
                },
                [LimitedCapability.ShareSecuredMessages] = new UsageLimit
                {
                    UsedCount = entitleModel.SecureMessageRecipients,
                    MaxCount = entitleModel.MaxSecureMessageRecipientLimit,
                    ValidationMessage = $"Secure Message limit exceeded. Maximum allowed is {entitleModel.MaxSecureMessageLimit} recipients."
                },
                [LimitedCapability.SecureWipe] = new UsageLimit
                {
                    UsedCount = entitleModel.DeletedFiles,
                    MaxCount = entitleModel.MaxDeleteFileLimit,
                    ValidationMessage = $"Secure Delete limit exceeded. Maximum allowed is {entitleModel.MaxDeleteFileLimit} files."
                }
            };
        }

        public async Task<bool> UserHasCapability(LimitedCapability licenceCapability, SubscriptionLevel subscriptionLevel)
        {
            if (subscriptionLevel > SubscriptionLevel.Lite)
                return true;

            UsageLimit usage = await GetUserUsageLimit(licenceCapability, subscriptionLevel);

            if (usage == null)
                return true;

            int current = usage.UsedCount;
            int? max = usage.MaxCount;

            if (max == null)
            {
                return true;
            }

            if (current >= max)
            {
                //await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.AlertText, usage.ValidationMessage);
                return false;
            }

            return true;
        }

        public async Task<bool> UserHasTextEncryptionLimit(string inputText, LimitedCapability licenceCapability, SubscriptionLevel subscriptionLevel)
        {
            UsageLimit usage = await GetAvailableUsageLimit(licenceCapability, subscriptionLevel);
            if (usage == null)
                return true;

            if (inputText.Length > usage.MaxCount)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.AlertText, usage.ValidationMessage);
                return false;
            }

            return true;
        }

        public async Task<int> GetRemainingCount(LimitedCapability capability, SubscriptionLevel subscriptionLevel, int selectedCount = 0)
        {
            if (subscriptionLevel > SubscriptionLevel.Lite)
                return selectedCount;

            UsageLimit usageLimit = await GetUserUsageLimit(capability, subscriptionLevel);

            if (usageLimit == null)
                return selectedCount;

            int availableCount = Math.Max(0, usageLimit.MaxCount - usageLimit.UsedCount);

            if (availableCount <= 0)
            {
                //await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.AlertText, $"you have exceed the Usage Count {usageLimit.MaxCount}");
                return availableCount;
            }

            if (selectedCount > availableCount)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.AlertText, $"{usageLimit.ValidationMessage} You can only {availableCount} file(s).");
            }

            return availableCount;
        }

        public async Task<UsageLimit> GetAvailableUsageLimit(LimitedCapability licenceCapability, SubscriptionLevel subscriptionLevel)
        {
            if (subscriptionLevel > SubscriptionLevel.Lite)
                return null;

            return await GetUserUsageLimit(licenceCapability, subscriptionLevel);
        }

        private async Task<UsageLimit> GetUserUsageLimit(LimitedCapability licenceCapability, SubscriptionLevel subscriptionLevel)
        {
            if (_userEntitlementMap == null)
            {
                await InitializeUserUsageLimit(subscriptionLevel);
            }

            if (_userEntitlementMap == null)
            {
                return new UsageLimit
                {
                    UsedCount = 0,
                    MaxCount = 0,
                    ValidationMessage = "There is a mismatch in your entitlement data. Please enable your account and log in again."
                };
            }

            if (_userEntitlementMap.TryGetValue(licenceCapability, out UsageLimit? usage))
            {
                return usage;
            }

            return null!;
        }

        public async Task<bool> InsertUserUsageCount(LimitedCapability licenceCapability, SubscriptionLevel subscriptionLevel)
        {
            if (subscriptionLevel > SubscriptionLevel.Lite)
                return true;

            // The entitlement map is null until the first successful API
            // fetch. Guard against it so recording usage (e.g. right after
            // a file is encrypted) can't throw a NullReferenceException
            // when the app hasn't synced yet / is offline.
            if (_userEntitlementMap == null || !_userEntitlementMap.ContainsKey(licenceCapability))
                return true;

            _userEntitlementMap[licenceCapability].UsedCount++;

            string capability = licenceCapability.ToString();
            EntitlementRequestOptions requestOptions = new EntitlementRequestOptions(New<KnownIdentities>().DefaultEncryptionIdentity.UserEmail.Address, capability);

            requestOptions.SubscriptionLevel = subscriptionLevel.ToString();
            return await New<LogOnIdentity, IEntitlementService>(Identity()).InsertUserUsageCount(requestOptions);
        }

        public async Task SyncUserUsageCountAsync(LimitedCapability licenceCapability, SubscriptionLevel subscriptionLevel, int usageCount)
        {
            if (subscriptionLevel > SubscriptionLevel.Lite)
                return;

            IEntitlementService entitlementService = New<LogOnIdentity, IEntitlementService>(Identity());

            EntitlementApiModel? model = subscriptionLevel switch
            {
                SubscriptionLevel.Free => await entitlementService.GetUserUsageCountAsync("Free"),
                SubscriptionLevel.Lite => await entitlementService.GetUserUsageCountAsync("Lite"),
                _ => null
            };

            if (model == null)
                return;

            IncrementUserUsageCount(model, licenceCapability, usageCount);

            await entitlementService.SyncUserUsageCount(model);
        }

        private void IncrementUserUsageCount(EntitlementApiModel userUageLimitInfo, LimitedCapability usedFeature, int UsageCount)
        {
            switch (usedFeature)
            {
                case LimitedCapability.StrongerEncryption:
                    userUageLimitInfo.EncryptedFiles += UsageCount;
                    break;

                case LimitedCapability.CreateSecret:
                    userUageLimitInfo.SecretCreateCount += UsageCount;
                    break;

                case LimitedCapability.ShareEncryptedText:
                    userUageLimitInfo.TextEncryptionShared += UsageCount;
                    break;

                case LimitedCapability.SecureWipe:
                    userUageLimitInfo.DeletedFiles += UsageCount;
                    break;

                case LimitedCapability.SecureFolders:
                    userUageLimitInfo.SecuredFolders += UsageCount;
                    break;

                case LimitedCapability.KeySharing:
                    userUageLimitInfo.KeySharingCount += UsageCount;
                    break;

                case LimitedCapability.SendSecuredMessages:
                    userUageLimitInfo.SecureMessageCount += UsageCount;
                    break;

                case LimitedCapability.ShareSecuredMessages:
                    userUageLimitInfo.SecureMessageRecipients += UsageCount;
                    break;

                default:
                    break;
            }
        }

        private static LogOnIdentity Identity()
        {
            return New<KnownIdentities>().DefaultEncryptionIdentity;
        }
    }
}