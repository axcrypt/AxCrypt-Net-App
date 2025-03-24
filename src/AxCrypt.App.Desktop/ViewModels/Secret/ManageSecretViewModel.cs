using AxCrypt.Api.Model.Secret;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Models.Secret;
using AxCrypt.Core.Runtime;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Desktop.ViewModels.Secret;

public class ManageSecretViewModel : Core.UI.ViewModel.ViewModelBase
{
    public ManageSecretViewModel(SecretService secretService)
    {
        Secret = secretService.CurrentSecret!;

        HasPaidSubscription = New<AccountStatusViewModel>().PlanState == PlanState.HasPasswordManager || New<AccountStatusViewModel>().PlanState == PlanState.HasPremium || New<AccountStatusViewModel>().PlanState == PlanState.HasBusiness;
        HasBusinessSubscription = New<AccountStatusViewModel>().PlanState == PlanState.HasBusiness;
        ClearErrorFileds();
    }

    public SecretViewModel Secret { get { return GetProperty<SecretViewModel>(nameof(Secret)); } set { SetProperty(nameof(Secret), value); } }

    public string ErrorMessage
    {
        get
        {
            return GetProperty<string>(nameof(ErrorMessage));
        }
        set
        {
            SetProperty(nameof(ErrorMessage), value);
            if (value != "")
                CanShowErrorMessage = true;
            else
                CanShowErrorMessage = false;
        }
    }

    public bool CanShowErrorMessage
    {
        get { return GetProperty<bool>(nameof(CanShowErrorMessage)); }
        set { SetProperty(nameof(CanShowErrorMessage), value); }
    }

    public bool HasPaidSubscription
    {
        get { return GetProperty<bool>(nameof(HasPaidSubscription)); }
        set { SetProperty(nameof(HasPaidSubscription), value); }
    }
    
    public bool HasBusinessSubscription
    {
        get { return GetProperty<bool>(nameof(HasBusinessSubscription)); }
        set { SetProperty(nameof(HasBusinessSubscription), value); }
    }

    public string PageTitle
    { get { return GetProperty<string>(nameof(PageTitle)); } internal set { SetProperty(nameof(PageTitle), value); } }

    public bool ShowPasswordLayout
    { get { return GetProperty<bool>(nameof(ShowPasswordLayout)); } internal set { SetProperty(nameof(ShowPasswordLayout), value); } }

    public bool ShowCardLayout
    { get { return GetProperty<bool>(nameof(ShowCardLayout)); } internal set { SetProperty(nameof(ShowCardLayout), value); } }

    public bool ShowNoteLayout
    { get { return GetProperty<bool>(nameof(ShowNoteLayout)); } internal set { SetProperty(nameof(ShowNoteLayout), value); } }

    public void ShowSecretByType(SecretType secretType = 0)
    {
        switch (secretType)
        {
            case SecretType.Legacy:
            case SecretType.Password:
                ShowPasswordLayout = true;
                ShowNoteLayout = false;
                ShowCardLayout = false;
                break;

            case SecretType.Card:
                ShowPasswordLayout = false;
                ShowCardLayout = true;
                ShowNoteLayout = false;
                break;

            case SecretType.Note:
                ShowPasswordLayout = false;
                ShowCardLayout = false;
                ShowNoteLayout = true;
                break;

            default:
                ShowPasswordLayout = false;
                ShowNoteLayout = false;
                ShowCardLayout = false;
                break;
        }
    }

    public bool ValidModel()
    {
        switch (Secret.SecretType)
        {
            case Api.Model.Secret.SecretType.Legacy:
            case Api.Model.Secret.SecretType.Password:
                return ValidPasswordModel();

            case Api.Model.Secret.SecretType.Card:
                return ValidCardModel();

            case Api.Model.Secret.SecretType.Note:
                return ValidNoteModel();
        }

        return false;
    }

    private bool ValidNoteModel()
    {
        if (string.IsNullOrEmpty(Secret.Note.Note) || string.IsNullOrEmpty(Secret.Note.SecretDesc))
        {
            ErrorMessage = "Fill all the required(marked *) fields!";
            return false;
        }

        return true;
    }

    private bool ValidCardModel()
    {
        if (string.IsNullOrEmpty(Secret.Card.CardNumber) || string.IsNullOrEmpty(Secret.Card.SecretDesc) || string.IsNullOrEmpty(Secret.Card.ExpirationDate) || string.IsNullOrEmpty(Secret.Card.NameOnCard) || string.IsNullOrEmpty(Secret.Card.SecurityCode))
        {
            ErrorMessage = "Fill all the required(marked *) fields!";
            return false;
        }

        return true;
    }

    private bool ValidPasswordModel()
    {
        if (string.IsNullOrEmpty(Secret.Password.Url) || string.IsNullOrEmpty(Secret.Password.SecretDesc) || string.IsNullOrEmpty(Secret.Password.Username) || string.IsNullOrEmpty(Secret.Password.SecretValue))
        {
            ErrorMessage = "Fill all the required(marked *) fields!";
            return false;
        }

        return true;
    }

    private void ClearErrorFileds()
    {
        CanShowErrorMessage = false;
        ErrorMessage = "";
    }
}