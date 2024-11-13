using AxCrypt.Api.Model;
using AxCrypt.App.Components.Helpers;
using AxCrypt.App.Components.Password;
using AxCrypt.App.Components.Services;
using AxCrypt.App.Components.Utility.View;
using AxCrypt.Common;
using AxCrypt.Content;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Components.Models.Secret
{
    public class ViewSecretViewModel : ManageSecretViewModel
    {
        private ProcessIndicatorService _ProcessIndicatorService;

        public ViewSecretViewModel(SecretService secretService, ProcessIndicatorService processIndicatorService) : base(secretService)
        {
            _ProcessIndicatorService = processIndicatorService;

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

        public bool HidePassword
        {
            get { return GetProperty<bool>(nameof(HidePassword)); }
            private set { SetProperty(nameof(HidePassword), value); }
        }

        public bool ShowCopiedToClipboardIndicator
        {
            get { return GetProperty<bool>(nameof(ShowCopiedToClipboardIndicator)); }
            private set { SetProperty(nameof(ShowCopiedToClipboardIndicator), value); }
        }

        public SubscriptionLevel SubscriptionLevel { get; set; }

        public string? UserEmail { get; set; }

        public string GetValue(bool isVisible, string value)
        {
            return isVisible ? value : "**click to show**";
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

            using (ProcessIndicator processIndicator = new ProcessIndicator(_ProcessIndicatorService))
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
    }
}