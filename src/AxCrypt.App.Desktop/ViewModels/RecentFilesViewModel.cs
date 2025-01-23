using AxCrypt.Api.Model;
using AxCrypt.App.Desktop.Code;
using AxCrypt.App.Desktop.Services;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Desktop.ViewModels;

public class RecentFilesViewModel : ViewModelBase
{
    private MainViewModel _mainViewModel;
    private FileOperationViewModel _fileOperationViewModel;
    private ShareKeyViewModel? _sharekeyViewModel;

    private ProcessIndicatorService? _ProcessIndicatorService;

    public RecentFilesViewModel(ShareKeyViewModel sharekeyViewModel)
    {
        LogOnViewModel = AxCServiceProvider.LogOnViewModel!;
        _mainViewModel = AxCServiceProvider.LogOnViewModel!.MainViewModel;
        _fileOperationViewModel = AxCServiceProvider.LogOnViewModel!.FileOperationViewModel;
        _sharekeyViewModel = sharekeyViewModel;

        SelectAllChecked = false;
        SelectedFiles = new List<string>();
        RecentFilesList = new ObservableCollection<FileDetails>();

        OnInitialized();
    }

    public void OnInitialized()
    {
        IsHideRecentFiles = New<UserSettings>().HideRecentFiles;

        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.License), (LicenseCapabilities license) => { _recentFilesListView.UpdateRecentFiles(_mainViewModel.RecentFiles); });

        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.RecentFiles), (IEnumerable<ActiveFile> files) => { UpdateRecentFiles(files); });
        this.BindPropertyChanged(nameof(SelectedFiles), (IEnumerable<string> files) => { _mainViewModel.SelectedRecentFiles = files; });

        //_recentFilesListView.DragOver += (sender, e) => { _mainViewModel.DragAndDropFiles = e.GetDragged(); e.Effect = GetEffectsForRecentFiles(e); };
        // _recentFilesListView.SelectedIndexChanged += (sender, e) => { _mainViewModel.SelectedRecentFiles = _recentFilesListView.SelectedItems.Cast<ListViewItem>().Select(lvi => EncryptedPath(lvi)); };
        //_recentFilesListView.DragDrop += async (sender, e) => { await DropFilesOrFoldersInRecentFilesListViewAsync(); };
    }

    public LogOnViewModel LogOnViewModel { get; set; }

    public ObservableCollection<FileDetails> RecentFilesList { get; set; }

    public IEnumerable<string> SelectedFiles
    { get { return GetProperty<IEnumerable<string>>(nameof(SelectedFiles)); } set { SetProperty(nameof(SelectedFiles), value); } }

    public SubscriptionLevel SubscriptionLevel
    {
        get
        {
            return LogOnViewModel.License.GetLicenseStatus();
        }
    }

    public bool SelectAllChecked { get; set; }

    public bool IsHideRecentFiles { get; set; }

    private void UpdateRecentFiles(IEnumerable<ActiveFile> files)
    {
        if (New<UserSettings>().HideRecentFiles)
        {
            RecentFilesList = new ObservableCollection<FileDetails>();
            return;
        }

        if (RecentFilesList != null)
        {
            RecentFilesList = new ObservableCollection<FileDetails>();
        }

        RecentFilesList = new ObservableCollection<FileDetails>(files.Select(f => new FileDetails(f)));
        AddToSelectedFileList();
        UpdateViewState();
    }

    public void SelectAllFiles(ChangeEventArgs e)
    {
        SelectAllChecked = Convert.ToBoolean(e.Value);
        if (!SelectAllChecked)
        {
            SelectedFiles = new List<string>();
            UpdateRecentFiles(_mainViewModel.RecentFiles);
            return;
        }

        SelectedFiles = RecentFilesList.Select(rf => { rf.IsChecked = SelectAllChecked; return rf.FilePath; }).ToList();
    }

    public void HandleFileClick(bool isChecked, string selectedFile)
    {
        UpdateSelectedFile(selectedFile, isChecked);
    }

    private void UpdateSelectedFile(string selectedFile, bool isChecked)
    {
        RecentFilesList.First(rf => rf.FilePath.Equals(selectedFile)).IsChecked = isChecked;

        AddToSelectedFileList();
    }

    private void AddToSelectedFileList()
    {
        SelectedFiles = RecentFilesList.Where(rf => rf.IsChecked).Select(rf => rf.FilePath).ToList();

        if (RecentFilesList.Count == SelectedFiles.Count())
        {
            SelectAllChecked = true;
        }
        else
        {
            SelectAllChecked = false;
        }
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

    private string selectFileOnContextMenuClick;

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

        if (selectFileOnContextMenuClick != null)
        {
            HandleFileClick(false, selectFileOnContextMenuClick);
        }
    }

    private async void OpenSecured()
    {
        await _fileOperationViewModel.OpenFiles.ExecuteAsync(_mainViewModel.SelectedRecentFiles);
    }

    public async void OpenSecuredMouseDoubleClick(MouseEventArgs args, string selectedFilePath)
    {
        if (args == null)
        {
            return;
        }

        if (args.Type == "contextmenu")
        {
            selectFileOnContextMenuClick = selectedFilePath;
            HandleFileClick(true, selectedFilePath);
            return;
        }

        if (args.Type == "dblclick")
        {
            await _fileOperationViewModel.OpenFiles.ExecuteAsync(selectedFilePath == null ? throw new NullReferenceException(nameof(selectedFilePath)) : new List<string> { selectedFilePath });
        }
    }

    private async void RemoveFromListKeepSecured()
    {
        await _mainViewModel.RemoveRecentFiles.ExecuteAsync(_mainViewModel.SelectedRecentFiles);
    }

    private async void DecryptAndRemoveFromList()
    {
        await _fileOperationViewModel.DecryptFiles.ExecuteAsync(_mainViewModel.SelectedRecentFiles);
    }

    private async void ShareKeyFromRecentFiles(EventArgs args)
    {
        await ShareKeyService.ShareKeysAsync(_mainViewModel.SelectedRecentFiles, _sharekeyViewModel, _fileOperationViewModel);
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