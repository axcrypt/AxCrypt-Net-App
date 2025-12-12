using AxCrypt.Api.Model.TextEncryption;
using AxCrypt.Common;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Service;
using AxCrypt.Core.Service.TextEncryption;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Helpers
{
    public static class TextShareApiHelper
    {
        public static async Task<Guid> ShareTextAsync(LogOnIdentity logOnIdentity, TextEncryptionApiModel textEncryptionApiModel)
        {
            if (textEncryptionApiModel == null)
            {
                return Guid.Empty;
            }

            if (logOnIdentity == LogOnIdentity.Empty)
            {
                return Guid.Empty;
            }

            return await New<LogOnIdentity, ITextEncryptionService>(logOnIdentity).ShareTextAsync(textEncryptionApiModel);
        }

        public static async Task<IEnumerable<UserPublicKey>?> GetAsync(KnownPublicKeys knownPublicKeys, IEnumerable<EmailAddress> emails)
        {
            emails = emails.Where(email => email != EmailAddress.Parse(New<UserSettings>().LicenseAuthorityEmail));
            if (emails == null || !emails.Any())
            {
                return null;
            }

            IEnumerable<UserPublicKey>? keys = knownPublicKeys.PublicKeys.Where(upk => emails.Contains(upk.Email) && New<UserPublicKeyUpdateStatus>().Status(upk) == PublicKeyUpdateStatus.RecentlyUpdated);
            if (keys != null && keys.Any())
            {
                return keys;
            }

            if (New<AxCryptOnlineState>().IsOffline)
            {
                return keys;
            }

            LogOnIdentity identity = New<Core.UI.KnownIdentities>().DefaultEncryptionIdentity;
            if (identity == LogOnIdentity.Empty || identity.UserEmail == EmailAddress.Empty)
            {
                return keys;
            }

            IAccountService accountService = New<LogOnIdentity, IAccountService>(identity);
            if (await accountService.IsAccountSourceLocalAsync())
            {
                return keys;
            }

            if (!New<LicensePolicy>().Capabilities.Has(LicenseCapability.KeySharing))
            {
                return keys;
            }

            IEnumerable<UserPublicKey> userPublicKeys = await UserPublicKeyAsync(emails);

            if (userPublicKeys != null && userPublicKeys.Any())
            {
                foreach (UserPublicKey userPublicKey in userPublicKeys)
                {
                    knownPublicKeys.AddOrReplace(userPublicKey);
                    New<UserPublicKeyUpdateStatus>().SetStatus(userPublicKey, PublicKeyUpdateStatus.RecentlyUpdated);
                }
            }
            return userPublicKeys;
        }

        private static async Task<IEnumerable<UserPublicKey>> UserPublicKeyAsync(IEnumerable<EmailAddress> sharedUsers)
        {
            LogOnIdentity logOnIdentity = New<Core.UI.KnownIdentities>().DefaultEncryptionIdentity;
            if (logOnIdentity == LogOnIdentity.Empty)
            {
                return null!;
            }

            IEnumerable<UserPublicKey> userPublicKeys = await New<LogOnIdentity, ITextEncryptionService>(logOnIdentity).GetUserPublicKeyAsync(sharedUsers);
            return userPublicKeys;
        }

        public static LogOnIdentity EncryptionIdentity(this Passphrase passphrase)
        {
            LogOnIdentity logOnIdentity = New<KnownIdentities>().DefaultEncryptionIdentity;
            if (passphrase != Passphrase.Empty)
            {
                logOnIdentity = new LogOnIdentity(passphrase);
            }

            return logOnIdentity;
        }
    }
}
