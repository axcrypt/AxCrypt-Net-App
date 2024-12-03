using AxCrypt.Api.Model;
using AxCrypt.App.Components.ViewModels;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Service;
using AxCrypt.Core;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using Microsoft.AspNetCore.Components;
using System.Collections.ObjectModel;
using AxCrypt.Common;
using AxCrypt.Core.Extensions;
using AxCrypt.App.Components.Services.Interface;
using AxCrypt.Content;
using AxCrypt.App.Components.Models;

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

    [Parameter]
    public ObservableCollection<FileDetails> SelectedRecentFiles { get; set; } = new ObservableCollection<FileDetails>();
    public KnownFoldersViewModel? KnownFoldersViewModel { get; set; }

    public int MembersCount { get; set; }
    public int TotalMembers { get; set; }
    public string? DisabledBackColor { get; set; }
    public bool IsFilesPending { get; set; }

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
        if (SelectedRecentFiles.Any())
        {
            await _fileOperationViewModel.OpenFiles.ExecuteAsync(SelectedRecentFiles.Select(fi => fi.FilePath));
            return;
        }

        await _fileOperationViewModel.OpenFiles.ExecuteAsync(null);
    }

    public async Task SecureFile()
    {
        if (SelectedRecentFiles.Any())
        {
            await _fileOperationViewModel.EncryptFiles.ExecuteAsync(SelectedRecentFiles.Select(fi => fi.FilePath));
            return;
        }

        await _fileOperationViewModel.EncryptFiles.ExecuteAsync(null);
    }

    public async Task StopSecuringFile()
    {
        if (SelectedRecentFiles.Any())
        {
            await _fileOperationViewModel.DecryptFiles.ExecuteAsync(SelectedRecentFiles.Select(fi => fi.FilePath));
            return;
        }

        await _fileOperationViewModel.DecryptFiles.ExecuteAsync(null);
    }

    public async void ShareKeysAsync(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, async (ss, ee) => { await ShareKeysWithFileSelectionAsync(SelectedRecentFiles); }, null, e);
    }

    private async Task ShareKeysWithFileSelectionAsync(IEnumerable<FileDetails> fileNames)
    {
        IEnumerable<string> selectedRecentFiles = fileNames?.Select(f => f.FilePath) ?? Enumerable.Empty<string>();

        if (selectedRecentFiles.Count() == 0)
        {
            IEnumerable<FileResult> pickResult = await FilePicker.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Please select files",
            });

            selectedRecentFiles = pickResult?.Select(f => f.FullPath).ToList() ?? Enumerable.Empty<string>();

            if (!selectedRecentFiles.Any())
            {
                return;
            }
        }

        IEnumerable<string> encryptableFileNames = selectedRecentFiles.Where(f => New<IDataStore>(f).IsEncryptable());
        if (encryptableFileNames != null && encryptableFileNames.Any())
        {
            PopupButtons click = await New<IPopup>().ShowAsync(PopupButtons.OkCancel, Texts.InformationTitle, "There are some unencrypted files also selected for key sharing. AxCrypt will encrypt and then key share the selected files. Would you like to continue to proceed?");
            if (click != PopupButtons.Ok)
            {
                return;
            }
        }

        IEnumerable<string> encryptedFileNames = selectedRecentFiles.Where(f => New<IDataStore>(f).IsEncrypted());
        SharingListViewModel sharingListViewModel = await SharingListViewModel.CreateForFilesAsync(encryptedFileNames, New<KnownIdentities>().DefaultEncryptionIdentity);
        _sharekeyViewModel.SetSelectedFilesOrFolders(encryptedFileNames, sharingListViewModel);

        if (encryptableFileNames != null && encryptableFileNames.Any())
        {
            _fileOperationViewModel.Recipients = sharingListViewModel.SharedWith;
            await _fileOperationViewModel.EncryptFiles.ExecuteAsync(encryptableFileNames);
            _fileOperationViewModel.Recipients = null;
        }

        await sharingListViewModel.ShareFiles.ExecuteAsync(null);
        SelectedRecentFiles.Clear();
    }

    public async void CleanAndRemoveOpenFilesButton_Click(EventArgs e)
    {
        await EncryptPendingFiles();
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
