using AxCrypt.Abstractions;
using AxCrypt.Api.Model.Secret;
using AxCrypt.App.Desktop.Data;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Password;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.Common;
using AxCrypt.Content;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Desktop.ViewModels.Secret;

public class EditSecretViewModel : ManageSecretViewModel
{
    public EditSecretViewModel(SecretService secretService) : base(secretService)
    {
        switch (Secret.SecretType)
        {
            case Api.Model.Secret.SecretType.Legacy:
            case Api.Model.Secret.SecretType.Password:
                PageTitle = Texts.EditPasswordTitle;
                break;

            case Api.Model.Secret.SecretType.Card:
                PageTitle = Texts.EditCardTitle;
                break;

            case Api.Model.Secret.SecretType.Note:
                PageTitle = Texts.EditNoteTitle;
                break;

            default:
                break;
        }

        base.ShowSecretByType(Secret.SecretType);
    }

    public async Task<bool> SaveSecretAsync()
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

            bool updated = false;

            updated = await UpdateSecretAsync();

            if (!updated)
            {
                return false;
            }
            return true;
        }
    }

    private async Task<bool> UpdateSecretAsync()
    {
        SecretClientCollection secrets = await PersonalSecrets.SelectById(Secret.SecretGuid);
        if (secrets == null || secrets.Count == 0)
        {
            ErrorMessage = Texts.EditSecretErrorNotFound;
            return false;
        }

        if (!ValidModel())
        {
            return false;
        }

        // If a secret was found the user does have permission to update it
        switch (Secret.SecretType)
        {
            case SecretType.Legacy:
            case SecretType.Password:
                secrets[0].Password.Title = Secret.Password.Title;
                secrets[0].Password.Url = Secret.Password.Url;
                secrets[0].Password.Description = Secret.Password.SecretDesc;
                secrets[0].Password.Username = Secret.Password.Username;
                secrets[0].Password.TheSecret = Secret.Password.SecretValue;
                break;

            case SecretType.Card:
                secrets[0].Card.Number = Secret.Card.CardNumber;
                secrets[0].Card.Description = Secret.Card.SecretDesc;
                secrets[0].Card.NameOnCard = Secret.Card.NameOnCard;
                secrets[0].Card.SecurityCode = Secret.Card.SecurityCode.ToString();
                secrets[0].Card.ExpirationDate = Secret.Card.ExpirationDate;
                break;

            case SecretType.Note:
                secrets[0].Note.Description = Secret.Note.SecretDesc;
                secrets[0].Note.Note = Secret.Note.Note;
                break;
        }
        secrets[0].UpdatedUtc = New<INow>().Utc;

        await PersonalSecrets.UpdateAsync(secrets[0]);
        if (secrets[0].Share != null)
        {
            await PersonalSecrets.ShareAsync(secrets[0]);
        }
        return true;
    }

    //public bool ValidModel()
    //{
    //    switch (Secret.SecretType)
    //    {
    //        case Api.Model.Secret.SecretType.Legacy:
    //        case Api.Model.Secret.SecretType.Password:
    //            return ValidPasswordModel();

    //        case Api.Model.Secret.SecretType.Card:
    //            return ValidCardModel();

    //        case Api.Model.Secret.SecretType.Note:
    //            return ValidNoteModel();
    //    }
    //    return false;
    //}

    //private bool ValidNoteModel()
    //{
    //    if (string.IsNullOrEmpty(Secret.Note.SecretDesc))
    //    {
    //        ErrorMessage = "Fill all the required(marked *) fields!";
    //        return false;
    //    }
    //    return true;
    //}

    //private bool ValidCardModel()
    //{
    //    if (string.IsNullOrEmpty(Secret.Card.CardNumber) || string.IsNullOrEmpty(Secret.Card.SecretDesc) || string.IsNullOrEmpty(Secret.Card.ExpirationDate) || string.IsNullOrEmpty(Secret.Card.NameOnCard) || string.IsNullOrEmpty(Secret.Card.SecurityCode))
    //    {
    //        ErrorMessage = "Fill all the required(marked *) fields!";
    //        return false;
    //    }
    //    return true;
    //}

    //private bool ValidPasswordModel()
    //{
    //    if (string.IsNullOrEmpty(Secret.Password.SecretDesc) || string.IsNullOrEmpty(Secret.Password.SecretValue))
    //    {
    //        ErrorMessage = "Fill all the required(marked *) fields!";
    //        return false;
    //    }
    //    return true;
    //}
}