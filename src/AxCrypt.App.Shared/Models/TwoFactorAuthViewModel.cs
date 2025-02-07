using AxCrypt.Core.Authenticator.Service;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared
{
    public class TwoFactorAuthViewModel : ViewModelBase
    {
        public void ShowLogOnDialog()
        {
            OneTimePassword = "";
            IsVisible = true;
            AxCServiceProviderExtension.LogOnViewModel!.ProcessIndicator.Dispose();

            while (PageResult == DialogResult.None)
            {
                Task.Delay(1000);
            }

            IsVisible = false;
            AxCServiceProviderExtension.LogOnViewModel!.InitiateProgressIndicator();
        }

        public string? OneTimePassword { get; set; }

        public string ErrorMessage
        {
            get { return GetProperty<string>(nameof(ErrorMessage)); }
            set { SetProperty(nameof(ErrorMessage), value); }
        }

        public DialogResult PageResult
        { get { return GetProperty<DialogResult>(nameof(PageResult)); } set { SetProperty(nameof(PageResult), value); } }

        private static bool _isVisible;

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                AxCServiceProviderExtension.LogOnViewModel!.IsTfaEnabled = value;
                UpdateViewState();
            }
        }

        public async Task TwoFactorVerifiedTaskAsync(string oneTimeCode)
        {
            if (string.IsNullOrEmpty(oneTimeCode))
            {
                return;
            }

            if (!await ValidateAsync(oneTimeCode))
            {
                ErrorMessage = "Invalid code! please try again";
                return;
            }

            PageResult = DialogResult.OK;
        }

        public async Task<bool> ValidateAsync(string oneTimeCode)
        {
            return await New<ITwoFactorAuthenticateService>().VerifyTwoFactorAsync(oneTimeCode, AxCrypt.Core.Resolve.KnownIdentities.TFAUniqueKey);
        }
    }
}