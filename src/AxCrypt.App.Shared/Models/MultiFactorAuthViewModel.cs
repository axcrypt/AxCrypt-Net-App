using AxCrypt.Core.Authenticator.Service;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;
using System.ComponentModel.DataAnnotations;
using AxCrypt.Api.Model;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.UI;
using AxCrypt.App.Shared.Helpers;

namespace AxCrypt.App.Shared
{
    public class MultiFactorAuthViewModel : ViewModelBase
    {
        private LogOnIdentity? _LogOnIdentity { get; set; }

        public async Task ShowLogOnDialog(EmailAddress userEmail, Passphrase passphrase)
        {
            _LogOnIdentity = new LogOnIdentity(userEmail, passphrase);
            OneTimePassword = "";
            MultiFactorAuthType = AxCrypt.Core.Resolve.KnownIdentities.MultiFactorAuthType;

            IsVisible = true;
            while (PageResult == DialogResult.None)
            {
                await Task.Delay(1000);
            }

            IsVisible = false;
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
                AxCServiceProviderExtension.LogOnViewModel!.IsMfaEnabled = value;
                UpdateViewState();
            }
        }

        private static MultiFactorAuthType _multiFactorAuthType;

        public MultiFactorAuthType MultiFactorAuthType
        {
            get => _multiFactorAuthType;
            set
            {
                _multiFactorAuthType = value;
                UpdateViewState();
            }
        }

        public MultiFactorAuthType SelectedMFAType { get; set; }

        public async Task VerifyMFAOtpAsync(string oneTimeCode, MultiFactorAuthType multiFactorAuthType)
        {
            if (string.IsNullOrEmpty(oneTimeCode))
            {
                return;
            }

            if (!await ValidateMFAOtpAsync(oneTimeCode, multiFactorAuthType))
            {
                ErrorMessage = "Invalid code! please try again";
                return;
            }

            PageResult = DialogResult.OK;
        }

        private async Task<bool> ValidateMFAOtpAsync(string oneTimeCode, MultiFactorAuthType multiFactorAuthType)
        {
            return await New<IMultiFactorAuthService>().VerifyMultiFactorAuthAsync(oneTimeCode, AxCrypt.Core.Resolve.KnownIdentities.MFAUniqueKey, multiFactorAuthType);
        }

        public async Task<bool> SendOTPAsync()
        {
            return await New<IMultiFactorAuthService>().SendMFAOTPAsync(_LogOnIdentity);
        }
    }
}