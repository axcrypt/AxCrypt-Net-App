using AxCrypt.Abstractions;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Services;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.App.Shared.Helpers;

namespace AxCrypt.App.Shared.Desktop;

public class AppMain
{
    private LogOnViewModel _logOnViewModel;
    private RegisterViewModel _registerViewModel;

    private MainViewModel _mainViewModel;
    private FileOperationViewModel _fileOperationViewModel;
    private KnownFoldersViewModel _knownFoldersViewModel;

    public LogOnService _logOnService;

    public AppMain()
    {
    }

    public void Initialize(LogOnViewModel logOnViewModel, MainViewModel mainViewModel, FileOperationViewModel fileOperationViewModel, KnownFoldersViewModel knownFoldersViewModel, RegisterViewModel registerViewModel)
    {
        _logOnViewModel = logOnViewModel;
        _mainViewModel = mainViewModel;
        _fileOperationViewModel = fileOperationViewModel;
        _knownFoldersViewModel = knownFoldersViewModel;
        _registerViewModel = registerViewModel;
        SetThisVersion();
        BindToViewModels();
        BindToFileOperationViewModel();
        Resolve.UserSettings.SettingsVersion = New<UserSettingsVersion>().Current;

        _logOnService = new LogOnService(logOnViewModel, _registerViewModel, mainViewModel);
    }

    private void BindToViewModels()
    {
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.License), async (LicenseCapabilities license) => await _knownFoldersViewModel.UpdateState.ExecuteAsync(null));
        _mainViewModel.BindPropertyAsyncChanged(nameof(_mainViewModel.LoggedOn), async (bool loggedOn) => { if (loggedOn) New<InactivitySignOut>().RestartInactivityTimer(); });
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.LoggedOn), async (bool loggedOn) => { await New<IUIThread>().SendToAsync(async () => await new Display().LocalSignInWarningPopUpAsync(loggedOn)); });
        _mainViewModel.BindPropertyChanged(nameof(MainViewModel.VaultChangeDetected), async (bool changed) =>
        {
            if (changed)
            {
                AxCServiceProviderExtension.GetService<VaultViewModel>().LoadVaultItems();
                _mainViewModel.VaultChangeDetected = false;
            }
        });
        SharedFactory.LoadUpdateCheck(_mainViewModel, _logOnViewModel);
    }

    private void BindToFileOperationViewModel()
    {
        _fileOperationViewModel.FirstLegacyOpen += (sender, e) => New<IUIThread>().SendTo(async () => await SetLegacyOpenMode(e));
        _fileOperationViewModel.IdentityViewModel.LoggingOnAsync += async (e) => await New<IUIThread>().SendToAsync(async () => await _logOnService.HandleLogOn(e));
        _fileOperationViewModel.IdentityViewModel.LoggingOnWithTOTPAsync = async (e) => await New<IUIThread>().SendToAsync(async () => await _logOnService.HandleExistingAccountLogOnWithTOTP(e));
        _fileOperationViewModel.SelectingFilesAsync += async (sender, e) => await New<IUIThread>().SendToAsync(() => New<IDataItemSelection>().HandleSelection(e));
        _fileOperationViewModel.ToggleEncryptionUpgradeMode += async (sender, e) => await ToggleEncryptionUpgradeMode();
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

}