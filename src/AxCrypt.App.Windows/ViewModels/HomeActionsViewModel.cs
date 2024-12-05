using AxCrypt.Api.Model;
using AxCrypt.App.Components.Models;
using AxCrypt.App.Components.Services.Interface;
using AxCrypt.App.Windows.Services;
using AxCrypt.Common;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Service;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using Microsoft.AspNetCore.Components;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.ViewModels;

public class HomeActionsViewModel : ComponentBase
{
    private LogOnViewModel _logOnViewModel;
    private FileOperationViewModel _fileOperationViewModel;
    private MainViewModel? _mainViewModel;
    private FileSystemState? _fileSystemState;
    private IStatusAlertService _statusAlertService;
    private ShareKeyViewModel? _sharekeyViewModel;

    //public ObservableCollection<FileDetails> SelectedRecentFiles { get; set; } = new ObservableCollection<FileDetails>();
    public KnownFoldersViewModel? KnownFoldersViewModel { get; set; }

    public int MembersCount { get; set; }
    public int TotalMembers { get; set; }
    public string? DisabledBackColor { get; set; }
    public bool IsFilesPending { get; set; }

    public bool EncryptButtonEnabled { get; set; }

    public bool KeyShareButtonEnabled { get; set; }

    public HomeActionsViewModel(LogOnViewModel logOnViewModel, ShareKeyViewModel shareKeyViewModel, IStatusAlertService statusAlertService)
    {
        _logOnViewModel = logOnViewModel;
        _sharekeyViewModel = shareKeyViewModel;
        _statusAlertService = statusAlertService;
        KnownFoldersViewModel = New<KnownFoldersViewModel>();
    }

    public void Initialized()
    {
        _mainViewModel = _logOnViewModel.MainViewModel;
        _fileOperationViewModel = _logOnViewModel.FileOperationViewModel;
        _fileSystemState = Resolve.FileSystemState;

        _mainViewModel.BindPropertyAsyncChanged(nameof(_mainViewModel.License), async (LicenseCapabilities license) => { await ConfigureKeyShareMenusAsync(license); });

        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.EncryptFileEnabled), (bool enabled) => { EncryptButtonEnabled = enabled; });
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.FilesArePending), (bool areFilesPending) => { IsFilesPending = areFilesPending; });

        KnownFoldersViewModel.BindPropertyChanged(nameof(KnownFoldersViewModel.KnownFolders), (IEnumerable<KnownFolder> folders) => UpdateKnownFolders(folders));
        KnownFoldersViewModel.KnownFolders = New<IKnownFoldersDiscovery>().Discover();
    }

    public SubscriptionLevel SubscriptionLevel
    {
        get
        {
            return _logOnViewModel.SubscriptionLevel;
        }
    }

    public async Task OpenFile()
    {
        await _fileOperationViewModel.OpenFilesFromFolder.ExecuteAsync(string.Empty);
    }

    public async Task SecureFile()
    {
        await _fileOperationViewModel.EncryptFiles.ExecuteAsync(null);
    }

    public async Task StopSecuringFile()
    {
        await _fileOperationViewModel.DecryptFiles.ExecuteAsync(_mainViewModel!.SelectedRecentFiles);
    }

    public async void ShareKeysAsync(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, async (ss, ee) => { await ShareKeyService.ShareKeysWithFileSelectionAsync(_sharekeyViewModel, _mainViewModel!.SelectedRecentFiles, _fileOperationViewModel); }, null, e);
    }

    public async void CleanAndRemoveOpenFilesButton_Click(EventArgs e)
    {
        await EncryptPendingFiles();
    }

    private async Task ConfigureKeyShareMenusAsync(LicenseCapabilities license)
    {
        if (license.Has(LicenseCapability.KeySharing))
        {
            KeyShareButtonEnabled = true;
        }
        else
        {
            KeyShareButtonEnabled = false;
        }
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

    private void UpdateKnownFolders(IEnumerable<KnownFolder> folders)
    {
        foreach (KnownFolder folder in folders)
        {
            GetIconClass(folder.My.FullName);
        }
    }

    public string GetIconClass(string displayName)
    {
        return displayName.ToLower() switch
        {
            "onedrive" => "onedrv-icon",
            "documents" => "cld-icon",
            "google drive" => "ggldrv-icon",
            "dropbox" => "drpbx-icon",
            _ => "default-icon"
        };
    }

    public async Task OnCloudServiceButtonClick(KnownFolder knownFolder)
    {
        await _fileOperationViewModel.OpenFilesFromFolder.ExecuteAsync(knownFolder.My.FullName);
    }

    public async void RandomRenameAsync(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.RandomRename, async (ss, ee) => { await _fileOperationViewModel.RandomRenameFiles.ExecuteAsync(null); }, null, e);
    }

    public async void SecureWipeFiles(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.SecureWipe, async (ss, ee) => { await _fileOperationViewModel.WipeFiles.ExecuteAsync(null); }, null, e);
    }

    public async void EncryptionUpgrade()
    {
        await _fileOperationViewModel.AsyncEncryptionUpgrade.ExecuteAsync(null);
    }

    public void AlwaysOfflineForFreeUser()
    {
        bool alwaysOnline = !New<UserSettings>().OfflineMode;
        New<UserSettings>().OfflineMode = alwaysOnline;
        New<AxCryptOnlineState>().IsOffline = alwaysOnline;

        string alert = alwaysOnline ? "Offline mode is enabled." : "Offline mode is disabled.";
        _statusAlertService.Success(alert);
    }

    public async void InviteUser(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, async (ss, ee) => { _logOnViewModel.InviteDialog.Show(); }, null, e);
    }

    public void UpgradeDialog()
    {
        _logOnViewModel.UpgradeDialog.Show();
    }

    public async void RedirectToAccountWebUrl()
    {
        LogOnIdentity identity = New<KnownIdentities>().DefaultEncryptionIdentity;
        string tag = string.Empty;
        if (New<KnownIdentities>().IsLoggedOn)
        {
            IAccountService accountService = New<LogOnIdentity, IAccountService>(identity);
            tag = (await accountService.AccountAsync()).Tag ?? string.Empty;
        }

        BrowseUtility.RedirectToPurchasePage(identity.UserEmail.Address, true, tag);
    }

    public void RedirectToAccountSite()
    {
        New<Abstractions.IBrowser>().OpenUri(new Uri("https://account.axcrypt.net/"));
    }

    public void BuyForSomeoneElseLink()
    {
        New<Abstractions.IBrowser>().OpenUri(new Uri("https://account.axcrypt.net/en/Premium/CreateSubscription"));
    }

    public void ChangeSubscriptionToBusinessLink()
    {
        New<Abstractions.IBrowser>().OpenUri(new Uri("https://account.axcrypt.net/en/HomeBusiness/CreateSubscription"));
    }

    private async Task PremiumFeature_ClickAsync(LicenseCapability requiredCapability, Func<object, EventArgs, Task> realHandler, object sender, EventArgs e)
    {
        if (_mainViewModel.License.Has(requiredCapability))
        {
            if (realHandler != null)
            {
                await realHandler(sender, e);
            }
            return;
        }

        _logOnViewModel.UpgradeDialog.Show();
    }
}