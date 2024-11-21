using AxCrypt.App.Components.Models;
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
using AxCrypt.Api.Model;
using AxCrypt.Abstractions;
using AxCrypt.Core.Crypto;
using System.Globalization;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.ViewModels;

public class RecentFilesViewModel : ComponentBase
{
    private LogOnViewModel _logOnViewModel;
    private MainViewModel _mainViewModel;
    private FileOperationViewModel _fileOperationViewModel;
    private ProcessIndicatorService _ProcessIndicatorService;

    public RecentFilesViewModel(LogOnViewModel logOnViewModel)
    {
        _logOnViewModel = logOnViewModel;
        _mainViewModel = logOnViewModel.MainViewModel;
        _fileOperationViewModel = logOnViewModel.FileOperationViewModel;
        SubscriptionLevel = logOnViewModel.SubscriptionLevel;
    }

    public void OnInitializedAsync()
    {
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.License), (LicenseCapabilities license) => { UpdateRecentFilesList(_mainViewModel.RecentFiles); });
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.RecentFiles), (IEnumerable<ActiveFile> files) => { UpdateRecentFiles(files); });
    }

    public ShareKeyViewModel? SharekeysViewModel { get; set; }

    public ObservableCollection<FileDetails> RecentFilesList { get; set; }

    public ObservableCollection<FileDetails> SelectedFiles = new ObservableCollection<FileDetails>();

    private FileDetails SelectedFile = new FileDetails();

    public SubscriptionLevel SubscriptionLevel { get; set; }

    public bool IsHeaderCheckboxChecked { get; set; } = false;
    public bool ContextMenu { get; set; } = false;
    public bool isNameAscending { get; set; }
    public bool isSizeAscending { get; set; }
    public bool isDateModifiedAscending { get; set; }

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
    }

    public void SelectSingleFile(ChangeEventArgs e, FileDetails selectedFile)
    {
        bool isChecked = (bool)e.Value;
        selectedFile.IsChecked = isChecked;
        if (!isChecked)
        {
            SelectedFiles.Remove(selectedFile);
            return;
        }

        SelectedFiles.Add(selectedFile);
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
        using (ProcessIndicator processIndicator = new ProcessIndicator(_ProcessIndicatorService))
        {
            if (RecentFilesList != null && files.Count() != RecentFilesList.Count)
            {
                RecentFilesList = new ObservableCollection<FileDetails>(files.Select(f => new FileDetails(f)));
                return;
            }

            if (RecentFilesList == null && files.Any())
            {
                RecentFilesList = new ObservableCollection<FileDetails>();
                RecentFilesList = new ObservableCollection<FileDetails>(files.Select(f => new FileDetails(f)));
                return;
            }

            RecentFilesList = new ObservableCollection<FileDetails>();
        }
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

    public async void OpenSecuredMouseDoubleClick(EventArgs args, IEnumerable<FileDetails> selectedFiles)
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
        SharekeysViewModel.SetSelectedFilesOrFolders(fileNames, viewModel);

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

    private void UpdateRecentFilesList(IEnumerable<ActiveFile> recentFiles)
    {
        if (New<UserSettings>().HideRecentFiles)
        {
            return;
        }

        int i = 0;
        foreach (ActiveFile file in recentFiles)
        {
            try
            {
                RecentFilesList = new ObservableCollection<FileDetails>();
                RecentFilesList.Add(UpdateListViewItem(file));
                ++i;
            }
            catch (Exception ex)
            {
                ex.ReportAndDisplay();
            }
        }
    }

    private FileDetails UpdateListViewItem(ActiveFile activeFile)
    {
        FileDetails fileDetails = new FileDetails();
        fileDetails.FileName = activeFile.DecryptedFileInfo.Name;
        fileDetails.FileSize = activeFile.Size();
        fileDetails.FileExtension = GetFileExtention(activeFile.DecryptedFileInfo.Name);

        fileDetails.FilePath = activeFile.EncryptedFileInfo.FullName;
        fileDetails.LastAccessedDate = activeFile.Properties.LastActivityTimeUtc.ToLocalTime().ToString(CultureInfo.CurrentCulture);
        fileDetails.LastModifiedDate = activeFile.EncryptedFileInfo.LastWriteTimeUtc.ToLocalTime().ToString(CultureInfo.CurrentCulture);

        LogOnIdentity decryptIdentity = ValidateActiveFileIdentity(activeFile.Identity);
        IAxCryptDocument document = activeFile.EncryptedFileInfo.GetAxCryptDocument(decryptIdentity);
        UpdateStatusDependentPropertiesOfListViewItem(activeFile, document.IsKeyShared(), document.IsMasterKeyShared());

        if (!activeFile.IsShared && !activeFile.IsMasterKeyShared)
        {
            return fileDetails;
        }

        string ownAccount = decryptIdentity.UserEmail.Address;
        EncryptedProperties properties = EncryptedProperties.Create(activeFile.EncryptedFileInfo, decryptIdentity);
        if (properties == null)
        {
            return fileDetails;
        }

        fileDetails.SharedWith = properties.SharedKeyHolders.Select(key => key.Email.Address).Where(address => address != ownAccount).ToList().Any() ? properties.SharedKeyHolders.Select(key => key.Email.Address).Where(address => address != ownAccount).ToList() : new List<string>();

        try
        {
            if (activeFile.Properties.CryptoId != Guid.Empty)
            {
                fileDetails.Algorithm = Resolve.CryptoFactory.Create(activeFile.Properties.CryptoId).Name;
            }

            return fileDetails;
        }
        catch (ArgumentException aex)
        {
            New<IReport>().Exception(aex);
        }

        return fileDetails;
    }

    public string GetFileExtention(string fileExt)
    {
        if (string.IsNullOrEmpty(fileExt)) return string.Empty;

        string extention = Path.GetExtension(fileExt);

        return extention.StartsWith(".") ? extention.Substring(1) : extention;
    }

    private static LogOnIdentity ValidateActiveFileIdentity(LogOnIdentity activeFileIdentity)
    {
        if (activeFileIdentity != LogOnIdentity.Empty)
        {
            return activeFileIdentity;
        }

        return New<KnownIdentities>().DefaultEncryptionIdentity;
    }

    private static void UpdateStatusDependentPropertiesOfListViewItem(ActiveFile activeFile, bool isShared, bool isMasterKeyShared)
    {
        if (activeFile.IsDecrypted)
        {
            return;
        }

        if (isShared && isMasterKeyShared)
        {
            return;
        }

        if (isShared)
        {
            return;
        }

        if (isMasterKeyShared)
        {
            return;
        }

        return;
    }
}
