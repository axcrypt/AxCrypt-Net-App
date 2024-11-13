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

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Components.Models.Secret
{
    public class NewSecretViewModel : ManageSecretViewModel
    {
        private ProcessIndicatorService? _ProcessIndicatorService;

        public NewSecretViewModel(SecretService secretService, ProcessIndicatorService processIndicatorService = null) : base(secretService)
        {
            _ProcessIndicatorService = processIndicatorService;

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

        public async Task SuggestPasswordAsync()
        {
            ShowSuggestPasswordLoadingIcon = true;
            Secret.Password.SecretValue = await SecretsApiHelper.SuggestPasswordAsync();
            ShowSuggestPasswordLoadingIcon = false;
        }

        #endregion Manage View Model
    }
}