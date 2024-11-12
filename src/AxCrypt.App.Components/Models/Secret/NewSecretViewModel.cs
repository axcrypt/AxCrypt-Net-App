using AxCrypt.Abstractions;
using AxCrypt.Api.Model.Secret;
using AxCrypt.App.Components.Helpers;
using AxCrypt.App.Components.Password;
using AxCrypt.App.Components.Services;
using AxCrypt.App.Components.Utility;
using AxCrypt.App.Components.Utility.View;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Cryptor.Model;
using Microsoft.AspNetCore.Components;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Components.Models.Secret
{
    public class NewSecretViewModel : ManageSecretViewModel
    {
        private ProcessIndicatorService _ProcessIndicatorService;
        public NewSecretViewModel(ProcessIndicatorService processIndicatorService)
        {
            _ProcessIndicatorService = processIndicatorService;
        }

        public string ErrorMessage { get; set; }
        public bool CanShowErrorMessage { get; set; }
        public bool ShowSuggestPasswordLoadingIcon { get; set; }

        [Parameter]
        public SecretViewModel Secret { get; set; }

        public NewSecretViewModel(SecretServiceUtility secretServiceUtility)
        {
            Secret = Initialize(secretServiceUtility.CurrentSecretType);
        }

        public async Task<bool> SaveSecretAsync()
        {
            if (New<AxCryptOnlineState>().IsOffline)
            {
                ErrorMessage = Texts.NoInternetErrorMessage;
                return false;
            }

            using (ProcessIndicator processIndicator = new ProcessIndicator(_ProcessIndicatorService))
            {
                if (!ViewModelHelper.CanAddNewSecret())
                {
                    ErrorMessage = Texts.SaveSecretErrorIsReadOnly;
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

                SecretClientModel newSecret;

                newSecret = Secret.ToClientModel(Guid.NewGuid());
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

        private Action _onStateChange;

        public void SetOnStateChange(Action onStateChange)
        {
            _onStateChange = onStateChange;
        }

        public async Task SuggestPasswordAsync()
        {
            ShowSuggestPasswordLoadingIcon = true;
            Secret.Password.SecretValue = await SecretsApiHelper.SuggestPasswordAsync();
            ShowSuggestPasswordLoadingIcon = false;
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
            if (string.IsNullOrEmpty(Secret.Note.SecretDesc))
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
            if (string.IsNullOrEmpty(Secret.Password.SecretDesc) || string.IsNullOrEmpty(Secret.Password.SecretValue))
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

        #endregion Manage View Model
    }
}