using AxCrypt.App.Shared.Facades;
using AxCrypt.App.Shared.Models.Secret;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Password;

/// <summary>
/// Gateway class to use as a ObjectDataSource
/// </summary>
public static class SharedSecrets
{
    public static async Task<SecretClientCollection> SelectBySearch(string search)
    {
        SecretClientCollection secrets = await SelectBySearchInternal(search);
        return secrets;
    }

    private static async Task<SecretClientCollection> SelectBySearchInternal(string search)
    {
        SecretClientCollection allSecrets = await LoadActiveSharedWithSecretsAsync();
        if (String.IsNullOrEmpty(search))
        {
            return allSecrets;
        }
        SecretClientCollection secrets = new SecretClientCollection();
        secrets.OriginalCount = allSecrets.OriginalCount;
        foreach (SecretClientModel secret in allSecrets)
        {
            if (secret.Type == AxCrypt.Api.Model.Secret.SecretType.Legacy || secret.Type == AxCrypt.Api.Model.Secret.SecretType.Password)
            {
                SearchInPasswords(search, secrets, secret);
                continue;
            }
            if (secret.Type == AxCrypt.Api.Model.Secret.SecretType.Card)
            {
                SearchInCards(search, secrets, secret);
                continue;
            }
            if (secret.Type == AxCrypt.Api.Model.Secret.SecretType.Note)
            {
                SearchInNotes(search, secrets, secret);
                continue;
            }
        }
        return secrets;
    }

    private static async Task<SecretClientCollection> LoadActiveSharedWithSecretsAsync(int pageCount = 20)
    {
        return await SecretsFacade.GetSharedWithSecretsAsync(New<AxCrypt.Core.UI.KnownIdentities>().DefaultEncryptionIdentity, pageCount);
    }

    private static void SearchInPasswords(string search, SecretClientCollection secrets, SecretClientModel secret)
    {
        if (secret.Password.Title.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0)
        {
            secrets.Add(secret);
            return;
        }
        if (secret.Password.Url.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0)
        {
            secrets.Add(secret);
            return;
        }
        if (secret.Password.Description.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0)
        {
            secrets.Add(secret);
            return;
        }
        if (secret.Password.Username.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0)
        {
            secrets.Add(secret);
            return;
        }
        if (secret.Password.TheSecret.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0)
        {
            secrets.Add(secret);
            return;
        }
    }

    private static void SearchInCards(string search, SecretClientCollection secrets, SecretClientModel secret)
    {
        if (secret.Card.Number.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0)
        {
            secrets.Add(secret);
            return;
        }
        if (secret.Card.Description.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0)
        {
            secrets.Add(secret);
            return;
        }
        if (secret.Card.NameOnCard.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0)
        {
            secrets.Add(secret);
            return;
        }
        if (secret.Card.SecurityCode.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0)
        {
            secrets.Add(secret);
            return;
        }
        if (secret.Card.ExpirationDate.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0)
        {
            secrets.Add(secret);
            return;
        }
    }

    private static void SearchInNotes(string search, SecretClientCollection secrets, SecretClientModel secret)
    {
        if (secret.Note.Description.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0)
        {
            secrets.Add(secret);
            return;
        }
        if (secret.Note.Note.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0)
        {
            secrets.Add(secret);
            return;
        }
    }

    public static async Task<bool> UpdateShareVisibility(SecretClientModel secret)
    {
        return await ShareSecretFacade.UpdateSharedVisibilityAsync(secret);
    }

    public static async Task<bool> DeleteSharedWithAsync(SecretClientModel secret)
    {
        return await ShareSecretFacade.DeleteSharedWithAsync(secret);
    }
}