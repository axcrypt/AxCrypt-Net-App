using AxCrypt.Abstractions;
using AxCrypt.Api.Model.Secret;
using AxCrypt.Api.Shared.Helper;
using AxCrypt.App.Desktop.Helpers;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Models.Secret;
using AxCrypt.App.Shared.Password;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Secrets;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Cryptor.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Desktop.Data;

/// <summary>
/// Gateway class to use as a ObjectDataSource
/// </summary>
public static class PersonalSecrets
{
    private static async Task<SecretClientCollection> SelectBySearchInternal(string search)
    {
        SecretClientCollection allSecrets = await LoadActiveSecretsAsync();
        if (String.IsNullOrEmpty(search))
        {
            return allSecrets;
        }
        return SearchSecretsInternal(search, allSecrets);
    }

    private static SecretClientCollection SearchSecretsInternal(string search, SecretClientCollection allSecrets)
    {
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

    public static async Task<SecretClientCollection> SelectBySearch(string search)
    {
        SecretClientCollection secrets = await SelectBySearchInternal(search);
        return secrets;
    }

    public static SecretClientCollection SearchInSecrets(IEnumerable<SecretClientModel> secrets, string search)
    {
        SecretClientCollection allSecrets = new SecretClientCollection();
        allSecrets.AddRange(secrets);
        return SearchSecretsInternal(search, allSecrets);
    }

    public static async Task<SecretClientCollection> SelectById(Guid id)
    {
        SecretClientCollection secrets = new SecretClientCollection();
        if (id == Guid.Empty)
        {
            return secrets;
        }
        SecretClientCollection allSecrets = await LoadActiveSecretsAsync();
        secrets.OriginalCount = allSecrets.OriginalCount;
        if (!allSecrets.Contains(id))
        {
            return secrets;
        }
        SecretClientModel theSecret = allSecrets[id];
        secrets.Add(theSecret);
        //if (AppSwitch.Instance.TraceInfo)
        //{
        //    AppSwitch.Instance.Information("Searched for and found secret with id {0} for user {1}", theSecret.Id.ToString("D", CultureInfo.InvariantCulture), UserContext.Name);
        //}
        return secrets;
    }

    private static async Task<SecretClientCollection> LoadActiveSecretsAsync()
    {
        if (string.IsNullOrEmpty(New<KnownIdentities>().DefaultEncryptionIdentity.UserEmail.Address))
        {
            new SecretCollection();
        }

        return await SecretsApiHelper.GetSecrets(New<KnownIdentities>().DefaultEncryptionIdentity);
    }

    public static async Task<bool> InsertAsync(SecretClientModel secret)
    {
        if (!ViewModelHelper.CanAddNewSecret())
        {
            return false;
        }
        await SecretsApiHelper.Insert(new List<SecretClientModel> { secret });
        if (ViewModelHelper.CanUpdateFreeUserNewSecretCount())
        {
            New<LogOnIdentity, AdditionalUserSettings>(New<KnownIdentities>().DefaultEncryptionIdentity).UpdateFreeUserSecretsCount();
        }
        return true;
    }

    public static async Task<bool> ShareAsync(SecretClientModel secret)
    {
        return await SecretsApiHelper.Share(secret);
    }

    public static async Task<bool> UpdateAsync(SecretClientModel secret)
    {
        return await SecretsApiHelper.Update(new List<SecretClientModel> { secret });
    }

    public static async Task<bool> DeleteAsync(SecretClientModel secret)
    {
        bool deleted = await SecretsApiHelper.Delete(new List<SecretClientModel> { secret });
        bool deletedshare = await ShareSecretFacade.DeleteSecretsSharedAsync(secret);
        return deleted || deletedshare;
    }

    public static async Task<SecretClientCollection> SelectSharedSecretById(Guid id)
    {
        SecretClientCollection secrets = new SecretClientCollection();
        if (id == Guid.Empty)
        {
            return secrets;
        }
        SecretClientCollection allSecrets = await LoadActiveSharedWithSecretsAsync();
        secrets.OriginalCount = allSecrets.OriginalCount;
        if (!allSecrets.Contains(id))
        {
            return secrets;
        }
        SecretClientModel theSecret = allSecrets[id];
        secrets.Add(theSecret);
        //if (AppSwitch.Instance.TraceInfo)
        //{
        //    AppSwitch.Instance.Information("Searched for and found secret with id {0} for user {1}", theSecret.Id.ToString("D", CultureInfo.InvariantCulture), UserContext.Name);
        //}
        return secrets;
    }

    private static async Task<SecretClientCollection> LoadActiveSharedWithSecretsAsync()
    {
        if (string.IsNullOrEmpty(New<KnownIdentities>().DefaultEncryptionIdentity.UserEmail.Address))
        {
            new SecretCollection();
        }

        return await SecretsApiHelper.GetSharedWithSecretsAsync(New<KnownIdentities>().DefaultEncryptionIdentity, 20);
    }

    public static SecretClientModel ToClientModel(this SecretViewModel secret, Guid guid)
    {
        SecretClientModel secretClientmodel;
        switch (secret.SecretType)
        {
            case SecretType.Legacy:
            case SecretType.Password:
                AxCrypt.Core.Secrets.SecretPassword secretPassword = new AxCrypt.Core.Secrets.SecretPassword(secret.Password.Title, secret.Password.Url, secret.Password.SecretDesc!, secret.Password.Username, secret.Password.SecretValue);
                secretClientmodel = new SecretClientModel(guid, secretPassword);
                break;

            case SecretType.Card:
                AxCrypt.Core.Secrets.SecretCard secretCard = new AxCrypt.Core.Secrets.SecretCard(secret.Card.CardNumber, secret.Card.SecretDesc!, secret.Card.NameOnCard, secret.Card.SecurityCode, secret.Card.ExpirationDate);
                secretClientmodel = new SecretClientModel(guid, secretCard);
                break;

            case SecretType.Note:
                AxCrypt.Core.Secrets.SecretNote secretNote = new AxCrypt.Core.Secrets.SecretNote(secret.Note.SecretDesc!, secret.Note.Note);
                secretClientmodel = new SecretClientModel(guid, secretNote);
                break;

            default:
                return null;
        }
        secretClientmodel.DBId = secret.DBId;
        secretClientmodel.CreatedUtc = secret.CreatedUtc;
        secretClientmodel.Type = secret.SecretType;
        IEnumerable<SecretSharedUser> secretSharedUsers = secret.SharedWith.Select(sw => new SecretSharedUser(sw.UserEmail, sw.Visibility));
        secretClientmodel.Share = new ShareSecret(secretSharedUsers, secret.OwnerEmail, New<INow>().Utc);
        return secretClientmodel;
    }
}