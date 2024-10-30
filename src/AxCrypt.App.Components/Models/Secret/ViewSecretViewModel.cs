using AxCrypt.App.Components.Helpers;
using AxCrypt.App.Components.Password;
using AxCrypt.App.Components.Services;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.Runtime;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Components.Models.Secret
{
    public class ViewSecretViewModel : ManageSecretViewModel
    {
        public ViewSecretViewModel(SecretService secretService)
        {
            _secretService = secretService;
            InitializeSecret();
        }

        private readonly SecretService _secretService;

        public void InitializeSecret()
        {
            _secret = _secretService.GetCurrentSecret();
        }

        public SecretViewModel _secret { get; private set; }

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

        private async Task EditSecretAsync()
        {
            if (New<AxCryptOnlineState>().IsOffline)
            {
                ErrorMessage = Texts.NoInternetErrorMessage;
                return;
            }

            if (!HasPaidSubscription)
            {
                ErrorMessage = Texts.SaveSecretErrorIsReadOnly;
                return;
            }

            if (Secret == null)
            {
                return;
            }
        }

        private async Task ShareSecretAsync()
        {
            if (_secret == null)
            {
                return;
            }
        }

        private bool _loading { get; set; } = false;

        private Action _onStateChange;

        public void SetOnStateChange(Action onStateChange)
        {
            _onStateChange = onStateChange;
        }

        public bool Loading
        {
            get => _loading;
            set
            {
                if (_loading != value)
                {
                    _loading = value;
                    _onStateChange?.Invoke();
                }
            }
        }


        public async Task<bool> DeleteSecretAsync(IProgress<LoadingModel> progress = null)
        {
            return await LoadingProgressHelper.ExecuteLoadingProgress(async () =>
            {
                if (New<AxCryptOnlineState>().IsOffline)
                {
                    ErrorMessage = Texts.NoInternetErrorMessage;
                    return false;
                }

                HasPaidSubscription = New<AccountStatusViewModel>().PlanState == PlanState.HasPasswordManager || New<AccountStatusViewModel>().PlanState == PlanState.HasPremium || New<AccountStatusViewModel>().PlanState == PlanState.HasBusiness;

                if (!HasPaidSubscription)
                {
                    ErrorMessage = Texts.SaveSecretErrorIsReadOnly;
                    return false;
                }

                _secret = _secretService.GetCurrentSecret();
                if (_secret == null)
                {
                    return false;
                }

                SecretClientCollection secrets = await PersonalSecrets.SelectById(_secret.SecretGuid);
                if (secrets == null || !secrets.Any())
                {
                    ErrorMessage = Texts.DeleteSecretErrorNotFound;
                    return false;
                }
                bool deleted = await PersonalSecrets.DeleteAsync(secrets[0]);
                return deleted;
            },progress);
                
        }
    }
}