using AxCrypt.Api.Model;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.User;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Services
{
    public class LogOnService : ISignIn
    {
        private LogOnViewModel _logOnViewModel;
        private MainViewModel _mainViewModel;
        private RegisterViewModel _registerViewModel;
        private FileOperationViewModel _fileOperationViewModel;
        private ApiVersion _apiVersion;

        public bool IsSigningIn { get; set; }

        public LogOnService(LogOnViewModel logOnViewModel, RegisterViewModel registerViewModel, MainViewModel mainViewModel)
        {
            _logOnViewModel = logOnViewModel;
            _mainViewModel = mainViewModel;
            _registerViewModel = registerViewModel;
            _fileOperationViewModel = logOnViewModel.FileOperationViewModel;
            _apiVersion = new ApiVersion();
        }

        public async Task SignInAsync()
        {
            SignUpSignIn signUpSignIn = new SignUpSignIn(_registerViewModel)
            {
                Version = _apiVersion,
                UserEmail = New<UserSettings>().UserEmail,
            };

            await signUpSignIn.DialogsAsync(this);

            New<UserSettings>().UserEmail = signUpSignIn.UserEmail;

            if (signUpSignIn.StopAndExit)
            {
                await new ApplicationManager().StopAndExit();
                return;
            }

            if (_mainViewModel.LoggedOn && Thread.CurrentThread.CurrentUICulture.Name != Resolve.UserSettings.CultureName)
            {
                //await SetLanguageAsync(Resolve.UserSettings.CultureName);
            }

            //ShowRenewSubscriptionDialog();
        }

        public async Task HandleLogOn(LogOnEventArgs e)
        {
            if (e.IsAskingForPreviouslyUnknownPassphrase)
            {
                await HandleCreateNewLogOn(e);
            }
            else
            {
                await HandleExistingLogOn(e);
            }
            if (New<UserSettings>().RestoreFullWindow)
            {
                //Styling.RestoreWindowWithFocus(this);
            }
        }

        private async Task HandleCreateNewLogOn(LogOnEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.EncryptedFileFullName))
            {
                HandleCreateNewLogOnForEncryptedFile(e);
            }
            else
            {
                await HandleCreateNewAccount(e);
            }
        }

        private void HandleCreateNewLogOnForEncryptedFile(LogOnEventArgs e)
        {
            NewPasswordViewModel viewModel = new NewPasswordViewModel(e.Passphrase.Text, e.EncryptedFileFullName);

            //using (NewPassphraseDialog passphraseDialog = new NewPassphraseDialog(this, Texts.NewPassphraseDialogTitle, viewModel))
            //{
            //    viewModel.ShowPassword = e.DisplayPassphrase;
            //    DialogResult dialogResult = passphraseDialog.ShowDialog(this);
            //    e.DisplayPassphrase = viewModel.ShowPassword;
            //    if (dialogResult != DialogResult.OK || viewModel.PasswordText.Length == 0)
            //    {
            //        e.Cancel = true;
            //        return;
            //    }
            //    e.Passphrase = new Passphrase(viewModel.PasswordText);
            //    e.Name = String.Empty;
            //}
            return;
        }

        private async Task HandleCreateNewAccount(LogOnEventArgs e)
        {
            await _registerViewModel.ShowDialog(e.Passphrase.Text, e.Identity.UserEmail);
            DialogResult result = _registerViewModel.DialogResult;
            if (result != DialogResult.OK)
            {
                e.Cancel = true;
                return;
            }

            e.DisplayPassphrase = _registerViewModel.CreateAccountModel.ShowPassword;
            e.Passphrase = new Passphrase(_registerViewModel.CreateAccountModel.PasswordText);
            e.UserEmail = _registerViewModel.CreateAccountModel.UserEmail;
        }

        private async Task HandleExistingLogOn(LogOnEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.EncryptedFileFullName) && (string.IsNullOrEmpty(Resolve.UserSettings.UserEmail) || Resolve.KnownIdentities.IsLoggedOn))
            {
                await HandleExistingLogOnForEncryptedFile(e);
            }
            else
            {
                await HandleExistingAccountLogOn(e);
            }
        }

        private async Task HandleExistingLogOnForEncryptedFile(LogOnEventArgs e)
        {
            FilePasswordDialogViewModel filePasswordDialog = AxCServiceProvider.GetService<FilePasswordDialogViewModel>();
            await filePasswordDialog.ShowFilePasswordDialog(e.EncryptedFileFullName);

            if (filePasswordDialog.DialogResult == DialogResult.Retry)
            {
                e.Passphrase = filePasswordDialog.ViewModel!.Passphrase;
                e.IsAskingForPreviouslyUnknownPassphrase = true;
                return;
            }

            if (filePasswordDialog.DialogResult != DialogResult.OK || filePasswordDialog.ViewModel!.Passphrase == Passphrase.Empty)
            {
                e.Cancel = true;
                return;
            }

            e.Passphrase = filePasswordDialog.ViewModel.Passphrase;
        }

        private async Task HandleExistingAccountLogOn(LogOnEventArgs e)
        {
            if (!_logOnViewModel.IsVisible)
            {
                _logOnViewModel.PageResult = DialogResult.None;

                if (WorkUserProfile.UserEmail == "" && Resolve.UserSettings.UserEmail != "")
                {
                    WorkUserProfile.SetUser(New<IUserProfilesStore>().AppRootFolder, Resolve.UserSettings.UserEmail);
                }

                Resolve.UserSettings.UserEmail = WorkUserProfile.UserEmail;
                LogOnAccountViewModel logOnModel = new LogOnAccountViewModel(Resolve.UserSettings, e.EncryptedFileFullName);
                await _logOnViewModel.ShowLogOnDialog(logOnModel, _mainViewModel);
            }

            if (_logOnViewModel.PageResult == DialogResult.None)
            {
                return;
            }

            if (_logOnViewModel.PageResult == DialogResult.Retry)
            {
                await ResetAllSettingsAndRestart();
            }

            if (_logOnViewModel.PageResult == DialogResult.Cancel)
            {
                return;
                //await new ApplicationManager().StopAndExit();
            }

            if (_logOnViewModel.PageResult != DialogResult.OK || _logOnViewModel.LogOnAccountModel.PasswordText.Length == 0)
            {
                e.Cancel = true;
                return;
            }

            e.Passphrase = new Passphrase(_logOnViewModel.LogOnAccountModel.PasswordText);
            e.UserEmail = _logOnViewModel.LogOnAccountModel.UserEmail;
            e.UserDevice = _logOnViewModel.CurrentUserDevice;
            _logOnViewModel.PageResult = DialogResult.None;

            return;
        }

        public async Task HandleExistingAccountLogOnWithTOTP(LogOnEventArgs e)
        {
            if (e.UserEmail == null || e.Passphrase == null)
            {
                return;
            }

            if (!_logOnViewModel.MultiFactorAuthViewModel!.IsVisible)
            {
                await _logOnViewModel.MultiFactorAuthViewModel.ShowLogOnDialog(EmailAddress.Parse(e.UserEmail), e.Passphrase);
            }

            if (_logOnViewModel.MultiFactorAuthViewModel.PageResult == DialogResult.None)
            {
                return;
            }

            if (_logOnViewModel.MultiFactorAuthViewModel.PageResult == DialogResult.Cancel)
            {
                e.Cancel = true;
                _logOnViewModel.MultiFactorAuthViewModel.PageResult = DialogResult.None;

                return;
            }

            if (_logOnViewModel.MultiFactorAuthViewModel.PageResult != DialogResult.OK || _logOnViewModel.MultiFactorAuthViewModel!.OneTimePassword!.Length == 0)
            {
                e.Cancel = true;
                return;
            }

            e.OneTimePassword = _logOnViewModel.MultiFactorAuthViewModel.OneTimePassword;
            e.MFAType = _logOnViewModel.MultiFactorAuthViewModel.SelectedMFAType;
            e.UserDevice = _logOnViewModel.MultiFactorAuthViewModel.UserDevice;
            e.RememberUntil = _logOnViewModel.MultiFactorAuthViewModel.RememberUntil;
            e.RememberMeOnMFA = _logOnViewModel.MultiFactorAuthViewModel.RememberMeOnMFA;
            _logOnViewModel.MultiFactorAuthViewModel.PageResult = DialogResult.None;

            return;
        }

        private async Task ResetAllSettingsAndRestart()
        {
            await AppLifecycleHandler.RestartApplication();
        }

        public async Task SignIn()
        {
            await _fileOperationViewModel.IdentityViewModel.LogOnAsync.ExecuteAsync(null);
        }
    }
}