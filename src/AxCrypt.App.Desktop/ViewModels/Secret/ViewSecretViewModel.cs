using AxCrypt.Abstractions;
using AxCrypt.App.Desktop.Data;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Models.Secret;
using AxCrypt.App.Shared.Password;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.Secrets;
using AxCrypt.Core.Service.Secrets;
using AxCrypt.Core.UI;
using AxCrypt.Cryptor.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Desktop.ViewModels.Secret;

public class ViewSecretViewModel : ManageSecretViewModel
{
    public ViewSecretViewModel(SecretService secretService) : base(secretService)
    {
        switch (Secret.SecretType)
        {
            case Api.Model.Secret.SecretType.Legacy:
            case Api.Model.Secret.SecretType.Password:
                PageTitle = Texts.ViewPasswordTitle;
                break;

            case Api.Model.Secret.SecretType.Card:
                PageTitle = Texts.ViewCardTitle;
                break;

            case Api.Model.Secret.SecretType.Note:
                PageTitle = Texts.ViewNoteTitle;
                break;

            default:
                break;
        }

        base.ShowSecretByType(Secret.SecretType);
    }

    public string? UserEmail { get; set; }

    public string GetValue(bool isVisible, string value)
    {
        return isVisible ? value : "**click to show**";
    }

    public async Task<bool> TryDeleteSecretAsync()
    {
        if (Secret is null)
        {
            return false;
        }
        return Secret.OwnerEmail != UserEmail ? await DeleteSharedWithMeSecret(Secret) : await DeleteSecretAsync();
    }

    public async Task UpdateSecretByVisibility()
    {
        SecretClientModel? secretClientModel = Secret.ToClientModel(Secret.SecretGuid);
        if (secretClientModel == null || secretClientModel.Share == null)
        {
            return;
        }
        if (secretClientModel.Share.OwnerEmail == New<UserSettings>().UserEmail)
        {
            return;
        }

        SecretSharedUser sharedUser = secretClientModel.Share.SharedWith.FirstOrDefault(sw => sw.UserEmail.Address == New<UserSettings>().UserEmail)!;
        if (sharedUser.VisibilityType != SecretShareVisibility.Once)
        {
            return;
        }

        sharedUser.Visibility = New<INow>().Utc;
        secretClientModel.Share.SharedWith = new List<SecretSharedUser> { sharedUser };
        await SharedSecrets.UpdateShareVisibility(secretClientModel);
    }

    public async Task<bool> DeleteSecretAsync()
    {
        if (New<AxCryptOnlineState>().IsOffline)
        {
            ErrorMessage = Texts.NoInternetErrorMessage;
            return false;
        }

        if (!HasPaidSubscription)
        {
            ErrorMessage = Texts.SaveSecretErrorIsReadOnly;
            return false;
        }

        using (ProcessIndicator processIndicator = new ProcessIndicator())
        {
            if (Secret == null)
            {
                return false;
            }

            SecretClientCollection secrets = await PersonalSecrets.SelectById(Secret.SecretGuid);
            if (secrets == null || !secrets.Any())
            {
                ErrorMessage = Texts.DeleteSecretErrorNotFound;
                return false;
            }
            bool deleted = await PersonalSecrets.DeleteAsync(secrets[0]);
            return deleted;
        }
    }

    public async Task<bool> DeleteSharedWithMeSecret(SecretViewModel secret)
    {
        if (New<AxCryptOnlineState>().IsOffline)
        {
            ErrorMessage = Texts.NoInternetErrorMessage;
            return false;
        }

        if (!HasPaidSubscription)
        {
            ErrorMessage = Texts.SaveSecretErrorIsReadOnly;
            return false;
        }

        using (ProcessIndicator processIndicator = new ProcessIndicator())
        {
            if (secret == null)
            {
                return false;
            }

            SecretClientCollection secrets = await PersonalSecrets.SelectSharedSecretById(Secret.SecretGuid);
            if (secrets == null || !secrets.Any())
            {
                ErrorMessage = Texts.DeleteSecretErrorNotFound;
                return false;
            }

            bool isDeleted = await SharedSecrets.DeleteSharedWithAsync(secrets[0]);
            return isDeleted;
        }
    }
}