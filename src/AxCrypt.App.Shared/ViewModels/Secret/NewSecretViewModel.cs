using AxCrypt.Abstractions;
using AxCrypt.Api.Model.Secret;
using AxCrypt.App.Shared.Data;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Models.Secret;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.Common;
using AxCrypt.Content;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.ViewModels.Secret;

public class NewSecretViewModel : ManageSecretViewModel
{
    public NewSecretViewModel(SecretService secretService) : base(secretService)
    {
        Secret = Initialize(secretService.SecretType);

        switch (secretService.SecretType)
        {
            case SecretType.Legacy:
            case SecretType.Password:
                PageTitle = Texts.AddPasswordTitle;
                Secret.Password = new SecretPasswordViewModel("", "", "", "", "");
                break;

            case SecretType.Card:
                PageTitle = Texts.AddCardTitle;
                Secret.Card = new SecretCardViewModel("", "", "", "", "");
                break;

            case SecretType.Note:
                PageTitle = Texts.AddNoteTitle;
                Secret.Note = new SecretNoteViewModel("", "");
                break;

            default:
                break;
        }

        base.ShowSecretByType(secretService.SecretType);
    }

    public bool? ShowSuggestPasswordLoadingIcon { get; set; }

    public async Task<bool> SaveSecretAsync()
    {
        if (New<AxCryptOnlineState>().IsOffline)
        {
            ErrorMessage = Texts.NoInternetErrorMessage;
            return false;
        }

        if (Secret == null)
        {
            return false;
        }

        if (!ValidModel())
        {
            return false;
        }

        using (ProcessIndicator processIndicator = new ProcessIndicator())
        {
            SecretClientModel newSecret = Secret.ToClientModel(Guid.NewGuid());
            if (newSecret == null)
            {
                ErrorMessage = string.Format(Texts.FileOperationFailed, $"to create {Secret.SecretType.ToString()}");
                return false;
            }

            newSecret.Type = Secret.SecretType;
            newSecret.CreatedUtc = New<INow>().Utc;
            newSecret.UpdatedUtc = New<INow>().Utc;
            bool created = await PersonalSecrets.InsertAsync(newSecret);

            return created;
        }
    }

    #region Manage View Model

    public SecretViewModel Initialize(SecretType type)
    {
        return new SecretViewModel(type, SecretPasswordViewModel.Empty, SecretCardViewModel.Empty, SecretNoteViewModel.Empty, new List<SecretSharedUserViewModel>());
    }

    public async Task SuggestPasswordAsync()
    {
        ShowSuggestPasswordLoadingIcon = true;
        Secret.Password.SecretValue = await SecretsApiHelper.SuggestPasswordAsync();
        ShowSuggestPasswordLoadingIcon = false;
    }

    #endregion Manage View Model
}