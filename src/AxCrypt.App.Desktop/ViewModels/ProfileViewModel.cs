using AxCrypt.Abstractions;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.Service;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Core.UI;
using AxCrypt.Core;
using AxCrypt.Content;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Runtime;
using AxCrypt.App.Shared.Models;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices;
using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Desktop.Helpers;

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
    public bool keyMPopup { get; set; }

    public ProfileViewModel()
    {
        _logOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
        _registerViewModel = AxCServiceProviderExtension.RegisterViewModel!;
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
                await ExportMySharingKey();
                break;

            case KeyManagement.ImportAxCryptID:
                _logOnViewModel.ImportPrivatePasswordDialog.Show();
                break;

            case KeyManagement.ExportAxCryptIDAndSharingKeyPair:
                await ExportMyPrivateKey();
                break;

            case KeyManagement.CreateAxCryptID:
                _registerViewModel.ShowDialog(string.Empty, EmailAddress.Empty);
                break;
        }
    }

    private async Task ImportOthersSharingKeyMenuItem_Click()
    {
        FileSelectionViewModel fileSelection = new FileSelectionViewModel();
        fileSelection.SelectFiles.Execute(FileSelectionType.ImportPublicKeys);
        IList<string> selectedFiles = await FileImportSelectionOperation();

        ImportPublicKeysViewModel importPublicKeys = new ImportPublicKeysViewModel(New<KnownPublicKeys>);
        importPublicKeys.ImportFiles.Execute(selectedFiles);
    }

    private async Task ExportMySharingKey()
    {
        UserKeyPair activeKeyPair = Resolve.KnownIdentities.DefaultEncryptionIdentity.ActiveEncryptionKeyPair;
        EmailAddress userEmail = activeKeyPair.UserEmail;
        IAsymmetricPublicKey publicKey = activeKeyPair.KeyPair.PublicKey;

        string fileName = $"{Texts.DialogExportSharingKeyFileName.InvariantFormat(userEmail.Address, publicKey.Tag)}";

        UserPublicKey userPublicKey = new UserPublicKey(userEmail, publicKey);
        string serialized = Core.Resolve.Serializer.Serialize(userPublicKey);

        string filter = Texts.FileFilterDialogFilterPatternWin.InvariantFormat("." + DefaultExt, Texts.FileFilterFileTypePublicSharingKeyFiles, Texts.FileFilterFileTypeAllFiles);

        //ExportKeyManagementFile exportKeyManagementFile = new ExportKeyManagementFile();
        //string savedPath = await exportKeyManagementFile.ShowSaveFileDialogAsync(Texts.DialogExportAxCryptIdTitle, DefaultExt, filter, fileName);
        string savedPath = "";
        if (!string.IsNullOrEmpty(savedPath))
        {
            await File.WriteAllTextAsync(savedPath, serialized);
        }
    }

    private async Task ExportMyPrivateKey()
    {
        UserKeyPair activeKeyPair = Resolve.KnownIdentities.DefaultEncryptionIdentity.ActiveEncryptionKeyPair;
        EmailAddress userEmail = activeKeyPair.UserEmail;
        IAsymmetricPublicKey publicKey = activeKeyPair.KeyPair.PublicKey;

        string fileName = $"{Texts.DialogExportAxCryptIdFileName.InvariantFormat(activeKeyPair.UserEmail, publicKey.Tag)}";
        string filter = Texts.FileFilterDialogFilterPatternWin.InvariantFormat("." + New<IRuntimeEnvironment>().AxCryptExtension, Texts.FileFilterFileTypeAxCryptIdFiles, Texts.FileFilterFileTypeAllFiles);
        byte[] export = activeKeyPair.ToArray(Resolve.KnownIdentities.DefaultEncryptionIdentity.Passphrase);

        //ExportKeyManagementFile exportKeyManagementFile = new ExportKeyManagementFile();
        //string savedPath = await exportKeyManagementFile.ShowSaveFileDialogAsync(Texts.DialogExportAxCryptIdTitle, New<IRuntimeEnvironment>().AxCryptExtension, filter, fileName);
        string savedPath = "";
        if (!string.IsNullOrEmpty(savedPath))
        {
            await File.WriteAllBytesAsync(savedPath, export);
        }
    }

    public static async Task<IList<string>> FileImportSelectionOperation()
    {
        FileSelectionEventArgs fileSelectionEventArgs = new FileSelectionEventArgs(new string[0])
        {
            FileSelectionType = FileSelectionType.ImportPublicKeys,
        };

        IEnumerable<FileResult> selectedFiles = await InternalFileSelectionAsync(fileSelectionEventArgs);

        if (fileSelectionEventArgs.Cancel)
        {
            return new List<string>();
        }

        foreach (string file in selectedFiles.Select(e => e.FullPath))
        {
            fileSelectionEventArgs.SelectedFiles.Add(file);
        }

        return fileSelectionEventArgs.SelectedFiles;
    }

    private static async Task<IEnumerable<FileResult>> InternalFileSelectionAsync(FileSelectionEventArgs e)
    {
        IDictionary<DevicePlatform, IEnumerable<string>> fileTypes = GetFileTypesForSelectionType(e.FileSelectionType);

        FilePickerFileType customFileType = new FilePickerFileType(fileTypes);
        IEnumerable<FileResult> pickResult = await FilePicker.PickMultipleAsync(new PickOptions
        {
            PickerTitle = "Please select files",
            FileTypes = customFileType,
        });

        if (!pickResult.Any())
        {
            e.Cancel = true;
        }

        return pickResult;
    }

    public static IDictionary<DevicePlatform, IEnumerable<string>> GetFileTypesForSelectionType(FileSelectionType selectionType)
    {
        Dictionary<DevicePlatform, IEnumerable<string>> fileTypes = new Dictionary<DevicePlatform, IEnumerable<string>>();
        IRuntimeEnvironment runtimeEnvironment = New<IRuntimeEnvironment>();

        switch (selectionType)
        {
            case FileSelectionType.Open:
            case FileSelectionType.Decrypt:
                fileTypes.Add(DevicePlatform.WinUI, new[] { "." + runtimeEnvironment.AxCryptExtension });
                fileTypes.Add(DevicePlatform.iOS, new string[] { });
                fileTypes.Add(DevicePlatform.Android, new string[] { });
                break;

            case FileSelectionType.Encrypt:
            case FileSelectionType.Rename:
            case FileSelectionType.KeySharing:
            case FileSelectionType.KeySharingEncrypt:
            case FileSelectionType.Wipe:
                fileTypes.Add(DevicePlatform.WinUI, new string[] { });
                fileTypes.Add(DevicePlatform.iOS, new string[] { });
                fileTypes.Add(DevicePlatform.Android, new string[] { });
                break;

            case FileSelectionType.ImportPublicKeys:
            case FileSelectionType.ImportPrivateKeys:
                fileTypes.Add(DevicePlatform.WinUI, new[] { ".txt", "." + runtimeEnvironment.AxCryptExtension });
                fileTypes.Add(DevicePlatform.iOS, new string[] { });
                fileTypes.Add(DevicePlatform.Android, new string[] { });
                break;

            default:
                throw new NotImplementedException("File selection type not supported.");
        }

        return fileTypes;
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

    public void ToggleSubsDtlsPopup()
    {
        SubsDtlsPopup = !SubsDtlsPopup;
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

    public void ToggleKMPopup()
    {
        keyMPopup = !keyMPopup;
    }

    public IDictionary<string, object> NavLinkAttributes()
    {
        Dictionary<string, object> attributes = new Dictionary<string, object>();
        attributes["class"] = "nav-link next-arrow" + (keyMPopup ? " active" : "");
        return attributes;
    }

    public void CancelSubscription()
    {
        New<Abstractions.IBrowser>().OpenUri(new Uri("https://account.axcrypt.net/en/Home/Login"));
    }

    public void UpgradeSubscription()
    {
        New<Abstractions.IBrowser>().OpenUri(new Uri("https://account.axcrypt.net/"));
    }

    public void HandleDialogClose(bool isOpen)
    {
        IsDialogOpen = isOpen;
    }

    public void HandleCreateDialogClose(bool isOpen)
    {
        IsDialogOpen = isOpen;
    }
}