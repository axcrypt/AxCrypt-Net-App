using AxCrypt.Abstractions;
using AxCrypt.Api.Extension;
using AxCrypt.Api.Model;
using AxCrypt.Api.Model.Secret;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Password;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Secrets;
using AxCrypt.Core.UI;
using AxCrypt.Cryptor;
using AxCrypt.App.Shared.Models.Secret;
using static AxCrypt.Abstractions.TypeResolve;
using System.Diagnostics.CodeAnalysis;

namespace AxCrypt.App.Shared.Facades;
public class SecretsFacade
{
    #region Private classes

    private class InternalSecret : Secret
    {
        public InternalSecret(Secret secret)
            : base(secret)
        {
        }

        private DateTime _lastUpdateUtc;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "The getter is currently unused but should still be there.")]
        public DateTime LastUpdateUtc
        {
            get { return _lastUpdateUtc; }
            set { _lastUpdateUtc = value; }
        }
    }

    private class InternalEncryptionKey : EncryptionKey
    {
        public InternalEncryptionKey(EncryptionKey key)
            : base(key)
        {
        }

        public new string DecryptPassphrase()
        {
            return base.DecryptPassphrase();
        }
    }

    #endregion Private classes

    public static async Task<bool> ProtectAsync(SecretClientModel secret)
    {
        if (secret == null)
        {
            throw new ArgumentNullException("secrets");
        }

        ICollection<SecretClientModel> nonEmptySecrets = FilterEmptySecrets(new List<SecretClientModel> { secret });
        if (nonEmptySecrets.Count == 0)
        {
            return false;
        }

        LogOnIdentity logOnIdentity = New<AxCrypt.Core.UI.KnownIdentities>().DefaultEncryptionIdentity;
        SecretClientCollection secretList = await GetSecrets(logOnIdentity);
        secretList.AddRange(nonEmptySecrets);

        return await EncryptSaveSecretsAsync(logOnIdentity, secretList);
    }

    private static async Task<bool> EncryptSaveSecretsAsync(LogOnIdentity logOnIdentity, SecretClientCollection secretCollection)
    {
        SecretsClientModel secretsClientModel = new SecretsClientModel()
        {
            Secrets = secretCollection.ToList(),
        };

        byte[] encryptedSecrets = await EncryptSecretsAsync(logOnIdentity, secretsClientModel);
        return await SaveSecretsAsync(logOnIdentity, encryptedSecrets);
    }


    private static async Task<byte[]> EncryptSecretsAsync(LogOnIdentity logOnIdentity, SecretsClientModel secretsClientModel)
    {
        EncryptionParameters encryptionParameters = await CreateEncryptionParameters(logOnIdentity);
        string serializedText = Serializer.Serialize(secretsClientModel);
        return await TextEncryption.EncryptAsync(encryptionParameters, serializedText);
    }

    private static async Task<bool> SaveSecretsAsync(LogOnIdentity logOnIdentity, byte[] encryptedSecrets)
    {
        string userEmail = logOnIdentity.UserEmail.Address;
        EncryptedSecretApiModel encryptedSecret = new EncryptedSecretApiModel()
        {
            UserEmail = userEmail,
            Cipher = encryptedSecrets,
            CreatedUtc = New<INow>().Utc
        };

        return await SecretsApiHelper.Insert(logOnIdentity, encryptedSecret);
    }

    public static async Task<SecretClientCollection> GetSecrets(LogOnIdentity logOnIdentity)
    {
        EncryptedSecretApiModel encryptedSecret = await SecretsApiHelper.GetSecrets(logOnIdentity);
        if (encryptedSecret == null || encryptedSecret.Cipher == null)
        {
            return new SecretClientCollection();
        }

        SecretClientCollection secretCollection = new SecretClientCollection();
        IEnumerable<DecryptionParameter> decryptionParameters = logOnIdentity.TextDecryptionParameters();
        IEnumerable<SecretClientModel> secretClientModels = await GetClientSecretsAsync(decryptionParameters, encryptedSecret);
        if (!secretClientModels.Any())
        {
            return secretCollection;
        }
        secretCollection.AddRange(secretClientModels.Select(s => s));
        return secretCollection;
    }

    public static async Task<SecretClientCollection> GetSharedWithSecretsAsync(LogOnIdentity logOnIdentity, int pageCount)
    {
        SecretClientCollection secretCollection = new SecretClientCollection();
        IEnumerable<DecryptionParameter> decryptionParameters = logOnIdentity.TextDecryptionParameters();

        IEnumerable<ShareSecretApiModel> shareSecretApiModels = await SecretsApiHelper.GetSharedWithSecretsAsync(logOnIdentity, pageCount);
        foreach (ShareSecretApiModel shareSecret in shareSecretApiModels)
        {
            EncryptedSecretApiModel encryptedSecret = new EncryptedSecretApiModel
            {
                Cipher = shareSecret.EncryptedSecret.GetCipherBytes(),
                UserEmail = logOnIdentity.UserEmail.Address,
                CreatedUtc = shareSecret.CreatedUtc,
            };

            IEnumerable<SecretClientModel> secrets = await GetClientSecretsAsync(decryptionParameters, encryptedSecret);
            SecretClientModel sharedSecret = secrets.FirstOrDefault()!;
            if (sharedSecret != null)
            {
                IEnumerable<SecretSharedUser> secretSharedUsers = shareSecret.SharedWith.Select(sw => new SecretSharedUser(EmailAddress.Parse(sw.UserEmail), (SecretShareVisibility)Enum.Parse(typeof(SecretShareVisibility), sw.VisibilityType)));
                sharedSecret.Share = new ShareSecret(secretSharedUsers, shareSecret.OwnerEmail, shareSecret.CreatedUtc);
                secretCollection.Add(sharedSecret);
            }
        }

        return secretCollection;
    }

    private static async Task<IEnumerable<SecretClientModel>> GetClientSecretsAsync(IEnumerable<DecryptionParameter> decryptionParameters, EncryptedSecretApiModel encryptedSecret)
    {
        string decryptedText = await TextEncryption.DecryptAsync(decryptionParameters, encryptedSecret.Cipher);
        SecretsClientModel secretsList = Serializer.Deserialize<SecretsClientModel>(decryptedText);
        if (secretsList == null)
        {
            return new SecretClientCollection();
        }

        return secretsList.Secrets;
    }


    public static async Task<bool> Update(IEnumerable<SecretClientModel> secrets)
    {
        LogOnIdentity logOnIdentity = New<AxCrypt.Core.UI.KnownIdentities>().DefaultEncryptionIdentity;

        SecretClientCollection secretsList = await GetSecrets(logOnIdentity);
        foreach (SecretClientModel secret in secrets)
        {
            int index = secretsList.IndexOf(secret);

            if ((secret.Type <= AxCrypt.Api.Model.Secret.SecretType.Password && secret.Password.IsEmpty) || (secret.Type == AxCrypt.Api.Model.Secret.SecretType.Card && secret.Card.IsEmpty) || (secret.Type == AxCrypt.Api.Model.Secret.SecretType.Note && secret.Note.IsEmpty))
            {
                secretsList.RemoveAt(index);
            }
            else
            {
                secretsList[index] = secret;
                secretsList[index].UpdatedUtc = New<Abstractions.INow>().Utc;
            }
        }

        return await EncryptSaveSecretsAsync(logOnIdentity, secretsList);
    }

    public static async Task<bool> Delete(IEnumerable<SecretClientModel> secrets)
    {
        LogOnIdentity logOnIdentity = New<AxCrypt.Core.UI.KnownIdentities>().DefaultEncryptionIdentity;
        SecretClientCollection secretsList = await GetSecrets(logOnIdentity);
        foreach (SecretClientModel secret in secrets)
        {
            secretsList.Remove(secret);
        }

        return await EncryptSaveSecretsAsync(logOnIdentity, secretsList);
    }

    private static ICollection<SecretClientModel> FilterEmptySecrets(IEnumerable<SecretClientModel> secrets)
    {
        ICollection<SecretClientModel> nonEmptySecrets = new List<SecretClientModel>();
        foreach (SecretClientModel secret in secrets)
        {
            if ((secret.Type == AxCrypt.Api.Model.Secret.SecretType.Legacy || secret.Type == AxCrypt.Api.Model.Secret.SecretType.Password) && !secret.Password.IsEmpty)
            {
                nonEmptySecrets.Add(secret);
            }

            if ((secret.Type == AxCrypt.Api.Model.Secret.SecretType.Card) && !secret.Card.IsEmpty)
            {
                nonEmptySecrets.Add(secret);
            }

            if ((secret.Type == AxCrypt.Api.Model.Secret.SecretType.Note) && !secret.Note.IsEmpty)
            {
                nonEmptySecrets.Add(secret);
            }
        }
        return nonEmptySecrets;
    }

    public static Secret GetSecret(SecretClientModel model, EncryptionKey key)
    {
        if (model == null)
        {
            return null;
        }

        Secret secret;
        switch (model.Type)
        {
            case SecretType.Legacy:
            case SecretType.Password:
                secret = new Secret(model.Id, model.Password, key, model.CreatedUtc, model.UpdatedUtc, model.DeletedUtc);
                break;

            case SecretType.Card:
                secret = new Secret(model.Id, model.Card, key, model.CreatedUtc, model.UpdatedUtc, model.DeletedUtc);
                break;

            case SecretType.Note:
                secret = new Secret(model.Id, model.Note, key, model.CreatedUtc, model.UpdatedUtc, model.DeletedUtc);
                break;

            default:
                return null;
        }

        secret.DBId = model.DBId;

        return new InternalSecret(secret);
    }

    private static async Task<EncryptionParameters> CreateEncryptionParameters(LogOnIdentity identity, IEnumerable<UserPublicKey> sharedKeyHolders = null)
    {
        Guid cryptoId = Resolve.CryptoFactory.Default(New<ICryptoPolicy>()).CryptoId;
        EncryptionParameters encryptionParameters = new EncryptionParameters(cryptoId, identity);
        if (sharedKeyHolders != null)
        {
            await AddSharingParameters(encryptionParameters, sharedKeyHolders);
        }

        return encryptionParameters;
    }

    private static async Task AddSharingParameters(EncryptionParameters parameters, IEnumerable<UserPublicKey> sharedKeyHolders)
    {
        if (sharedKeyHolders == null || !sharedKeyHolders.Any())
        {
            return;
        }

        await parameters.AddAsync(sharedKeyHolders);
    }

    private static IStringSerializer Serializer
    {
        get
        {
            return New<IStringSerializer>();
        }
    }
}