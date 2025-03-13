using AxCrypt.Abstractions;
using AxCrypt.App.Desktop.ViewModels;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Desktop;

public class AppMain
{
    //private ICustomNavigationService _navigationManager;
    private LogOnViewModel _logOnService;

    private RegisterViewModel _registerViewModel;

    private MainViewModel _mainViewModel;
    private FileOperationViewModel _fileOperationViewModel;
    private KnownFoldersViewModel _knownFoldersViewModel;

    //private ApiVersion? _apiVersion;
    public AppMain()
    {
    }

    public void Initialize(LogOnViewModel logOnService, MainViewModel mainViewModel, FileOperationViewModel fileOperationViewModel, KnownFoldersViewModel knownFoldersViewModel, RegisterViewModel registerViewModel)
    {
        _logOnService = logOnService;
        _mainViewModel = mainViewModel;
        _fileOperationViewModel = fileOperationViewModel;
        _knownFoldersViewModel = knownFoldersViewModel;
        _registerViewModel = registerViewModel;
        SetThisVersion();
        BindToViewModels();
        BindToFileOperationViewModel();
    }

    private void BindToViewModels()
    {
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.DebugMode), (bool enabled) => { UpdateDebugMode(enabled); });
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.License), async (LicenseCapabilities license) => await _knownFoldersViewModel.UpdateState.ExecuteAsync(null));
        _mainViewModel.BindPropertyAsyncChanged(nameof(_mainViewModel.LoggedOn), async (bool loggedOn) => { if (loggedOn) New<InactivitySignOut>().RestartInactivityTimer(); });
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.LoggedOn), async (bool loggedOn) => { await new Display().LocalSignInWarningPopUpAsync(loggedOn); });
    }

    private void BindToFileOperationViewModel()
    {
        _fileOperationViewModel.FirstLegacyOpen += (sender, e) => New<IUIThread>().SendTo(async () => await SetLegacyOpenMode(e));
        _fileOperationViewModel.IdentityViewModel.LoggingOnAsync = async (e) => await New<IUIThread>().SendToAsync(async () => await HandleLogOn(e));
        _fileOperationViewModel.SelectingFilesAsync += async (sender, e) => await New<IUIThread>().SendToAsync(() => New<IDataItemSelection>().HandleSelection(e));
        _fileOperationViewModel.ToggleEncryptionUpgradeMode += async (sender, e) => await ToggleEncryptionUpgradeMode();
    }

    private void UpdateDebugMode(bool enabled)
    {
        //_optionsDebugToolStripMenuItem.Checked = enabled;
        //_debugToolStripMenuItem.Visible = enabled;
    }

    private static void SetThisVersion()
    {
        New<UserSettings>().ThisVersion = New<IVersion>().Current.ToString();
    }

    private static async Task SetLegacyOpenMode(FileOperationEventArgs e)
    {
        if (!Resolve.KnownIdentities.IsLoggedOn)
        {
            return;
        }

        PopupButtons click = await New<IPopup>().ShowAsync(PopupButtons.OkCancel, Texts.WarningTitle, Texts.LegacyOpenMessage);
        if (click == PopupButtons.Cancel)
        {
            e.Cancel = true;
            return;
        }
    }

    private async Task ToggleEncryptionUpgradeMode()
    {
        if (_mainViewModel.EncryptionUpgradeMode == EncryptionUpgradeMode.AutoUpgrade)
        {
            _mainViewModel.EncryptionUpgradeMode = EncryptionUpgradeMode.RetainWithoutUpgrade;
            return;
        }

        if (!await New<IVerifySignInPassword>().Verify(Texts.LegacyConversionVerificationPrompt))
        {
            return;
        }

        _mainViewModel.EncryptionUpgradeMode = EncryptionUpgradeMode.AutoUpgrade;
    }

    private async Task HandleLogOn(LogOnEventArgs e)
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
}