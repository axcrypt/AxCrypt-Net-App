using AxCrypt.Abstractions;
using AxCrypt.App.Desktop.Helpers;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Services.Interface;
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

namespace AxCrypt.App.Desktop.ViewModels;

public class ProfileViewModel
{
    private LogOnViewModel _logOnViewModel;
    private RegisterViewModel _registerViewModel;
    private FileOperationViewModel? _fileOperationViewModel;
    private MainViewModel? _mainViewModel;
    private IExportKeyManagementFile? ExportKeyFile;
    private string DefaultExt = ".txt";

    public AccountModel Account { get; set; }
    public bool SubsDtlsPopup { get; set; }
    public bool IsDialogOpen { get; set; } = false;
    public string ValidFormatted => Account.DaysLeft == 0 ? "0 days left" : New<INow>().Utc.AddDays(Account.DaysLeft).ToString("dd MMM yyyy");

    public ProfileViewModel()
    {
        _logOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
        _registerViewModel = AxCServiceProviderExtension.RegisterViewModel!;
        ExportKeyFile = AxCServiceProviderExtension.GetService<IExportKeyManagementFile>()!;
        Account = new AccountModel();
    }

    public async Task InitializeAsync()
    {
        _mainViewModel = _logOnViewModel.MainViewModel;
        _mainViewModel.LoggedOn = Resolve.KnownIdentities.IsLoggedOn;
        Account.IsLoggedOn = _mainViewModel.LoggedOn;
        _fileOperationViewModel = _logOnViewModel.FileOperationViewModel;

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
                    _logOnViewModel.ImportPrivatePasswordDialog.Show();
                }
                break;

            case KeyManagement.ExportAxCryptIDAndSharingKeyPair:
                await ImportMyPrivateKeyToolStripMenuItem_Click();
                break;

            case KeyManagement.CreateAxCryptID:
                if (!_logOnViewModel.IsLoggedOn)
                {
                    _registerViewModel.ShowDialog(string.Empty, EmailAddress.Empty);
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
        importPublicKeys.ImportFiles.Execute(fileSelectionArgs.SelectedFiles);
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

        if (!string.IsNullOrEmpty(savedPath))
        {
            await File.WriteAllTextAsync(savedPath, serialized);
        }
    }

    private async Task ImportMyPrivateKeyToolStripMenuItem_Click()
    {
        UserKeyPair activeKeyPair = Resolve.KnownIdentities.DefaultEncryptionIdentity.ActiveEncryptionKeyPair;
        EmailAddress userEmail = activeKeyPair.UserEmail;
        Core.Crypto.Asymmetric.IAsymmetricPublicKey publicKey = activeKeyPair.KeyPair.PublicKey;

        string fileName = $"{Texts.DialogExportAxCryptIdFileName.InvariantFormat(activeKeyPair.UserEmail, publicKey.Tag)}";
        string filter = Texts.FileFilterDialogFilterPatternWin.InvariantFormat("." + New<IRuntimeEnvironment>().AxCryptExtension, Texts.FileFilterFileTypeAxCryptIdFiles, Texts.FileFilterFileTypeAllFiles);
        byte[] export = activeKeyPair.ToArray(Resolve.KnownIdentities.DefaultEncryptionIdentity.Passphrase);

        string data = Convert.ToBase64String(export);

        string savedPath = await ExportKeyFile!.ShowSaveFileDialogAsync(Texts.DialogExportAxCryptIdTitle, New<IRuntimeEnvironment>().AxCryptExtension, filter, fileName);

        if (!string.IsNullOrEmpty(savedPath))
        {
            await File.WriteAllBytesAsync(savedPath, export);
        }
    }

    public void ChangePassphraseMenuItem_Click(EventArgs e)
    {
        string userEmail = New<UserSettings>().UserEmail.ToString();
        userEmail.ProcessChangePassword();
    }

    public void PasswordReset_Click(EventArgs e)
    {
        if (!_mainViewModel.LoggedOn && !string.IsNullOrEmpty(New<UserSettings>().UserEmail))
        {
            BrowseUtility.RedirectToAccountWebUrl(Texts.PasswordResetHyperLink);
        }
    }

    public async void ClearAllSettingsAndRestartAsync()
    {
        if (_mainViewModel.DecryptedFiles.Any())
        {
            await _mainViewModel.WarnIfAnyDecryptedFiles.ExecuteAsync(null);
            return;
        }
        await new ApplicationManager().ClearAllSettings();
        await ShutDownAnd(New<IUIThread>().RestartApplication);
    }

    private async Task ShutDownAnd(Action finalAction)
    {
        await new ApplicationManager().ShutdownBackgroundSafe();
        await EncryptPendingFiles();

        finalAction();
    }

    private async Task EncryptPendingFiles()
    {
        if (_mainViewModel != null)
        {
            new ApplicationManager().WaitForBackgroundToComplete();
            await _mainViewModel.EncryptPendingFiles.ExecuteAsync(null);
            new ApplicationManager().WaitForBackgroundToComplete();
        }
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
        await Task.Run(async () =>
        {
            if (_mainViewModel.DecryptedFiles.Any())
            {
                await _mainViewModel.WarnIfAnyDecryptedFiles.ExecuteAsync(null);
                return;
            }

            await _logOnViewModel.InvokeLogOnOrLogOffAndLogOnAgainAsync();
        });
    }

    public async void ExitMenuItem_Click(EventArgs e)
    {
        if (_mainViewModel.LoggedOn && _mainViewModel.DecryptedFiles.Any())
        {
            await _mainViewModel.WarnIfAnyDecryptedFiles.ExecuteAsync(null);
            return;
        }

        await ShutDownAnd(New<IUIThread>().ExitApplication);
    }

    public string GetDisabledClass(bool isDisabled)
    {
        return isDisabled ? "disabled" : string.Empty;
    }

    public void CancelSubscription()
    {
        New<Abstractions.IBrowser>().OpenUri(new Uri("https://account.axcrypt.net/en/Home/Login"));
    }

    public void UpgradeSubscription()
    {
        New<Abstractions.IBrowser>().OpenUri(new Uri("https://account.axcrypt.net/"));
    }
}