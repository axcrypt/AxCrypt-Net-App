using AxCrypt.Api;
using AxCrypt.Api.Model;
using AxCrypt.Api.Model.Secret;
using AxCrypt.Api.Shared.Helper;
using AxCrypt.App.Shared.Password;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Secrets;
using AxCrypt.Core.Service.Secrets;
using AxCrypt.Core.UI;
using AxCrypt.Cryptor;
using AxCrypt.Cryptor.Model;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Helpers;

public static class SecretsApiHelper
{
    public static async Task<SecretClientCollection> GetSecrets(LogOnIdentity logOnIdentity)
    {
        SecretClientCollection secretCollection = new SecretClientCollection();
        LogOnIdentity identity = New<AxCrypt.Core.UI.KnownIdentities>().DefaultEncryptionIdentity;
        SecretsListRequestOptions requestOptions = new SecretsListRequestOptions(identity.UserEmail.Address);
        EncryptedSecretApiModel userSecrets = await New<LogOnIdentity, ISecretsService>(identity).GetSecretsAsync(requestOptions);
        if (userSecrets.Cipher == null)
        {
            return secretCollection;
        }
        IEnumerable<SecretClientModel> secrets = TextCryptor.GetClientSecrets(identity, userSecrets);
        if (!secrets.Any())
        {
            return secretCollection;
        }
        secretCollection.AddRange(secrets.Select(s => s));
        return secretCollection;
    }

    /// <summary>
    /// Inserts the specified key collection.
    /// </summary>
    /// <param name="secrets">The secrets.</param>
    public static async Task<bool> Insert(IEnumerable<SecretClientModel> secrets)
    {
        if (secrets == null)
        {
            throw new ArgumentNullException("secrets");
        }

        ICollection<SecretClientModel> nonEmptySecrets = FilterEmptySecrets(secrets);
        if (nonEmptySecrets.Count == 0)
        {
            return false;
        }

        SecretClientCollection newSecrets = await GetSecrets(New<AxCrypt.Core.UI.KnownIdentities>().DefaultEncryptionIdentity);
        newSecrets.AddRange(nonEmptySecrets);

        return await InternalSave(newSecrets);

        //// Log each new secret insertion for now. This may be too much in the future.
        //foreach (Secret secret in nonEmptySecrets)
        //{
        //    string m = String.Format(CultureInfo.InvariantCulture, "Inserted secret {0} for user {1}.", secret.Id.ToString(), username);
        //    new XecretsRequestInfoEvent(m, this, (int)XecretsEventCode.SecretInserted).Raise();
        //}
    }

    public static async Task<SecretClientCollection> GetSharedWithSecretsAsync(LogOnIdentity logOnIdentity, int pageCount)
    {
        SecretClientCollection secretCollection = new SecretClientCollection();
        SecretsListRequestOptions requestOptions = new SecretsListRequestOptions(logOnIdentity.UserEmail.Address);
        requestOptions.PageCount = pageCount;

        IEnumerable<ShareSecretApiModel> shareSecretApiModels = await New<LogOnIdentity, ISecretsService>(logOnIdentity).GetSharedWithSecretsAsync(requestOptions);

        foreach (ShareSecretApiModel shareSecret in shareSecretApiModels)
        {
            EncryptedSecretApiModel encryptedSecret = new EncryptedSecretApiModel
            {
                Cipher = shareSecret.EncryptedSecret.GetCipherString(),
                UserEmail = logOnIdentity.UserEmail.Address,
                CreatedUtc = shareSecret.CreatedUtc,
            };

            IEnumerable<SecretClientModel> secrets = TextCryptor.GetClientSecrets(logOnIdentity, encryptedSecret);
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

    public static byte[] GetCipherString(this string cipher)
    {
        return Convert.FromBase64String(cipher);
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

        return true;
        //// Log each new secret insertion for now. This may be too much in the future.
        //foreach (Secret secret in nonEmptySecrets)
        //{
        //    string m = String.Format(CultureInfo.InvariantCulture, "Inserted secret {0} for user {1}.", secret.Id.ToString(), username);
        //    new XecretsRequestInfoEvent(m, this, (int)XecretsEventCode.SecretInserted).Raise();
        //}
    }

    /// <summary>
    /// Filters the empty secrets.
    /// </summary>
    /// <param name="secrets">The secrets.</param>
    /// <returns></returns>
    private static ICollection<SecretClientModel> FilterEmptySecrets(IEnumerable<SecretClientModel> secrets)
    {
        List<SecretClientModel> nonEmptySecrets = new List<SecretClientModel>();
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

    public static async Task<bool> Update(IEnumerable<SecretClientModel> secrets)
    {
        SecretClientCollection newSecrets = await GetSecrets(New<AxCrypt.Core.UI.KnownIdentities>().DefaultEncryptionIdentity);
        foreach (SecretClientModel secret in secrets)
        {
            int index = newSecrets.IndexOf(secret);

            if ((secret.Type <= AxCrypt.Api.Model.Secret.SecretType.Password && secret.Password.IsEmpty) || (secret.Type == AxCrypt.Api.Model.Secret.SecretType.Card && secret.Card.IsEmpty) || (secret.Type == AxCrypt.Api.Model.Secret.SecretType.Note && secret.Note.IsEmpty))
            {
                newSecrets.RemoveAt(index);
            }
            else
            {
                newSecrets[index] = secret;
                newSecrets[index].UpdatedUtc = New<Abstractions.INow>().Utc;
            }
        }

        return await InternalSave(newSecrets);
    }

    public static async Task<bool> Delete(IEnumerable<SecretClientModel> secrets)
    {
        SecretClientCollection newSecrets = await GetSecrets(New<AxCrypt.Core.UI.KnownIdentities>().DefaultEncryptionIdentity);
        foreach (SecretClientModel secret in secrets)
        {
            newSecrets.Remove(secret);
        }

        return await InternalSave(newSecrets);
    }

    private static async Task<bool> InternalSave(IEnumerable<SecretClientModel> secretList)
    {
        return await Task.Run(async () =>
        {
            SecretsClientModel secretsClientModel = new SecretsClientModel()
            {
                Secrets = secretList.ToList(),
            };
            LogOnIdentity logOnIdentity = New<AxCrypt.Core.UI.KnownIdentities>().DefaultEncryptionIdentity;
            EncryptedSecretApiModel encryptedSecretsModel = await TextCryptor.Encrypt(logOnIdentity, secretsClientModel);
            return await New<LogOnIdentity, ISecretsService>(logOnIdentity).SaveSecretsAsync(encryptedSecretsModel);
        });
    }

    public static async Task<string> SuggestPasswordAsync()
    {
        LogOnIdentity logOnIdentity = New<AxCrypt.Core.UI.KnownIdentities>().DefaultEncryptionIdentity;
        int strongPassword = 3;
        PasswordSuggestion suggestion = await New<LogOnIdentity, ISecretsService>(logOnIdentity).SuggestPasswordAsync(strongPassword);
        return suggestion?.Suggestion ?? "";
    }
}