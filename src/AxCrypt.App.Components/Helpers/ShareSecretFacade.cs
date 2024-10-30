using AxCrypt.Abstractions;
using AxCrypt.Api.Extension;
using AxCrypt.Api.Model;
using AxCrypt.Api.Model.Secret;
using AxCrypt.App.Components.Password;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Secrets;
using AxCrypt.Core.Service;
using AxCrypt.Core.Service.Secrets;
using AxCrypt.Core.UI;
using AxCrypt.Cryptor;
using AxCrypt.Cryptor.Model;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Api.Components.Helper
{
    public static class ShareSecretFacade
    {
        public static async Task ShareSecret(SecretClientModel secret, LogOnIdentity identity)
        {
            if (secret == null)
            {
                return;
            }

            if (secret.Share == null)
            {
                return;
            }

            IList<UserPublicKey> usersPublicKeys = new List<UserPublicKey>();
            IList<SecretSharedUser> sharedWithUsers = new List<SecretSharedUser>();

            foreach (SecretSharedUser shared in secret.Share.SharedWith)
            {
                usersPublicKeys.Add(await UserPublicKey(shared, identity));
                sharedWithUsers.Add(shared);
            }

            EncryptedSecretApiModel encryptedData = EncryptedSecretApiModel.Empty;
            if (secret.Share.SharedWith.Any())
            {
                encryptedData = await EncryptSecretAsync(secret, usersPublicKeys, identity);
            }

            await ShareSecretAsync(secret, sharedWithUsers, encryptedData);
        }

        private static async Task<UserPublicKey> UserPublicKey(SecretSharedUser adminUser, LogOnIdentity identity)
        {
            return await New<LogOnIdentity, ISecretsService>(identity).OtherPublicKeyAsync(adminUser.UserEmail);
        }

        private static async Task<EncryptedSecretApiModel> EncryptSecretAsync(SecretClientModel secret, IList<UserPublicKey> usersPublicKeys, LogOnIdentity identity)
        {
            SecretsClientModel secretJson = new SecretsClientModel()
            {
                Secrets = new List<SecretClientModel> { secret },
            };

            EncryptedSecretApiModel encryptedData = await TextCryptor.EncryptAsync(identity, secretJson, usersPublicKeys);
            return encryptedData;
        }

        private static async Task ShareSecretAsync(SecretClientModel secret, IList<SecretSharedUser> sharedWithUsers, EncryptedSecretApiModel encryptedData)
        {
            string encryptedSecret = "";
            if (encryptedData.Cipher != null)
            {
                encryptedSecret = encryptedData.Cipher.GetCipherBytes();
            }
            LogOnIdentity logOnIdentity = New<AxCrypt.Core.UI.KnownIdentities>().DefaultEncryptionIdentity;

            IEnumerable<SharedUserApiModel> sharedUserApiModelList = sharedWithUsers.Select(us => new SharedUserApiModel(us.UserEmail.Address, us.VisibilityType.ToString(), us.Visibility));
            ShareSecretApiModel shareSecretModel = new ShareSecretApiModel(0, logOnIdentity.UserEmail.Address, secret.Id, encryptedSecret, sharedUserApiModelList, New<INow>().Utc, New<INow>().Utc, null);

            await New<LogOnIdentity, ISecretsService>(logOnIdentity).ShareSecretsAsync(shareSecretModel);
        }

        public static async Task<SecretClientCollection> GetSharedSecretsListAsync(IEnumerable<ShareSecretApiModel> secretModel, IEnumerable<EncryptionKey> keys, LogOnIdentity identity)
        {
            if (!secretModel.Any())
            {
                return null;
            }
            UserKeyPair currentKeyPair = await New<LogOnIdentity, IAccountService>(identity).CurrentKeyPairAsync();

            identity = new LogOnIdentity(new List<UserKeyPair>() { currentKeyPair }, identity.Passphrase);

            return GetSecretCollection(secretModel, identity);
        }

        private static SecretClientCollection GetSecretCollection(IEnumerable<ShareSecretApiModel> secretModel, LogOnIdentity identity)
        {
            SecretClientCollection secrets = new SecretClientCollection();
            foreach (ShareSecretApiModel shareSecret in secretModel)
            {
                EncryptedSecretApiModel encryptedSecret = new EncryptedSecretApiModel
                {
                    Cipher = shareSecret.EncryptedSecret.GetCipherString(),
                    UserEmail = identity.UserEmail.Address,
                    CreatedUtc = shareSecret.CreatedUtc,
                };

                IEnumerable<SecretClientModel> sharedSecrets = TextCryptor.GetClientSecrets(identity, encryptedSecret);
                SecretClientModel secret = sharedSecrets.FirstOrDefault();
                if (secret != null)
                {
                    IEnumerable<SecretSharedUser> secretSharedUsers = shareSecret.SharedWith.Select(sw => new SecretSharedUser(AxCrypt.Core.UI.EmailAddress.Parse(sw.UserEmail), (SecretShareVisibility)Enum.Parse(typeof(SecretShareVisibility), sw.VisibilityType)));
                    secret.Share = new ShareSecret(secretSharedUsers, shareSecret.OwnerEmail, shareSecret.CreatedUtc);
                    secrets.Add(secret);
                }
            }

            return secrets;
        }

        public static async Task<bool> UpdateSharedVisibilityAsync(SecretClientModel secret, LogOnIdentity identity = null)
        {
            if (secret.Share == null)
            {
                return false;
            }

            IEnumerable<SharedUserApiModel> sharedUserApiModelList = secret.Share.SharedWith.Select(us => new SharedUserApiModel(us.UserEmail.Address, us.VisibilityType.ToString(), us.Visibility));
            ShareSecretApiModel model = new ShareSecretApiModel(0, Identity().UserEmail.Address, secret.Id, "", sharedUserApiModelList, New<INow>().Utc, New<INow>().Utc, null);

            return await New<LogOnIdentity, ISecretsService>(Identity()).UpdateSecretSharedWithAsync(model);
        }

        public static async Task<bool> DeleteSharedWithAsync(SecretClientModel secret)
        {
            if (secret.Share == null)
            {
                return false;
            }

            IEnumerable<SharedUserApiModel> sharedUserApiModelList = secret.Share.SharedWith.Select(us => new SharedUserApiModel(us.UserEmail.Address, us.VisibilityType.ToString(), us.Visibility, New<INow>().Utc));
            ShareSecretApiModel model = new ShareSecretApiModel(0, Identity().UserEmail.Address, secret.Id, "", sharedUserApiModelList, New<INow>().Utc, New<INow>().Utc, null);

            return await New<LogOnIdentity, ISecretsService>(Identity()).UpdateSecretSharedWithAsync(model);
        }

        public static async Task<bool> DeleteSecretsSharedAsync(SecretClientModel secret)
        {
            if (secret.Share == null)
            {
                return false;
            }

            ShareSecretApiModel model = new ShareSecretApiModel(0, Identity().UserEmail.Address, secret.Id, "", null, New<INow>().Utc, New<INow>().Utc, null);
            return await New<LogOnIdentity, ISecretsService>(Identity()).DeleteSecretSharedAsync(model);
        }

        private static LogOnIdentity Identity()
        {
            return New<KnownIdentities>().DefaultEncryptionIdentity;
        }
    }
}