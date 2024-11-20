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
using AxCrypt.App.Components.Data;
using AxCrypt.Content;
using AxCrypt.App.Windows.Services;
using AxCrypt.App.Components.Services;
using AxCrypt.App.Components.Models;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.ViewModels;

public class HomeActionsViewModel : ComponentBase
{
    private LogOnViewModel _logOnViewModel;
    private FileOperationViewModel _fileOperationViewModel;
    private MainViewModel? _mainViewModel;
    private FileSystemState? _fileSystemState;
    private IFolderPicker? _folderPicker;

    [Inject]
    public FileShareService? FileShareService { get; set; }

    [Parameter]
    public IEnumerable<string> SelectedFilesOrFoldersList { get; set; } = new List<string>();

    [Parameter]
    public bool? IsFolder { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public ObservableCollection<FileDetails> SelectedRecentFiles { get; set; } = new ObservableCollection<FileDetails>();

    public event EventHandler<FileSelectionEventArgs>? SelectingFiles;

    public SubscriptionLevel SubscriptionLevel { get; set; }
    public bool ShowInvitePopup { get; set; }
    public int MembersCount { get; set; }
    public int TotalMembers { get; set; }
    public string? DisabledBackColor { get; set; }
    public string? ErrorMessage { get; set; }
    public bool UpgradePopup { get; set; }
    public bool UpgradeToEncrypt { get; set; }
    public bool UpgradeToShare { get; set; }
    public bool showUpgradePopup { get; set; }
    public bool shareKey { get; set; }
    public string hoveredElement { get; set; } = string.Empty;
    public bool isHovered { get; set; }
    public bool isFilesPending { get; set; }

    public event Action<bool>? OnSharingListStateChanged;

    private bool _showSharePopup;
    public bool ShowSharePopup
    {
        get => _showSharePopup;
        set
        {
            _showSharePopup = value;
            OnSharingListStateChanged?.Invoke(_showSharePopup);
        }
    }

    public void ShowPopup(string element)
    {
        //isHovered = true;
        //hoveredElement = element;
    }

    public void HidePopup()
    {
        //isHovered = false;
        //hoveredElement = string.Empty;
    }

    public KnownFoldersViewModel? KnownFoldersViewModel { get; set; }
    private IStatusAlertService _statusAlertService;

    public HomeActionsViewModel(LogOnViewModel logOnViewModel, FileShareService fileShareService, IStatusAlertService statusAlertService)
    {
        _logOnViewModel = logOnViewModel;
        FileShareService = fileShareService;
        _statusAlertService = statusAlertService;
        KnownFoldersViewModel = New<KnownFoldersViewModel>();
    }

    public void Initialized()
    {
        _mainViewModel = _logOnViewModel.MainViewModel;
        _fileOperationViewModel = _logOnViewModel.FileOperationViewModel;
        _fileSystemState = Resolve.FileSystemState;
        Utility.OnIsMainMenuHiddenChanged += StateHasChanged;

        SubscriptionLevel = _logOnViewModel.SubscriptionLevel;
        _folderPicker = new FolderPickerWindows();
        FileShareService = new FileShareService();

        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.License), (LicenseCapabilities license) => { UpdateRecentFiles(RecentFilesList); });
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.RecentFiles), (IEnumerable<ActiveFile> files) => { UpdateRecentFiles(files); });

        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.FilesArePending), (bool areFilesPending) => { AreFilesPending(); });

        KnownFoldersViewModel.BindPropertyChanged(nameof(KnownFoldersViewModel.KnownFolders), (IEnumerable<KnownFolder> folders) => UpdateKnownFolders(folders));
        KnownFoldersViewModel.KnownFolders = New<IKnownFoldersDiscovery>().Discover();
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
        await _fileOperationViewModel.DecryptFiles.ExecuteAsync(null);
    }

    public async void ShareKeysAsync(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, async (ss, ee) => { await ShareKeysWithFileSelectionAsync(SelectedRecentFiles); }, null, e);
    }

    public IEnumerable<string>? SelectedShareKeyFiles { get; set; }
    public SharingListViewModel SharingListViewModel { get; set; }

    private async Task ShareKeysWithFileSelectionAsync(IEnumerable<FileDetails> fileNames)
    {
        IEnumerable<string> selectedRecentFiles = new List<string>();
        selectedRecentFiles = fileNames.Select(f => f.FilePath);

        if (selectedRecentFiles == null && !selectedRecentFiles.Any())
        {
            IEnumerable<FileResult> pickResult = await FilePicker.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Please select files",
            });

            if (!pickResult.Any())
            {
                selectedRecentFiles = pickResult.Select(f => f.FullPath).ToList();
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
        FileShareService = new FileShareService();
        FileShareService.SetSelectedFilesOrFolders(encryptedFileNames, sharingListViewModel);
        SharingListViewModel = sharingListViewModel;
        SelectedShareKeyFiles = encryptedFileNames;

        if (encryptableFileNames != null && encryptableFileNames.Any())
        {
            _fileOperationViewModel.Recipients = sharingListViewModel.SharedWith;
            await _fileOperationViewModel.EncryptFiles.ExecuteAsync(encryptableFileNames);
            _fileOperationViewModel.Recipients = null;
        }

        await sharingListViewModel.ShareFiles.ExecuteAsync(null);

        ShowSharePopup = true;
    }

    public void CloseSharePopup()
    {
        ShowSharePopup = false;
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

    public bool AreFilesPending()
    {
        IList<ActiveFile> openFiles = _fileSystemState.DecryptedActiveFiles;
        if (openFiles.Count > 0)
        {
            return isFilesPending = true;
        }

        List<IDataStore> files = new List<IDataStore>();
        foreach (IDataContainer container in Resolve.KnownIdentities.LoggedOnWatchedFolders.Select(wf => New<IDataContainer>(wf.Path)))
        {
            files.AddRange(container.ListOfFiles(_fileSystemState.WatchedFolders.Select(x => New<IDataContainer>(x.Path)), New<UserSettings>().FolderOperationMode.Policy()));
        }
        if (!New<UserSettings>().DoNotShowAgain.HasFlag(DoNotShowAgainOptions.IgnoreFileWarning))
        {
            return files.Where(ds => !ds.IsEncrypted()).Any();
        }

        return isFilesPending = files.Where(ds => ds.IsEncryptable()).Any();
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

    public ObservableCollection<FileDetails> RecentFilesList { get; set; } = new ObservableCollection<FileDetails>();

    private void UpdateRecentFiles(IEnumerable<ActiveFile> files)
    {
        RecentFilesList = new ObservableCollection<FileDetails>(files.Select(f => new FileDetails(f)));
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

    public void Dispose()
    {
        Utility.OnIsMainMenuHiddenChanged -= StateHasChanged;
    }
}
