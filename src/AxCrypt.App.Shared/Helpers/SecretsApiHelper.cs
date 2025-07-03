using AxCrypt.Api;
using AxCrypt.Api.Model;
using AxCrypt.Api.Model.Secret;
using AxCrypt.App.Shared.Facades;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Notification;
using AxCrypt.Core.Service.Secrets;
using AxCrypt.Cryptor.Model;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Helpers;

public static class SecretsApiHelper
{
    public static async Task<EncryptedSecretApiModel> GetSecrets(LogOnIdentity identity)
    {
        SecretsListRequestOptions requestOptions = new SecretsListRequestOptions(identity.UserEmail.Address);
        EncryptedSecretApiModel userSecrets = await New<LogOnIdentity, ISecretsService>(identity).GetSecretsAsync(requestOptions);
        if (userSecrets.Cipher == null)
        {
            return userSecrets;
        }

        return userSecrets;
    }

    /// <summary>
    /// Inserts the specified key collection.
    /// </summary>
    /// <param name="secrets">The secrets.</param>
    public static async Task<bool> Insert(LogOnIdentity logOnIdentity, EncryptedSecretApiModel encryptedSecretsModel)
    {
        if (logOnIdentity is null)
        {
            throw new ArgumentNullException(nameof(logOnIdentity));
        }

        if (encryptedSecretsModel is null)
        {
            throw new ArgumentNullException(nameof(encryptedSecretsModel));
        }

        return await New<LogOnIdentity, ISecretsService>(logOnIdentity).SaveSecretsAsync(encryptedSecretsModel);
    }

    public static async Task<IEnumerable<ShareSecretApiModel>> GetSharedWithSecretsAsync(LogOnIdentity logOnIdentity, int pageCount)
    {
        SecretsListRequestOptions requestOptions = new SecretsListRequestOptions(logOnIdentity.UserEmail.Address);
        requestOptions.PageCount = pageCount;

        return await New<LogOnIdentity, ISecretsService>(logOnIdentity).GetSharedWithSecretsAsync(requestOptions);
    }

    /// <summary>
    /// Share the specified key collection.
    /// </summary>
    /// <param name="secrets">The secrets.</param>
    public static async Task<bool> Share(SecretClientModel shareSecret)
    {
        if (shareSecret == null)
        {
            throw new ArgumentNullException("secrets");
        }

        await ShareSecretFacade.ShareSecret(shareSecret, New<AxCrypt.Core.UI.KnownIdentities>().DefaultEncryptionIdentity);
        await NotificationLogger.PushAsync(shareSecret.Share.OwnerEmail, NotificationType.ShareSecret, "A secret has been shared with you!", shareSecret.Share.SharedWith.Select(ssw => ssw.UserEmail.Address).ToArray(), null);

        return true;
    }

    public static async Task<string> SuggestPasswordAsync()
    {
        LogOnIdentity logOnIdentity = New<AxCrypt.Core.UI.KnownIdentities>().DefaultEncryptionIdentity;
        int strongPassword = 3;
        PasswordSuggestion suggestion = await New<LogOnIdentity, ISecretsService>(logOnIdentity).SuggestPasswordAsync(strongPassword);
        return suggestion?.Suggestion ?? "";
    }
}