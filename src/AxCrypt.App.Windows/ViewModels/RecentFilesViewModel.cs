using AxCrypt.App.Components.Models;
using AxCrypt.App.Components.Services.Interface;
using AxCrypt.App.Components.Services;
using AxCrypt.App.Components.ViewModels;
using AxCrypt.Content;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Core.UI;
using AxCrypt.Core;
using Microsoft.AspNetCore.Components;
using System.Collections.ObjectModel;
using AxCrypt.App.Components.Utility.View;
using AxCrypt.Core.IO;
using Microsoft.AspNetCore.Components.Web;

using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.Api.Model;

namespace AxCrypt.App.Windows.ViewModels;

internal class RecentFilesViewModel : ComponentBase
{
    private ProcessIndicatorService _ProcessIndicatorService;

    [Inject]
    public FileShareService FileShareService { get; set; }

    [Inject]
    public IFolderPicker FolderPicker { get; set; }

    public KnownFoldersViewModel _knownFoldersViewModel;

    public RecentFilesViewModel()
    {
        _mainViewModel = New<MainViewModel>();
        _mainViewModel.LoggedOn = Resolve.KnownIdentities.IsLoggedOn;
        _fileOperationViewModel = New<FileOperationViewModel>();
    }

    private MainViewModel _mainViewModel;
    private FileOperationViewModel _fileOperationViewModel;

