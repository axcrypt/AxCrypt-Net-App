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

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.ViewModels;

public class HomeBodyViewModel : ComponentBase
{
    private FileOperationViewModel _fileOperationViewModel;
    private MainViewModel? _mainViewModel;
    private FileSystemState? _fileSystemState;
    private IFolderPicker? _folderPicker;

    [Inject]
    public FileShareService? FileShareService { get; set; }

    private ShareKeyViewModel? _shareKeyViewModel { get; set; }
    public ShareKeyViewModel? ShareKeyViewModel
    {
        get
        {
            return _shareKeyViewModel;
        }
        set
        {
            _shareKeyViewModel = value;
            OnSharingListStateChanged?.Invoke();
        }
    }

    [Parameter]
    public IEnumerable<string> SelectedFilesOrFoldersList { get; set; } = new List<string>();

    [Parameter]
    public bool? IsFolder { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public ObservableCollection<FileDetails> SelectedRecentFiles { get; set; } = new ObservableCollection<FileDetails>();

    public KnownFoldersViewModel? _knownFoldersViewModel { get; set; }

    public event EventHandler<FileSelectionEventArgs>? SelectingFiles;

    public string? DisplayName { get; set; }
    public bool IsEnabled { get; set; }
    public string? ImageSource { get; set; }
    public string? EnabledBackColor { get; set; }
    public bool IsHovered { get; set; }
    public bool IsPopupVisible { get; set; }
    public bool ActiveSubScription { get; set; }
    public string? UserEmail { get; set; }
    public int DaysLeft { get; set; }
    public bool SubscribedFromAppStore { get; set; }
    public string? SubscriptionStatusText { get; set; }
    public bool ShowConfirmDeleteAccountPopup { get; set; }

    public SubscriptionLevel SubscriptionLevel { get; set; }
    public bool ShowInvitePopup { get; set; }
    public int MembersCount { get; set; }
    public int TotalMembers { get; set; }
    public string? DisabledBackColor { get; set; }
    public bool ShowSharePopup { get; set; }
    public string? ErrorMessage { get; set; }
    public bool UpgradePopup { get; set; }
    public bool UpgradeToEncrypt { get; set; }
    public bool UpgradeToShare { get; set; }
    public bool showUpgradePopup { get; set; }
    public bool shareKey { get; set; }

    public string hoveredElement { get; set; } = string.Empty;
    public bool isHovered { get; set; }
    public bool isFilesPending { get; set; }

    public void OpenPopup()
    {
        ShowInvitePopup = !ShowInvitePopup;
    }

    public void ShowPopup(string element)
    {
        isHovered = true;
        hoveredElement = element;
    }

    public void HidePopup()
    {
        isHovered = false;
        hoveredElement = string.Empty;
    }

    public void Initialized()
    {
        _mainViewModel = New<MainViewModel>();
        _fileOperationViewModel = New<FileOperationViewModel>();
        _knownFoldersViewModel = New<KnownFoldersViewModel>();
        _fileSystemState = Resolve.FileSystemState;
        Utility.OnIsMainMenuHiddenChanged += StateHasChanged;
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.RecentFiles), (IEnumerable<ActiveFile> files) => { UpdateRecentFiles(files); });
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.FilesArePending), (bool areFilesPending) => { AreFilesPending(); /*StateHasChanged();*/ });

        SubscriptionLevel = New<AxCrypt.App.Components.Models.AccountStatusViewModel>().SubscriptionLevel;
        _folderPicker = new FolderPickerWindows();
        FileShareService = new FileShareService();
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

    public void CloseSharePopup()
    {
        ShowSharePopup = false;
    }

    public async Task OnCloudServiceButtonClick(KnownFolder knownFolder)
    {
        IEnumerable<string> filesList = new List<string>();
        IEnumerable<FileResult> selectedFiles = new List<FileResult>();
        FileSelectionEventArgs fileSelectionEventArgs = new FileSelectionEventArgs(new string[] { knownFolder.My.FullName })
        {
            FileSelectionType = FileSelectionType.Open
        };

        selectedFiles = await _folderPicker.PickMultipleAsync(knownFolder.My.FullName, fileSelectionEventArgs);

        if (!selectedFiles.Any())
        {
            return;
        }

        filesList = selectedFiles.Select(e => e.FullPath).ToList();

        await _fileOperationViewModel.OpenFilesFromFolder.ExecuteAsync(filesList);
    }

    public void BuyForSomeoneElseLink()
    {
        New<Abstractions.IBrowser>().OpenUri(new Uri("https://account.axcrypt.net/en/Premium/CreateSubscription"));
    }

    public void ChangeSubscriptionToBusinessLink()
    {
        New<Abstractions.IBrowser>().OpenUri(new Uri("https://account.axcrypt.net/en/HomeBusiness/CreateSubscription"));
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

    public async void HandleSelectingFiles(object sender, FileSelectionEventArgs fileSelectionEventArgs)
    {
        IEnumerable<FileResult> pickResult = await InternalFileSelectionAsync(fileSelectionEventArgs);

        if (pickResult.Any())
        {
            fileSelectionEventArgs.SelectedFiles = pickResult.Select(file => file.FullPath).ToArray();

            if (!fileSelectionEventArgs.Cancel)
            {
                OnFileSelection(fileSelectionEventArgs.FileSelectionType, fileSelectionEventArgs);
            }
        }
    }

    public void ExecuteSelectFilesCommand(FileSelectionType fileSelectionType, ObservableCollection<FileDetails> selectedFiles)
    {
        if (!selectedFiles.Any())
        {
            ExecuteSelectFilesCommand(fileSelectionType);
        }
        FileSelectionEventArgs fileSelectionEventArgs = new FileSelectionEventArgs(new string[0])
        {
            FileSelectionType = fileSelectionType,
            SelectedFiles = selectedFiles.Select(file => file.FilePath).ToArray()
        };

        OnFileSelection(fileSelectionType, fileSelectionEventArgs);
    }

    private void ExecuteSelectFilesCommand(FileSelectionType fileSelectionType)
    {
        FileSelectionEventArgs fileSelectionArgs = new FileSelectionEventArgs(new string[0])
        {
            FileSelectionType = fileSelectionType,
        };
        OnSelectingFiles(fileSelectionArgs);
        if (fileSelectionArgs.Cancel)
        {
            new FileSelectionEventArgs(new List<string>());
        }
    }

    protected virtual void OnSelectingFiles(FileSelectionEventArgs e)
    {
        SelectingFiles?.Invoke(this, e);
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
            case FileSelectionType.WipeConfirm:
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

    private async void OnFileSelection(FileSelectionType fileSelectionType, FileSelectionEventArgs args)
    {
        if (!args.SelectedFiles.Any())
        {
            return;
        }

        IEnumerable<string> selectedFiles = args.SelectedFiles;

        switch (fileSelectionType)
        {
            case FileSelectionType.Open:
                await _fileOperationViewModel.OpenFiles.ExecuteAsync(selectedFiles);
                StateHasChanged();
                break;

            case FileSelectionType.Encrypt:
                await _fileOperationViewModel.EncryptFiles.ExecuteAsync(selectedFiles);
                StateHasChanged();
                break;

            case FileSelectionType.Decrypt:
                await _fileOperationViewModel.DecryptFiles.ExecuteAsync(selectedFiles);
                StateHasChanged();
                break;
        }
    }

    public async void ShareKeysAsync(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, async (ss, ee) => { await ShareKeysWithFileSelectionAsync(SelectedRecentFiles); }, null, e);
    }

    public IEnumerable<string>? SelectedShareKeyFiles { get; set; }
    public SharingListViewModel SharingListViewModel { get; set; }

    private async Task ShareKeysWithFileSelectionAsync(IEnumerable<FileDetails> selectedRecentFiles)
    {
        IEnumerable<FileResult> selectedFiles = await GetFilesToProcessAsync(selectedRecentFiles);
        IEnumerable<string> filesList = selectedFiles.Select(e => e.FullPath).ToList();

        if (await HasUnencryptedFilesAsync(filesList))
        {
            return;
        }

        await ProcessFileSharingAsync(filesList);
    }

    private async Task<IEnumerable<FileResult>> GetFilesToProcessAsync(IEnumerable<FileDetails> selectedRecentFiles)
    {
        if (selectedRecentFiles != null && selectedRecentFiles.Any())
        {
            return selectedRecentFiles?.Select(f => new FileResult(f.FilePath)).ToList();
        }

        return await PromptUserFileSelectionAsync();
    }

    private async Task<IEnumerable<FileResult>> PromptUserFileSelectionAsync()
    {
        FileSelectionEventArgs fileSelectionEventArgs = new FileSelectionEventArgs(new string[0])
        {
            FileSelectionType = FileSelectionType.KeySharingEncrypt
        };

        IEnumerable<FileResult> selectedFiles = await InternalFileSelectionAsync(fileSelectionEventArgs);

        return fileSelectionEventArgs.Cancel ? Enumerable.Empty<FileResult>() : selectedFiles;
    }

    private async Task<bool> HasUnencryptedFilesAsync(IEnumerable<string> filesList)
    {
        IEnumerable<string> encryptableFileNames = filesList.Where(f => New<IDataStore>(f).IsEncryptable());

        if (encryptableFileNames.Any())
        {
            PopupButtons click = await New<IPopup>().ShowAsync(PopupButtons.OkCancel, Texts.InformationTitle, "There are some unencrypted files also selected for key sharing. AxCrypt will encrypt and then key share the selected files. Would you like to continue to proceed?");
            return click != PopupButtons.Ok;
        }

        return false;
    }

    public event Action? OnSharingListStateChanged;

    private async Task ProcessFileSharingAsync(IEnumerable<string> filesList)
    {
        SharingListViewModel sharingListViewModel = await SharingListViewModel.CreateForFilesAsync(filesList, New<KnownIdentities>().DefaultEncryptionIdentity);
        FileShareService = new FileShareService();
        FileShareService.SetSelectedFilesOrFolders(filesList, sharingListViewModel);
        SharingListViewModel = sharingListViewModel;
        SelectedShareKeyFiles = filesList;

        IEnumerable<string> encryptableFileNames = filesList.Where(f => New<IDataStore>(f).IsEncryptable());
        if (encryptableFileNames.Any())
        {
            _fileOperationViewModel.Recipients = sharingListViewModel.SharedWith;
            await _fileOperationViewModel.EncryptFiles.ExecuteAsync(encryptableFileNames);
            _fileOperationViewModel.Recipients = null;
        }

        //await sharingListViewModel.ShareFiles.ExecuteAsync(null);

        ShowSharePopup = true;
        OnSharingListStateChanged?.Invoke();
    }

    public async void CloseAndRemoveOpenFilesButton_Click(EventArgs e)
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

    public ObservableCollection<FileDetails> RecentFilesList { get; set; } = new ObservableCollection<FileDetails>();

    private void UpdateRecentFiles(IEnumerable<ActiveFile> files)
    {
        RecentFilesList = new ObservableCollection<FileDetails>(files.Select(f => new FileDetails(f)));
        //StateHasChanged();
    }

    public void Dispose()
    {
        Utility.OnIsMainMenuHiddenChanged -= StateHasChanged;
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

    public async void RandomRenameAsync(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.RandomRename, async (ss, ee) => { await FileSelectionOperation(FileSelectionType.Rename); }, null, e);
    }

    public async void SecureWipeFiles(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.SecureWipe, async (ss, ee) => { await FileSelectionOperation(FileSelectionType.WipeConfirm); }, null, e);
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

        showUpgradePopup = true;
    }

    public async Task FileSelectionOperation(FileSelectionType fileSelectionType)
    {
        FileSelectionEventArgs fileSelectionEventArgs = new FileSelectionEventArgs(new string[0])
        {
            FileSelectionType = fileSelectionType
        };

        IEnumerable<FileResult> selectedFiles = await InternalFileSelectionAsync(fileSelectionEventArgs);

        if (fileSelectionEventArgs.Cancel)
        {
            return;
        }

        fileSelectionEventArgs.SelectedFiles = selectedFiles.Select(e => e.FullPath).ToList();

        HandleFileOperation(fileSelectionType, fileSelectionEventArgs);
    }

    private async void HandleFileOperation(FileSelectionType fileSelectionType, FileSelectionEventArgs e)
    {
        switch (fileSelectionType)
        {
            case FileSelectionType.Rename:
                HandleRandomRename(e);
                break;

            case FileSelectionType.WipeConfirm:
                await HandleWipeConfirm(e);
                break;
        }
    }

    private async void HandleRandomRename(FileSelectionEventArgs e)
    {
        IList<string> files = e.SelectedFiles;
        await _fileOperationViewModel.RandomRenameFiles.ExecuteAsync(files);
    }

    private async Task HandleWipeConfirm(FileSelectionEventArgs e)
    {
        IList<string> selectedFiles = e.SelectedFiles;
        foreach (string file in selectedFiles)
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert("Confirm Wipe", "Are you sure you want to permanently wipe the selected file?", "Yes", "No");
            if (confirm)
            {
                await _fileOperationViewModel.WipeFiles.ExecuteAsync(new List<string> { file });
            }
        }
    }

    public async void UpgradeTo256()
    {
        string folder = await _folderPicker.PickFolderAsync();
        if (folder == null)
        {
            return;
        }

        IEnumerable<IDataContainer> dataContainers = new List<IDataContainer> { New<IDataContainer>(folder) };
        await _fileOperationViewModel.AsyncEncryptionUpgrade.ExecuteAsync(dataContainers);
    }

    private bool alwaysOnline;

    public bool AlwaysOffline
    {
        get => alwaysOnline;
        set
        {
            if (alwaysOnline != value)
            {
                alwaysOnline = value;
                AlwaysOfflineForFreeUser();
            }
        }
    }

    public void AlwaysOfflineForFreeUser()
    {
        AlwaysOffline = !New<UserSettings>().OfflineMode;
        ErrorMessage = AlwaysOffline ? "Offline mode is now enabled." : "Offline mode is now disabled.";
        New<IPopup>().ShowAsync(PopupButtons.Ok, "Success", ErrorMessage);
    }
}
