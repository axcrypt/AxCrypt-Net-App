using AxCrypt.Abstractions;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Service;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.ViewModels;

public class ProfileViewModel : ViewModelBase
{
    private LogOnViewModel _logOnViewModel;
    private RegisterViewModel _registerViewModel;
    private MainViewModel? _mainViewModel;
    private IStatusAlertService? _statusAlertService;
    private IExportKeyManagementFile? ExportKeyFile;
    private string DefaultExt = ".txt";

    public AccountModel Account { get; set; }

    public bool IsUserActivityEnabled
    {
        get
        {
            return New<UserSettings>().UserActivityMode;
        }
    }

    public bool FindFileEnabled
    {
        get
        {
            return New<UserSettings>().FindFileMode;
        }
    }

    public string ValidFormatted => Account.DaysLeft == 0 ? "0 days left" : New<LicensePolicy>().Expiration.ToString("dd MMMM yyyy", System.Globalization.CultureInfo.CurrentCulture);

    public ProfileViewModel()
    {
        _logOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
        _registerViewModel = AxCServiceProviderExtension.RegisterViewModel!;
        _mainViewModel = _logOnViewModel.MainViewModel;
        _statusAlertService = AxCServiceProviderExtension.StatusAlertService;
        ExportKeyFile = AxCServiceProviderExtension.GetService<IExportKeyManagementFile>()!;
        Account = new AccountModel();
    }

    public async Task InitializeAsync()
    {
        await New<AccountStatusViewModel>().LoadAccountStatusAsync();
        _mainViewModel = _logOnViewModel.MainViewModel;
        Account.IsLoggedOn = New<KnownIdentities>().IsLoggedOn;

        Account.SubscriptionLevel = _logOnViewModel.SubscriptionLevel;
        Account.UserEmail = Resolve.KnownIdentities.DefaultEncryptionIdentity.UserEmail.Address;
        Account.Subscription = await DetermineSubscriptionType();
        Account.DaysLeft = New<AccountStatusViewModel>().DaysLeft;
    }

    public async Task<string> DetermineSubscriptionType()
    {
        Account.CreatedTime = await GetManageAxCryptID();
        double totalDays = (New<INow>().Utc - Account.CreatedTime).TotalDays + Account.DaysLeft;
        return totalDays <= 30 ? "Monthly" : "Yearly";
    }

    public async Task<DateTime> GetManageAxCryptID()
    {
        if (!string.IsNullOrEmpty(Resolve.KnownIdentities.DefaultEncryptionIdentity.UserEmail.Address))
        {
            AccountStorage userKeyPairs = new AccountStorage(New<LogOnIdentity, IAccountService>(Resolve.KnownIdentities.DefaultEncryptionIdentity));
            ManageAccountViewModel viewModel = await ManageAccountViewModel.CreateAsync(userKeyPairs);
            Account.CreatedTime = viewModel.AccountProperties.First().Timestamp;
            return Account.CreatedTime;
        }

        return Account.CreatedTime;
    }

    public void OnOpenLogViewerClicked(LogType logType)
    {
        New<IDebugLoggingWindow>().ShowLogWindow(logType);
    }

    public async Task HandleImportAndExportKeys(KeyManagement keyManagement)
    {
        switch (keyManagement)
        {
            case KeyManagement.ImportSomeonesSharingKey:
                await ImportOthersSharingKeyMenuItem_Click();
                break;

            case KeyManagement.ExportMySharingKey:
                await ExportMySharingKeyToolStripMenuItem_Click();
                break;

            case KeyManagement.ImportAxCryptID:
                if (!_logOnViewModel.IsLoggedOn)
                {
                    ImportPrivateKeyViewModel importPrivateKeyDialog = AxCServiceProvider.GetService<ImportPrivateKeyViewModel>();
                    await importPrivateKeyDialog.ShowDialogAsync(Resolve.UserSettings, Resolve.KnownIdentities);
                }
                break;

            case KeyManagement.ExportAxCryptIDAndSharingKeyPair:
                await ExportMyPrivateKeyToolStripMenuItem_Click();
                break;

            case KeyManagement.CreateAxCryptID:
                if (!_logOnViewModel.IsLoggedOn)
                {
                    _registerViewModel.DialogResult = DialogResult.None;
                    await _registerViewModel.ShowDialog(string.Empty, EmailAddress.Empty);
                }
                break;
        }
    }

    private async Task ImportOthersSharingKeyMenuItem_Click()
    {
        FileSelectionEventArgs fileSelectionArgs = new FileSelectionEventArgs(new List<string>())
        {
            FileSelectionType = FileSelectionType.ImportPublicKeys,
        };
        await New<IDataItemSelection>().HandleSelection(fileSelectionArgs);

        ImportPublicKeysViewModel importPublicKeys = new ImportPublicKeysViewModel(New<KnownPublicKeys>);

        if (!fileSelectionArgs.SelectedFiles.Any())
        {
            return;
        }

        importPublicKeys.ImportFiles.Execute(fileSelectionArgs.SelectedFiles);

        if (importPublicKeys.FailedFiles.Any())
        {
            _statusAlertService!.Error($"Failed to import key!");
            return;
        }

        _statusAlertService!.Success($"Imported successfully!.");
    }

