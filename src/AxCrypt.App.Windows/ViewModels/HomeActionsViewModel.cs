using AxCrypt.Api.Model;
using AxCrypt.App.Components.Models;
using AxCrypt.App.Components.Services.Interface;
using AxCrypt.App.Windows.Services;
using AxCrypt.Common;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Service;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using Microsoft.AspNetCore.Components;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.ViewModels;

public class HomeActionsViewModel : ComponentBase
{
    private FileOperationViewModel _fileOperationViewModel;
    private MainViewModel? _mainViewModel;

    private IStatusAlertService _statusAlertService;
    private ShareKeyViewModel? _sharekeyViewModel;

    public HomeActionsViewModel(ShareKeyViewModel shareKeyViewModel)
    {
        LogOnViewModel = AxCServiceProvider.LogOnViewModel!;
        _statusAlertService = AxCServiceProvider.StatusAlertService!;

        _mainViewModel = LogOnViewModel.MainViewModel;
        _fileOperationViewModel = LogOnViewModel.FileOperationViewModel;

        _sharekeyViewModel = shareKeyViewModel;
        KnownFoldersViewModel = New<KnownFoldersViewModel>();

        Initialized();
    }

    public void Initialized()
    {
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.License), (LicenseCapabilities license) => { ConfigureKeyShareMenus(license); });

        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.EncryptFileEnabled), (bool enabled) => { EncryptButtonEnabled = enabled; });
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.FilesArePending), (bool areFilesPending) => { IsFilesPending = areFilesPending; });

        KnownFoldersViewModel!.BindPropertyChanged(nameof(KnownFoldersViewModel.KnownFolders), (IEnumerable<KnownFolder> folders) => UpdateKnownFolders(folders));
        KnownFoldersViewModel.KnownFolders = New<IKnownFoldersDiscovery>().Discover();
    }

    public KnownFoldersViewModel? KnownFoldersViewModel { get; set; }

    public string? DisabledBackColor { get; set; }

    public bool IsFilesPending { get; set; }

    public bool EncryptButtonEnabled { get; set; }

    public bool KeyShareButtonEnabled { get; set; }

    public LogOnViewModel LogOnViewModel { get; set; }

    public SubscriptionLevel SubscriptionLevel
    {
        get
        {
            return LogOnViewModel.SubscriptionLevel;
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
        await _fileOperationViewModel.DecryptFiles.ExecuteAsync(_mainViewModel!.SelectedRecentFiles.Any() ? _mainViewModel!.SelectedRecentFiles : null);
    }

    public async void ShareKeysAsync(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, async (ss, ee) => { await ShareKeyService.ShareKeysWithFileSelectionAsync(_sharekeyViewModel, _mainViewModel!.SelectedRecentFiles, _fileOperationViewModel); }, null, e);
    }

    public async void CleanAndRemoveOpenFilesButton_Click(EventArgs e)
    {
        await EncryptPendingFiles();
    }

    private void ConfigureKeyShareMenus(LicenseCapabilities license)
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
        await PremiumFeature_ClickAsync(LicenseCapability.RandomRename, async (ss, ee) => { await _fileOperationViewModel.RandomRenameFiles.ExecuteAsync(_mainViewModel!.SelectedRecentFiles.Any() ? _mainViewModel!.SelectedRecentFiles : null); }, null, e);
    }

    public async void SecureWipeFiles(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.SecureWipe, async (ss, ee) => { await _fileOperationViewModel.WipeFiles.ExecuteAsync(_mainViewModel!.SelectedRecentFiles.Any() ? _mainViewModel!.SelectedRecentFiles : null); }, null, e);
    }

    public async void EncryptionUpgrade(EventArgs e)
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
        await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, async (ss, ee) => { LogOnViewModel.InviteDialog.Show(); }, null, e);
    }

    public void UpgradeDialog()
    {
        LogOnViewModel.UpgradeDialog.Show();
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

    //public void BuyForSomeoneElseLink()
    //{
    //    //New<Abstractions.IBrowser>().OpenUri(new Uri("https://account.axcrypt.net/en/Premium/CreateSubscription"));
    //}

    //public void ChangeSubscriptionToBusinessLink()
    //{
    //    New<Abstractions.IBrowser>().OpenUri(new Uri("https://account.axcrypt.net/en/HomeBusiness/CreateSubscription"));
    //}

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

        LogOnViewModel.UpgradeDialog.Show();
    }
}