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
using AxCrypt.App.Components.Services;

using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.App.Components.Data;
using AxCrypt.Content;

namespace AxCrypt.App.Windows.ViewModels;

public class HomeBodyViewModel : ComponentBase
{
    private FileOperationViewModel? _fileOperationViewModel;
    private MainViewModel? _mainViewModel;
    public KnownFoldersViewModel? _knownFoldersViewModel { get; set; }

    private FileSystemState? _fileSystemState;

    [Parameter]
    public IEnumerable<string> SelectedFilesOrFoldersList { get; set; } = new List<string>();
    
    [Parameter]
    public bool IsFolder { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public ObservableCollection<FileDetails> SelectedRecentFiles { get; set; } = new ObservableCollection<FileDetails>();

    [Inject]
    private IFolderPicker FolderPicker { get; set; }

    public event EventHandler<FileSelectionEventArgs> SelectingFiles;

    public bool isFilesPending = false;

    public SubscriptionLevel SubscriptionLevel { get; set; }

    public bool ShowInvitePopup { get; set; }

    public void OpenPopup()
    {
        ShowInvitePopup = !ShowInvitePopup;
    }

    public int MembersCount { get; set; }
    public int TotalMembers { get; set; }
    public string? DisplayName { get; set; }
    public bool IsEnabled { get; set; }
    public string? ImageSource { get; set; }
    public string? EnabledBackColor { get; set; }
    public string? DisabledBackColor { get; set; }
    public bool isHovered = false;
    public string hoveredElement = string.Empty;

    public void ShowPopup(string element)
    {
        isHovered = true;
        hoveredElement = element;
    }

    public bool IsHovered { get; set; }

    public void HidePopup()
    {
        isHovered = false;
        hoveredElement = string.Empty;
    }

    public bool IsPopupVisible { get; set; }
    public bool ActiveSubScription { get; set; }
    public string UserEmail { get; set; }
    public int DaysLeft { get; set; }
    public bool SubscribedFromAppStore { get; set; }
    public string SubscriptionStatusText { get; set; }
    public bool ShowConfirmDeleteAccountPopup { get; set; }
    public bool shareKey = false;
    public bool ShowSharePopup { get; set; }
    public bool UpgradePopup = false;
    public bool UpgradeToEncrypt = false;
    public bool UpgradeToShare = false;
    public bool showUpgradePopup = false;
    public string? ErrorMessage { get; set; }

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

        selectedFiles = await FolderPicker.PickMultipleAsync(knownFolder.My.FullName, fileSelectionEventArgs);

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

    public List<string> SelectedShareKeyFiles { get; set; }

    private async Task ShareKeysWithFileSelectionAsync(IEnumerable<FileDetails> selectedRecentFiles = null)
    {
        List<string> filesList = selectedRecentFiles?.Select(f => f.FilePath).ToList();
        IEnumerable<string> encryptableFileNames = filesList.Where(f => New<IDataStore>(f).IsEncryptable());
        if (encryptableFileNames != null && encryptableFileNames.Any())
        {
            PopupButtons click = await New<IPopup>().ShowAsync(PopupButtons.OkCancel, Texts.InformationTitle, "There are some unencrypted files also selected for key sharing. AxCrypt will encrypt and then key share the selected files. Would you like to continue to proceed?");
            if (click != PopupButtons.Ok)
            {
                return;
            }
        }

        IEnumerable<FileResult> selectedFiles = selectedRecentFiles?.Select(f => new FileResult(f.FilePath)).ToList();

        if (selectedFiles == null || !selectedFiles.Any())
        {
            FileSelectionEventArgs fileSelectionEventArgs = new FileSelectionEventArgs(new string[0])
            {
                FileSelectionType = FileSelectionType.KeySharingEncrypt
            };

            selectedFiles = await InternalFileSelectionAsync(fileSelectionEventArgs);

            if (fileSelectionEventArgs.Cancel)
            {
                return;
            }

            filesList = selectedFiles.Select(e => e.FullPath).ToList();
        }

        SharingListViewModel sharingListViewModel = await SharingListViewModel.CreateForFilesAsync(filesList, New<KnownIdentities>().DefaultEncryptionIdentity);
        FileShareService FileShareService = new FileShareService();
        FileShareService.SetSelectedFilesOrFolders(selectedFiles.Select(e => e.FullPath), sharingListViewModel);
        SelectedShareKeyFiles = filesList;

        if (encryptableFileNames != null && encryptableFileNames.Any())
        {
            _fileOperationViewModel.Recipients = sharingListViewModel.SharedWith;
            await _fileOperationViewModel.EncryptFiles.ExecuteAsync(encryptableFileNames);
            _fileOperationViewModel.Recipients = null;
        }

        await sharingListViewModel.ShareFiles.ExecuteAsync(null);

        ShowSharePopup = true;
        //StateHasChanged();
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

            case FileSelectionType.Wipe:
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
                await _fileOperationViewModel.WipeFiles.ExecuteAsync(file);
            }
        }
    }

    public async void UpgradeTo256()
    {
        string folder = await FolderPicker.PickFolderAsync();
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