    private async Task ExportMySharingKeyToolStripMenuItem_Click()
    {
        UserKeyPair activeKeyPair = Resolve.KnownIdentities.DefaultEncryptionIdentity.ActiveEncryptionKeyPair;
        EmailAddress userEmail = activeKeyPair.UserEmail;
        Core.Crypto.Asymmetric.IAsymmetricPublicKey publicKey = activeKeyPair.KeyPair.PublicKey;

        string fileName = $"{Texts.DialogExportSharingKeyFileName.InvariantFormat(userEmail.Address, publicKey.Tag)}";
        Core.Crypto.Asymmetric.UserPublicKey userPublicKey = new Core.Crypto.Asymmetric.UserPublicKey(userEmail, publicKey);

        string serialized = Resolve.Serializer.Serialize(userPublicKey);

        string filter = Texts.FileFilterDialogFilterPatternWin.InvariantFormat("." + DefaultExt, Texts.FileFilterFileTypePublicSharingKeyFiles, Texts.FileFilterFileTypeAllFiles);

        string savedPath = await ExportKeyFile!.ShowSaveFileDialogAsync(Texts.DialogExportAxCryptIdTitle, DefaultExt, filter, fileName);
        if (string.IsNullOrEmpty(savedPath))
        {
            return;
        }

        try
        {
            await File.WriteAllTextAsync(savedPath, serialized);
            _statusAlertService!.Success($"Your AxCrypt sharing key pairs are saved in {savedPath}.");
        }
        catch (Exception ex)
        {
            _statusAlertService!.Error($"Failed to export your AxCrypt sharing key, due to {ex.Message}!");
            return;
        }
    }

    private async Task ExportMyPrivateKeyToolStripMenuItem_Click()
    {
        UserKeyPair activeKeyPair = Resolve.KnownIdentities.DefaultEncryptionIdentity.ActiveEncryptionKeyPair;
        EmailAddress userEmail = activeKeyPair.UserEmail;
        Core.Crypto.Asymmetric.IAsymmetricPublicKey publicKey = activeKeyPair.KeyPair.PublicKey;

        string fileName = $"{Texts.DialogExportAxCryptIdFileName.InvariantFormat(activeKeyPair.UserEmail, publicKey.Tag)}";
        string filter = Texts.FileFilterDialogFilterPatternWin.InvariantFormat("." + New<IRuntimeEnvironment>().AxCryptExtension, Texts.FileFilterFileTypeAxCryptIdFiles, Texts.FileFilterFileTypeAllFiles);
        byte[] export = activeKeyPair.ToArray(Resolve.KnownIdentities.DefaultEncryptionIdentity.Passphrase);

        string savedPath = await ExportKeyFile!.ShowSaveFileDialogAsync(Texts.DialogExportAxCryptIdTitle, New<IRuntimeEnvironment>().AxCryptExtension, filter, fileName);
        if (string.IsNullOrEmpty(savedPath))
        {
            return;
        }

        try
        {
            await File.WriteAllBytesAsync(savedPath, export);
            _statusAlertService!.Success($"Your AxCrypt ID and sharing key pairs are saved in {savedPath}.");
        }
        catch (Exception ex)
        {
            _statusAlertService!.Error($"Failed to export the AxCrypt ID and sharing key pairs, due to {ex.Message}!");
            return;
        }
    }

    public void ChangePassphraseMenuItem_Click(EventArgs e)
    {
        string userEmail = New<UserSettings>().UserEmail.ToString();
        userEmail.ProcessChangePassword();
    }

    public void PasswordReset_Click(EventArgs e)
    {
        if (!_mainViewModel!.LoggedOn && !string.IsNullOrEmpty(New<UserSettings>().UserEmail))
        {
            BrowseUtility.RedirectToAccountWebUrl(Texts.PasswordResetHyperLink);
        }
    }

    public async Task ClearAllSettingsAndRestartAsync()
    {
        await AppLifecycleHandler.RestartApplication();
    }

    public void RedirectToMyAxCryptIDPage()
    {
        BrowseUtility.RedirectToMyAxCryptIDPage();
    }

    public void ViewHelpMenuItemClick()
    {
        BrowseUtility.RedirectTo(Resolve.UserSettings.AxCrypt2HelpUrl.ToString());
    }

    public async Task SignOut()
    {
        await AppLifecycleHandler.SignOutSignIn();
    }

    public async Task ExitMenuItem_Click(EventArgs e)
    {
        await AppLifecycleHandler.ExitApplication();
    }

    public string GetDisabledClass(bool isDisabled)
    {
        return isDisabled ? "disabled" : string.Empty;
    }

    //public void CancelSubscription()
    //{
    //    BrowseUtility.RedirectToMyAxCryptIDPage();
    //}

    public void UpgradeSubscription()
    {
        BrowseUtility.RedirectToPurchasePage(Account.UserEmail, true, "");
    }
}