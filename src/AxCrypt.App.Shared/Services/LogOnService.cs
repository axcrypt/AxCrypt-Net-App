using System;
using AxCrypt.Abstractions;
using AxCrypt.Api.Model;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Services
{
    public class LogOnService : ISignIn
	{
        LogOnViewModel _logOnService;
        MainViewModel _mainViewModel;
        RegisterViewModel _registerViewModel;
        FileOperationViewModel _fileOperationViewModel;
        ApiVersion _apiVersion;

        public bool IsSigningIn { get; set; }

        public LogOnService(LogOnViewModel logOnViewModel, RegisterViewModel registerViewModel)
        {
            _logOnService = logOnViewModel;
            _mainViewModel = logOnViewModel.MainViewModel;
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
                HandleCreateNewLogOn(e);
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

        private void HandleCreateNewLogOn(LogOnEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.EncryptedFileFullName))
            {
                HandleCreateNewLogOnForEncryptedFile(e);
            }
            else
            {
                HandleCreateNewAccount(e);
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

        private void HandleCreateNewAccount(LogOnEventArgs e)
        {
            _registerViewModel.ShowDialog(e.Passphrase.Text, e.Identity.UserEmail);
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
            if (!_logOnService.IsVisible)
            {
                LogOnAccountViewModel logOnModel = new LogOnAccountViewModel(Resolve.UserSettings, e.EncryptedFileFullName);
                await _logOnService.ShowLogOnDialog(logOnModel, _mainViewModel);
            }

            if (_logOnService.PageResult == DialogResult.None)
            {
                return;
            }

            if (_logOnService.PageResult == DialogResult.Retry)
            {
                await ResetAllSettingsAndRestart();
            }

            if (_logOnService.PageResult == DialogResult.Cancel)
            {
                await new ApplicationManager().StopAndExit();
            }

            if (_logOnService.PageResult != DialogResult.OK || _logOnService.LogOnAccountModel.PasswordText.Length == 0)
            {
                e.Cancel = true;
                return;
            }

            e.Passphrase = new Passphrase(_logOnService.LogOnAccountModel.PasswordText);
            e.UserEmail = _logOnService.LogOnAccountModel.UserEmail;
            _logOnService.PageResult = DialogResult.None;

            return;
        }

        private async Task ResetAllSettingsAndRestart()
        {
            if (_mainViewModel.DecryptedFiles.Any())
            {
                await _mainViewModel.WarnIfAnyDecryptedFiles.ExecuteAsync(null);
                return;
            }

            PopupButtons result = await New<IPopup>().ShowAsync(PopupButtons.OkCancel, Texts.WarningTitle, Texts.ResetAllSettingsWarningText);
            if (result == PopupButtons.Ok)
            {
                new ApplicationManager().WaitForBackgroundToComplete();
                await new ApplicationManager().ClearAllSettings();
                await new ApplicationManager().ShutdownBackgroundSafe();

                New<IUIThread>().RestartApplication();
            }
        }

        public async Task SignIn()
        {
            await _fileOperationViewModel.IdentityViewModel.LogOnAsync.ExecuteAsync(null);
        }
    }
}

