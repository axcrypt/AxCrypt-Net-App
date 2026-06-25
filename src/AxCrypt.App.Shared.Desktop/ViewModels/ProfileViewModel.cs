using AxCrypt.Abstractions;
using AxCrypt.App.Shared.Desktop.Models;
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

    // ── Profile menu items ─────────────────────────────────────
    // SVG icon strings — 16×16, stroke-based, inherit color.
    private const string IcoSettings  = "<svg width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><circle cx='12' cy='12' r='3'/><path d='M19.07 4.93l-1.41 1.41M5.34 18.66l-1.41 1.41M2 12h2M20 12h2M4.93 4.93l1.41 1.41M18.66 18.66l1.41 1.41M12 2v2M12 20v2'/></svg>";
    private const string IcoHelp      = "<svg width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><circle cx='12' cy='12' r='10'/><path d='M9.09 9a3 3 0 015.83 1c0 2-3 3-3 3'/><line x1='12' y1='17' x2='12.01' y2='17'/></svg>";
    private const string IcoCreditCard = "<svg width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><rect x='1' y='4' width='22' height='16' rx='2'/><line x1='1' y1='10' x2='23' y2='10'/></svg>";
    private const string IcoUser      = "<svg width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2'/><circle cx='12' cy='7' r='4'/></svg>";
    private const string IcoLogOut    = "<svg width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4'/><polyline points='16 17 21 12 16 7'/><line x1='21' y1='12' x2='9' y2='12'/></svg>";
    private const string IcoPower     = "<svg width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M18.36 6.64a9 9 0 11-12.73 0'/><line x1='12' y1='2' x2='12' y2='12'/></svg>";

    /// <summary>Returns the ordered list of items shown in the profile popup menu.</summary>
    public List<ProfileMenuItem> GetProfileMenuItems() => new()
    {
        new() { Label = Texts.OptionsClearAllSettingsAndExitToolStripMenuItemText, SubLabel = Texts.OptionsClearAllSettingsAndExitSubTitle, Route = "/optionsclearallsettingsandexit", IconSvg = IcoSettings },
        new() { Label = Texts.HelpToolStripMenuItemText,       SubLabel = Texts.HelpToolSubTitle,            Route = "/help",                IconSvg = IcoHelp },
        new() { Label = Texts.SubscriptionDetailsTitle,        SubLabel = Texts.SubscriptionDetailsSubTitle, Route = "/subscriptiondetails", IconSvg = IcoCreditCard },
        new() { Label = Texts.DebugManageAccountToolStripMenuItemText, SubLabel = Texts.DebugManageAccountSubTitle, Route = "/manageaccount", IconSvg = IcoUser },
        new() { IsDivider = true },
        new() { Label = Texts.LogOffText,              SubLabel = Texts.LogOffSubTitle,  Route = "/logout", IconSvg = IcoLogOut, IsDanger = true },
        new() { Label = Texts.ExitToolStripMenuItemText, SubLabel = Texts.ExitToolSubTitle, Route = "/exit", IconSvg = IcoPower, IsDanger = true },
    };

    /// <summary>
    /// Executes the action for a profile menu item.
    /// Returns true when the caller (razor) should also show the subscription details popup.
    /// </summary>
    public async Task<bool> ExecuteMenuActionAsync(ProfileMenuItem item)
    {
        switch (item.Route)
        {
            case "/optionsclearallsettingsandexit":
                await ClearAllSettingsAndRestartAsync();
                return false;
            case "/help":
                ViewHelpMenuItemClick();
                return false;
            case "/subscriptiondetails":
                return true; // signal the razor to show the SubscriptionDetails popup
            case "/manageaccount":
                RedirectToMyAxCryptIDPage();
                return false;
            case "/logout":
                await SignOut();
                return false;
            case "/exit":
                await ExitMenuItem_Click(null!);
                return false;
            default:
                return false;
        }
    }
}