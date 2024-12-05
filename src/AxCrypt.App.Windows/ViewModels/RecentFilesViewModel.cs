using AxCrypt.Api.Model;
using AxCrypt.App.Components.Models;
using AxCrypt.App.Components.Services;
using AxCrypt.App.Components.Utility;
using AxCrypt.App.Components.Utility.View;
using AxCrypt.App.Components.ViewModels;
using AxCrypt.App.Windows.Code;
using AxCrypt.App.Windows.Services;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.ObjectModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.ViewModels;

public class RecentFilesViewModel : ViewModelBase
{
    private MainViewModel _mainViewModel;
    private FileOperationViewModel _fileOperationViewModel;

    private ProcessIndicatorService? _ProcessIndicatorService;

    public RecentFilesViewModel(LogOnViewModel logOnViewModel)
    {
        LogOnViewModel = logOnViewModel;
        _mainViewModel = logOnViewModel.MainViewModel;
        _fileOperationViewModel = logOnViewModel.FileOperationViewModel;
    }

    public void OnInitializedAsync()
    {
        SelectedFiles = new List<string>();
        RecentFilesList = new ObservableCollection<FileDetails>();

        IsHideRecentFiles = New<UserSettings>().HideRecentFiles;
        UpdateRecentFiles(_mainViewModel.RecentFiles);

        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.RecentFiles), (IEnumerable<ActiveFile> files) => { UpdateRecentFiles(files); LogOnViewModel.UIStateChanged(); });
        BindPropertyChanged(nameof(SelectedFiles), (IEnumerable<string> files) => { _mainViewModel.SelectedRecentFiles = files; });

        //_recentFilesListView.DragOver += (sender, e) => { _mainViewModel.DragAndDropFiles = e.GetDragged(); e.Effect = GetEffectsForRecentFiles(e); };
        // _recentFilesListView.SelectedIndexChanged += (sender, e) => { _mainViewModel.SelectedRecentFiles = _recentFilesListView.SelectedItems.Cast<ListViewItem>().Select(lvi => EncryptedPath(lvi)); };
        //_recentFilesListView.DragDrop += async (sender, e) => { await DropFilesOrFoldersInRecentFilesListViewAsync(); };
    }

    public LogOnViewModel LogOnViewModel { get; set; }

    public ObservableCollection<FileDetails> RecentFilesList { get; set; }

    public IList<string> SelectedFiles { get; set; } = new List<string>();

    //private FileDetails SelectedFile = new FileDetails();

    public SubscriptionLevel SubscriptionLevel
    {
        get
        {
            return LogOnViewModel.License.GetLicenseStatus();
        }
    }

    public bool SelectAllChecked { get; set; } = false;

    public bool ContextMenu { get; set; } = false;

    public bool IsHideRecentFiles { get; set; }

    public bool isNameAscending { get; set; }
    public bool isSizeAscending { get; set; }
    public bool isDateModifiedAscending { get; set; }

    private void UpdateRecentFiles(IEnumerable<ActiveFile> files)
    {
        if (New<UserSettings>().HideRecentFiles)
        {
            RecentFilesList.Clear();
            return;
        }

        if (RecentFilesList != null)
        {
            RecentFilesList.Clear();
        }

        RecentFilesList = new ObservableCollection<FileDetails>(files.Select(f => new FileDetails(f)));
    }

    public void SelectAllFiles(ChangeEventArgs e)
    {
        SelectAllChecked = Convert.ToBoolean(e.Value);
        if (!SelectAllChecked)
        {
            SelectedFiles.Clear();
            UpdateRecentFiles(_mainViewModel.RecentFiles);
            return;
        }

        SelectedFiles = RecentFilesList.Select(rf => { rf.IsChecked = SelectAllChecked; return rf.FilePath; }).ToList();
    }

    public void SelectFile(ChangeEventArgs e, string selectedFile)
    {
        if (selectedFile == null)
        {
            throw new InvalidOperationException($"{nameof(selectedFile)} path should not empty!");
        }

        bool isChecked = Convert.ToBoolean(e.Value);
        UpdateSelectedFile(selectedFile, isChecked);
    }

    private void UpdateSelectedFile(string selectedFile, bool isChecked)
    {
        RecentFilesList.First(rf => rf.FilePath.Equals(selectedFile)).IsChecked = isChecked;
        if (!isChecked)
        {
            SelectedFiles = SelectedFiles.Where(sf => !sf.Equals(selectedFile)).ToList();
            return;
        }

        AddToSelectedFileList(selectedFile);
    }

    private void AddToSelectedFileList(string selectedFilepath)
    {
        if (!SelectedFiles.Contains(selectedFilepath))
        {
            SelectedFiles.Add(selectedFilepath);
        }
    }

    public void HandleFileClick(MouseEventArgs e, string selectedFile)
    {
        //ContextMenu = false;
        UpdateSelectedFile(selectedFile, true);
    }

    public void SetSortOrder(int column)
    {
        ActiveFileComparer comparer = GetComparer(column, AppPreferences.RecentFilesSortColumn == column ? AppPreferences.RecentFilesAscending : false);
        if (comparer == null)
        {
            return;
        }
        AppPreferences.RecentFilesAscending = !comparer.ReverseSort;
        AppPreferences.RecentFilesSortColumn = column;
        _mainViewModel.RecentFilesComparer = comparer;
    }

    private ActiveFileComparer GetComparer(int column, bool reverseSort)
    {
        ActiveFileComparer comparer;
        switch (column)
        {
            case 0:
                comparer = ActiveFileComparer.DecryptedNameComparer;
                break;

            case 1:
                comparer = ActiveFileComparer.SizeComparer;
                break;

            case 2:
                comparer = ActiveFileComparer.DateComparer;
                break;

            case 3:
                comparer = ActiveFileComparer.EncryptedNameComparer;
                break;

            case 4:
                comparer = ActiveFileComparer.DateComparer;
                break;

            case 5:
                comparer = ActiveFileComparer.CryptoNameComparer;
                break;

            default:
                throw new ArgumentException($"Can't sort column index '{column}'.");
        }
        comparer.ReverseSort = reverseSort;
        return comparer;
    }

    //public void HandleContextMenu(MouseEventArgs e, string selectedFilepath)
    //{
    //    try
    //    {
    //        AddToSelectedFileList(selectedFilepath);
    //    }
    //    catch (Exception ex)
    //    {
    //        throw new Exception(ex.Message);
    //    }
    //}
    //public IEnumerable<string> GetDragged(this DragEventArgs e)
    //{
    //    IList<string> dropped = e.Data.GetData(DataFormats.FileDrop) as IList<string>;
    //    if (dropped == null)
    //    {
    //        return new string[0];
    //    }

    //    return dropped;
    //}

    //private DragDropEffects GetEffectsForRecentFiles(DragEventArgs e)
    //{
    //    if (!_mainViewModel.DroppableAsRecent && !_mainViewModel.DroppableAsWatchedFolder)
    //    {
    //        return DragDropEffects.None;
    //    }
    //    return (DragDropEffects.Link | DragDropEffects.Copy) & e.AllowedEffect;
    //}

    //private async Task DropFilesOrFoldersInRecentFilesListViewAsync()
    //{
    //    await this.WithWaitCursorAsync(async () =>
    //    {
    //        if (_mainViewModel.DroppableAsRecent)
    //        {
    //            await _fileOperationViewModel.AddRecentFiles.ExecuteAsync(_mainViewModel.DragAndDropFiles);
    //        }
    //    }, () => { });
    //}

    //public void SortByName()
    //{
    //    if (isNameAscending)
    //    {
    //        RecentFilesList = new ObservableCollection<FileDetails>(RecentFilesList.OrderBy(f => f.FileName).ToList());
    //    }
    //    else
    //    {
    //        RecentFilesList = new ObservableCollection<FileDetails>(RecentFilesList.OrderByDescending(f => f.FileName).ToList());
    //    }
    //    isNameAscending = !isNameAscending;
    //}

    //public void SortBySize()
    //{
    //    if (isSizeAscending)
    //    {
    //        RecentFilesList = new ObservableCollection<FileDetails>(RecentFilesList.OrderBy(f => f.FileSize).ToList());
    //    }
    //    else
    //    {
    //        RecentFilesList = new ObservableCollection<FileDetails>(RecentFilesList.OrderByDescending(f => f.FileSize).ToList());
    //    }
    //    isSizeAscending = !isSizeAscending;
    //}

    //public void SortByDateModified()
    //{
    //    if (isDateModifiedAscending)
    //    {
    //        RecentFilesList = new ObservableCollection<FileDetails>(RecentFilesList.OrderBy(f => f.LastModifiedDate).ToList());
    //    }
    //    else
    //    {
    //        RecentFilesList = new ObservableCollection<FileDetails>(RecentFilesList.OrderByDescending(f => f.LastModifiedDate).ToList());
    //    }
    //    isDateModifiedAscending = !isDateModifiedAscending;
    //}

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
        await _fileOperationViewModel.OpenFiles.ExecuteAsync(_mainViewModel.SelectedRecentFiles);
        // await _fileOperationViewModel.OpenFiles.ExecuteAsync(SelectedFiles.Select(f => f.FilePath));
    }

    public async void OpenSecuredMouseDoubleClick(EventArgs args, IEnumerable<FileDetails> selectedFiles)
    {
        await _fileOperationViewModel.OpenFiles.ExecuteAsync(_mainViewModel.SelectedRecentFiles);
        // await _fileOperationViewModel.OpenFiles.ExecuteAsync(selectedFiles.Select(f => f.FilePath));
    }

    private async void RemoveFromListKeepSecured()
    {
        await _mainViewModel.RemoveRecentFiles.ExecuteAsync(_mainViewModel.SelectedRecentFiles);

        //await _mainViewModel.RemoveRecentFiles.ExecuteAsync(SelectedFiles.Select(f => f.FilePath));
    }

    private async void DecryptAndRemoveFromList()
    {
        await _fileOperationViewModel.DecryptFiles.ExecuteAsync(_mainViewModel.SelectedRecentFiles);
        //await _fileOperationViewModel.DecryptFiles.ExecuteAsync(SelectedFiles.Select(f => f.FilePath));
    }

    private async void ShareKeyFromRecentFiles(EventArgs args)
    {
        await ShareKeyService.ShareKeysWithFileSelectionAsync(_mainViewModel.SelectedRecentFiles);
        //await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, async (ss, ee) => { await ShareKeysWithFileSelectionAsync(SelectedFiles.Select(f => f.FilePath).ToList()); }, null, args);
    }

    private async void ShowInFolder()
    {
        await _fileOperationViewModel.ShowInFolder.ExecuteAsync(_mainViewModel.SelectedRecentFiles);
    }

    private async Task RestoreToOriginalRecentFiles(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.RandomRename, async (ss, ee) => { await _fileOperationViewModel.RestoreRandomRenameFiles.ExecuteAsync(_mainViewModel.SelectedRecentFiles); }, null, e);
    }

    private async void ClearAllRecentFiles()
    {
        await _mainViewModel.RemoveRecentFiles.ExecuteAsync(_mainViewModel.RecentFiles.Select(files => files.EncryptedFileInfo.FullName));
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

        LogOnViewModel.UpgradeDialog.Show();
    }
}