    public void OnInitializedAsync()
    {
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.RecentFiles), (IEnumerable<ActiveFile> files) => { UpdateRecentFiles(files); });
    }

    private ObservableCollection<FileDetails> _recentFilesList = new ObservableCollection<FileDetails>();

    public ObservableCollection<FileDetails> RecentFilesList
    {
        get
        {
            return _recentFilesList;
        }
        set
        {
            _recentFilesList = value;
            OnRecentFilesStateChanged?.Invoke();
        }
    }

    public ObservableCollection<FileDetails> SelectedFiles = new ObservableCollection<FileDetails>();

    private FileDetails SelectedFile = new FileDetails();

    public event Action? OnRecentFilesStateChanged;

    public bool IsHeaderCheckboxChecked { get; set; } = false;

    public void ToggleAllCheckboxes(ChangeEventArgs e)
    {
        if (SelectedFiles.Any())
        {
            SelectedFiles.Clear();
        }

        IsHeaderCheckboxChecked = (bool)e.Value;

        if (!IsHeaderCheckboxChecked)
        {
            return;
        }

        foreach (FileDetails fileDetails in RecentFilesList)
        {
            fileDetails.IsChecked = IsHeaderCheckboxChecked;
            SelectedFiles.Add(fileDetails);
        }

        OnRecentFilesStateChanged?.Invoke();
    }

    public void SelectSingleFile(ChangeEventArgs e, FileDetails selectedFile)
    {
        bool isChecked = (bool)e.Value;
        selectedFile.IsChecked = isChecked;
        if (!isChecked)
        {
            SelectedFiles.Remove(selectedFile);
            OnRecentFilesStateChanged?.Invoke();
            return;
        }

        SelectedFiles.Add(selectedFile);
        OnRecentFilesStateChanged?.Invoke();
    }

    public void HandleFileClick(MouseEventArgs e, FileDetails fileDetails)
    {
        ContextMenu = false;
        if (e.Button == 0)
        {
            if (SelectedFiles.Contains(fileDetails))
            {
                SelectedFiles.Remove(fileDetails);
            }
            else
            {
                SelectedFiles.Add(fileDetails);
            }
        }

        else if (e.Button == 2)
        {
            fileDetails.IsChecked = true;
            SelectedFiles.Add(fileDetails);
        }
    }

    public bool ContextMenu { get; set; } = false;

    public void HandleContextMenu(MouseEventArgs e, FileDetails fileDetails)
    {
        try
        {
            if (!SelectedFiles.Contains(fileDetails))
            {
                SelectedFiles.Add(fileDetails);
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public SubscriptionLevel SubscriptionLevel { get; set; }


    public bool Upgradepop = false;

    public List<string> SelectedShareKeyFiles { get; set; }

    public bool ShowSharePopup { get; set; }

    public void ClosePopup()
    {
        ShowSharePopup = false;
    }

    public bool ShowInvitePopup { get; set; }
    public void OpenPopup()
    {
        ShowInvitePopup = !ShowInvitePopup;
    }

    public bool isNameAscending { get; set; }
    public bool isSizeAscending { get; set; }
    public bool isDateModifiedAscending { get; set; }

    public void SortByName()
    {
        if (isNameAscending)
        {
            RecentFilesList = new ObservableCollection<FileDetails>(RecentFilesList.OrderBy(f => f.FileName).ToList());
        }
        else
        {
            RecentFilesList = new ObservableCollection<FileDetails>(RecentFilesList.OrderByDescending(f => f.FileName).ToList());
        }
        isNameAscending = !isNameAscending;
    }

    public void SortBySize()
    {
        if (isSizeAscending)
        {
            RecentFilesList = new ObservableCollection<FileDetails>(RecentFilesList.OrderBy(f => f.FileSize).ToList());
        }
        else
        {
            RecentFilesList = new ObservableCollection<FileDetails>(RecentFilesList.OrderByDescending(f => f.FileSize).ToList());
        }
        isSizeAscending = !isSizeAscending;
    }

    public void SortByDateModified()
    {
        if (isDateModifiedAscending)
        {
            RecentFilesList = new ObservableCollection<FileDetails>(RecentFilesList.OrderBy(f => f.LastModifiedDate).ToList());
        }
        else
        {
            RecentFilesList = new ObservableCollection<FileDetails>(RecentFilesList.OrderByDescending(f => f.LastModifiedDate).ToList());
        }
        isDateModifiedAscending = !isDateModifiedAscending;
    }

    private void UpdateRecentFiles(IEnumerable<ActiveFile> files)
    {
        RecentFilesList = new ObservableCollection<FileDetails>(files.Select(f => new FileDetails(f)));
    }

    public void UpgradeSubscription()
    {
        New<Abstractions.IBrowser>().OpenUri(new Uri("https://axcrypt.net/pricing/"));
    }

    public bool showUpgradePopup = false;

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

    public async Task OnContextMenuAction(EventArgs args, SecuredFilesContextMenu securedFilesContextMenu)
    {
        switch (securedFilesContextMenu)
        {
            case SecuredFilesContextMenu.OpenSecured:
                OpenSecured();
                break;

            case SecuredFilesContextMenu.RemoveFromListButKeepSecured:
                RemoveFromListKeepSecured();
                break;

            case SecuredFilesContextMenu.StopSecureAndRemoveFromList:
                DecryptAndRemoveFromList();
                break;

            case SecuredFilesContextMenu.ShareKey:
                ShareKeyFromRecentFiles(args);
                break;

            case SecuredFilesContextMenu.ShowInFolder:
                ShowInFolder();
                break;

            case SecuredFilesContextMenu.RenameToOriginal:
                await RestoreToOriginalRecentFiles(args);
                break;

            case SecuredFilesContextMenu.ClearRecentFiles:
                ClearAllRecentFiles();
                break;
        }
    }

    private async void OpenSecured()
    {
        await _fileOperationViewModel.OpenFiles.ExecuteAsync(SelectedFiles.Select(f => f.FilePath));
    }

    public async void OpenSecuredRecentFiles(EventArgs args, IEnumerable<FileDetails> selectedFiles)
    {
        await _fileOperationViewModel.OpenFiles.ExecuteAsync(selectedFiles.Select(f => f.FilePath));
    }

    private async void RemoveFromListKeepSecured()
    {
        await _mainViewModel.RemoveRecentFiles.ExecuteAsync(SelectedFiles.Select(f => f.FilePath));
    }

    private async void DecryptAndRemoveFromList()
    {
        await _fileOperationViewModel.DecryptFiles.ExecuteAsync(SelectedFiles.Select(f => f.FilePath));
    }

    private async void ShareKeyFromRecentFiles(EventArgs args)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, async (ss, ee) => { await ShareKeysWithFileSelectionAsync(SelectedFiles.Select(f => f.FilePath).ToList()); }, null, args);
    }

    private async Task ShareKeysWithFileSelectionAsync(IEnumerable<string> selectedRecentFileNames)
    {
        FileSelectionEventArgs fileSelectionArgs = new FileSelectionEventArgs(selectedRecentFileNames)
        {
            FileSelectionType = FileSelectionType.KeySharingEncrypt,
        };

        if (!fileSelectionArgs.SelectedFiles.Any())
        {
            await New<IDataItemSelection>().HandleSelection(fileSelectionArgs);
        }

        if (fileSelectionArgs.Cancel)
        {
            return;
        }

        await ShareKeysAsync(fileSelectionArgs.SelectedFiles);
        OnRecentFilesStateChanged?.Invoke();
    }

    private async Task ShareKeysAsync(IEnumerable<string> fileNames)
    {
        IEnumerable<string> encryptableFileNames = fileNames.Where(f => New<IDataStore>(f).IsEncryptable());
        if (encryptableFileNames != null && encryptableFileNames.Any())
        {
            PopupButtons click = await New<IPopup>().ShowAsync(PopupButtons.OkCancel, Texts.InformationTitle, "There are some unencrypted files also selected for key sharing. AxCrypt will encrypt and then key share the selected files. Would you like to continue to proceed?");
            if (click != PopupButtons.Ok)
            {
                return;
            }
        }

        IEnumerable<string> encryptedFileNames = fileNames.Where(f => New<IDataStore>(f).IsEncrypted());
        SharingListViewModel viewModel = await SharingListViewModel.CreateForFilesAsync(encryptedFileNames, Resolve.KnownIdentities.DefaultEncryptionIdentity);
        // using (KeyShareDialog dialog = new KeyShareDialog(this, viewModel, fileNames))
        // {
        //     if (dialog.ShowDialog(this) != DialogResult.OK)
        //     {
        //         return;
        //     }
        // }

        FileShareService.SetSelectedFilesOrFolders(fileNames, viewModel);
        SelectedShareKeyFiles = fileNames.Select(f => f).ToList();
        ShowSharePopup = true;

        if (encryptableFileNames != null && encryptableFileNames.Any())
        {
            _fileOperationViewModel.Recipients = viewModel.SharedWith;
            await _fileOperationViewModel.EncryptFiles.ExecuteAsync(encryptableFileNames);
            _fileOperationViewModel.Recipients = null;
        }

        await viewModel.ShareFiles.ExecuteAsync(null);
    }

    private async void ShowInFolder()
    {
        await _fileOperationViewModel.ShowInFolder.ExecuteAsync(SelectedFiles.Select(f => f.FilePath));
    }

    private async Task RestoreToOriginalRecentFiles(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.RandomRename, async (ss, ee) => { await _fileOperationViewModel.RestoreRandomRenameFiles.ExecuteAsync(SelectedFiles.Select(f => f.FilePath)); }, null, e);
    }

    private async void ClearAllRecentFiles()
    {
        await _mainViewModel.RemoveRecentFiles.ExecuteAsync(RecentFilesList.Select(files => files.FilePath));
    }

    public async Task<IList<FileDetails>> LoadRecentFiles()
    {
        using (ProcessIndicator processIndicator = new ProcessIndicator(_ProcessIndicatorService))
        {
            IList<FileDetails> recentFiles = new List<FileDetails>();

            try
            {
                IEnumerable<ActiveFile> files = _mainViewModel.SetRecentFiles();

                if (files == null)
                {
                    return recentFiles;
                }

                recentFiles = new ObservableCollection<FileDetails>(files.Select(f => new FileDetails(f)));

                return recentFiles;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching recent files: {ex.Message}");
                return recentFiles;
            }
        }
    }
}
