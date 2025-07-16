using AxCrypt.Core.Authenticator.Service;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;
using System.ComponentModel.DataAnnotations;
using AxCrypt.App.Shared.ViewModels;

namespace AxCrypt.App.Shared
{
    public class TwoFactorAuthViewModel : ViewModelBase
    {
        private LogOnViewModel _logOnViewModel;  
        public TwoFactorAuthViewModel(LogOnViewModel logOnViewModel)
        {          
            _logOnViewModel = logOnViewModel;   
        }

        public async Task ShowLogOnDialog()
        {
            OneTimePassword = "";
            IsVisible = true;
            _logOnViewModel!.ProcessIndicator.Dispose();

            while (PageResult == DialogResult.None)
            {
                await Task.Delay(1000);
            }

            IsVisible = false;
            _logOnViewModel!.InitiateProgressIndicator();
        }

        [RegularExpression("^[0-9]{6}$", ErrorMessage = "Please enter valid code.")]
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
                _logOnViewModel!.IsTfaEnabled = value;
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
            return await New<ITwoFactorAuthenticateService>().VerifyTwoFactorAsync(oneTimeCode, AxCrypt.Core.Resolve.KnownIdentities.MFAUniqueKey);
        }
    }
}