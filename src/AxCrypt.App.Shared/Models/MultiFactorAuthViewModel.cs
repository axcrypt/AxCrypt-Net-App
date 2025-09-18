using AxCrypt.Abstractions;
using AxCrypt.Api.Model;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Core.Authenticator.Service;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System.ComponentModel.DataAnnotations;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared
{
    public class MultiFactorAuthViewModel : ViewModelBase
    {
        private LogOnIdentity? _LogOnIdentity { get; set; }

        public MultiFactorAuthViewModel()
        {
        }

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

            MFARememberIn mFARememberIn = (MFARememberIn)Enum.Parse(typeof(MFARememberIn), MFARememberInOption);
            RememberUntil = RememberMFAUntill(mFARememberIn);

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

        public bool RememberMeOnMFA { get; set; }

        public string UserDevice { get; set; }

        public DateTime RememberUntil { get; set; }

        public string MFARememberInOption { get; set; } = MFARememberIn.OneHour.ToString();

        public IEnumerable<KeyValuePair<string, string>> RememberMFAUntillList()
        {
            return new[]
            {
                new KeyValuePair<string, string>("1 Hour", (MFARememberIn.OneHour).ToString()),
                new KeyValuePair<string, string>("3 Hours", (MFARememberIn.ThreeHours).ToString()),
                new KeyValuePair<string, string>("6 Hours", (MFARememberIn.SixHours).ToString()),
                new KeyValuePair<string, string>("12 Hours", (MFARememberIn.TwelveHours).ToString()),
                new KeyValuePair<string, string>("1 Day", (MFARememberIn.OneDay).ToString())
            };
        }

        private DateTime RememberMFAUntill(MFARememberIn rememberUntil)
        {
            DateTime expiryTimeofMFA = New<INow>().Utc;
            switch (rememberUntil)
            {
                case MFARememberIn.OneHour: return expiryTimeofMFA.AddHours(1);
                case MFARememberIn.ThreeHours: return expiryTimeofMFA.AddHours(3);
                case MFARememberIn.SixHours: return expiryTimeofMFA.AddHours(6);
                case MFARememberIn.TwelveHours: return expiryTimeofMFA.AddHours(12);
                case MFARememberIn.OneDay: return expiryTimeofMFA.AddDays(1);
                default: throw new ArgumentOutOfRangeException(nameof(rememberUntil));
            }
        }
    }
}