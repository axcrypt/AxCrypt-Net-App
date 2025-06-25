using AxCrypt.Abstractions;
using AxCrypt.Api.Extension;
using AxCrypt.Api.Model;
using AxCrypt.Api.Model.Secret;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Secrets;
using AxCrypt.Core.Service.Secrets;
using AxCrypt.Core.UI;
using AxCrypt.Cryptor;
using AxCrypt.Cryptor.Model;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Facades;

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
        SecretsClientModel secretsClientModel = new SecretsClientModel()
        {
            Secrets = new List<SecretClientModel> { secret },
        };

        EncryptionParameters encryptionParameters = await CreateEncryptionParameters(identity, usersPublicKeys);
        string serializedText = Serializer.Serialize(secretsClientModel);
        byte[] encryptedSecrets = await TextEncryption.EncryptAsync(encryptionParameters, serializedText);

        string userEmail = identity.UserEmail.Address;
        EncryptedSecretApiModel encryptedSecret = new EncryptedSecretApiModel()
        {
            UserEmail = userEmail,
            Cipher = encryptedSecrets,
            CreatedUtc = New<INow>().Utc
        };
        return encryptedSecret;
    }

    private static async Task ShareSecretAsync(SecretClientModel secret, IList<SecretSharedUser> sharedWithUsers, EncryptedSecretApiModel encryptedData)
    {
        string encryptedSecret = "";
        if (encryptedData.Cipher != null)
        {
            encryptedSecret = encryptedData.Cipher.GetCipherString();
        }
        LogOnIdentity logOnIdentity = New<AxCrypt.Core.UI.KnownIdentities>().DefaultEncryptionIdentity;

        IEnumerable<SharedUserApiModel> sharedUserApiModelList = sharedWithUsers.Select(us => new SharedUserApiModel(us.UserEmail.Address, us.VisibilityType.ToString(), us.Visibility));
        ShareSecretApiModel shareSecretModel = new ShareSecretApiModel(0, logOnIdentity.UserEmail.Address, secret.Id, encryptedSecret, sharedUserApiModelList, New<INow>().Utc, New<INow>().Utc, null);

        await New<LogOnIdentity, ISecretsService>(logOnIdentity).ShareSecretsAsync(shareSecretModel);
    }

    public static async Task<bool> UpdateSharedVisibilityAsync(SecretClientModel secret, LogOnIdentity identity = null!)
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