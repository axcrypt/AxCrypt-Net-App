using AxCrypt.App.Entitlement.Contracts;
using AxCrypt.App.Entitlement.Services;
using AxCrypt.App.Shared.Desktop.Code;
using AxCrypt.App.Shared.Desktop.Services;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Content;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.ViewModels;

public class RecentFilesViewModel : ViewModelBase
{
    private MainViewModel _mainViewModel;
    private FileOperationViewModel _fileOperationViewModel;
    private ShareKeyViewModel? _sharekeyViewModel;
    private BatchFileOperationService _batchService;

    public RecentFilesViewModel(ShareKeyViewModel sharekeyViewModel, BatchFileOperationService batchService)
    {
        LogOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
        _mainViewModel = AxCServiceProviderExtension.LogOnViewModel!.MainViewModel;
        _fileOperationViewModel = AxCServiceProviderExtension.LogOnViewModel!.FileOperationViewModel;
        _sharekeyViewModel = sharekeyViewModel;
        _batchService = batchService;

        SelectAllChecked = false;
        SelectedFiles = new List<string>();
        RecentFilesList = new ObservableCollection<FileDetails>();
    }

    /// <summary>Expose the batch service so a UI component can subscribe to its progress / summary events.</summary>
    public BatchFileOperationService BatchService => _batchService;

    private bool _bindingsHooked;

    public void OnInitialized()
    {
        IsHideRecentFiles = New<UserSettings>().HideRecentFiles;

        ConfigureMenus(LogOnViewModel.License);

        // The VM is a singleton (see AppDesktopFactory) but the Razor
        // component re-runs OnInitializedAsync on every mount. Guard the
        // property-change subscriptions so we don't keep stacking duplicate
        // callbacks (which would fan-out one ActiveFile change into N
        // redundant UI refreshes).
        if (_bindingsHooked)
        {
            // Even on remount, re-emit current state so the new component
            // instance picks up rows that already exist.
            UpdateRecentFiles(_mainViewModel.RecentFiles);
            return;
        }
        _bindingsHooked = true;

        // Core push: whole RecentFiles collection changes.
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.RecentFiles), (IEnumerable<ActiveFile> files) => { UpdateRecentFiles(files); });

        // Core push: an individual file transitioned open ↔ closed.
        // MainViewModel doesn't always rebuild the RecentFiles collection
        // when only the status of one file flips, so we re-derive from the
        // current list and notify the view.
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.FilesArePending), (bool _) => { UpdateRecentFiles(_mainViewModel.RecentFiles); });

        this.BindPropertyChanged(nameof(SelectedFiles), (IEnumerable<string> files) =>
        {
            _mainViewModel.SelectedRecentFiles = files;
            UpdateViewState();
        });

        //_recentFilesListView.DragOver += (sender, e) => { _mainViewModel.DragAndDropFiles = e.GetDragged(); e.Effect = GetEffectsForRecentFiles(e); };
        // _recentFilesListView.SelectedIndexChanged += (sender, e) => { _mainViewModel.SelectedRecentFiles = _recentFilesListView.SelectedItems.Cast<ListViewItem>().Select(lvi => EncryptedPath(lvi)); };
        //_recentFilesListView.DragDrop += async (sender, e) => { await DropFilesOrFoldersInRecentFilesListViewAsync(); };
    }

    public LogOnViewModel LogOnViewModel { get; set; }

    public ObservableCollection<FileDetails> RecentFilesList { get; set; }

    public IEnumerable<string> SelectedFiles
    { get { return GetProperty<IEnumerable<string>>(nameof(SelectedFiles)); } set { SetProperty(nameof(SelectedFiles), value); } }

    /// <summary>
    /// The single file path that the right-click / ⋮-dots context menu
    /// is targeting. Intentionally separate from <see cref="SelectedFiles"/>
    /// so that opening the context menu on an unchecked row does NOT
    /// add it to the checkbox selection (and therefore does not show the
    /// bulk-action bar).
    /// </summary>
    public string? ContextMenuTarget
    {
        get { return GetProperty<string?>(nameof(ContextMenuTarget)); }
        set { SetProperty(nameof(ContextMenuTarget), value); }
    }

    public bool SelectAllChecked { get; set; }

    public bool IsHideRecentFiles { get; set; }

    public bool HasNoSubscription { get; set; }

    public void UpgradePopup()
    {
        AxCServiceProviderExtension.UpgradeSubscriptionViewModel!.ShowUpgradeDialog();
    }

    private void ConfigureMenus(LicenseCapabilities license)
    {
        HasNoSubscription = license.CryptoPolicy.Name == "Free";

        UpdateViewState();
    }


    private void UpdateRecentFiles(IEnumerable<ActiveFile> files)
    {
        ConfigureMenus(LogOnViewModel.License);

        // Preserve checked-state across re-builds — the underlying
        // ActiveFile collection gets replaced on every change, so without
        // this the UI would lose selection whenever any file's status
        // transitioned.
        HashSet<string> previouslyChecked = (RecentFilesList ?? new ObservableCollection<FileDetails>())
            .Where(rf => rf.IsChecked)
            .Select(rf => rf.FilePath)
            .ToHashSet();

        if (New<UserSettings>().HideRecentFiles || files == null)
        {
            RecentFilesList = new ObservableCollection<FileDetails>();
            SelectedFiles = new List<string>();
            SelectAllChecked = false;
            UpdateViewState();
            return;
        }

        RecentFilesList = new ObservableCollection<FileDetails>(
            files.Select(f =>
            {
                FileDetails details = new FileDetails(f);
                if (previouslyChecked.Contains(details.FilePath))
                {
                    details.IsChecked = true;
                }
                return details;
            }));

        UpdateSelectedFileList();
        UpdateViewState();
    }

    public void SelectAllFiles(ChangeEventArgs e)
    {
        SelectAllChecked = Convert.ToBoolean(e.Value);
        if (RecentFilesList == null)
        {
            return;
        }

        if (!SelectAllChecked)
        {
            foreach (FileDetails rf in RecentFilesList)
            {
                rf.IsChecked = false;
            }
            SelectedFiles = new List<string>();
            UpdateViewState();
            return;
        }

        // Mark every visible file as checked and publish the new list.
        SelectedFiles = RecentFilesList
            .Select(rf =>
            {
                rf.IsChecked = SelectAllChecked;
                return rf.FilePath;
            })
            .ToList();

        UpdateViewState();
    }

    public void HandleFileClick(bool isChecked, string selectedFile)
    {
        UpdateSelectedFile(selectedFile, isChecked);
        UpdateViewState();
    }

    /// <summary>
    /// Records which file the user right-clicked / opened the ⋮ menu on,
    /// WITHOUT mutating the checkbox selection. The bulk-action bar stays
    /// hidden because no row's IsChecked flag changes.
    /// </summary>
    public void OpenContextMenuForFile(string filePath)
    {
        ContextMenuTarget = filePath;
        UpdateViewState();
    }

    /// <summary>Clear the context-menu target (call when the menu closes).</summary>
    public void ClearContextMenuTarget()
    {
        ContextMenuTarget = null;
        UpdateViewState();
    }

    /// <summary>
    /// Uncheck every row, hide the bulk-action bar, and clear the
    /// downstream selection list on MainViewModel.
    /// </summary>
    public void ClearSelection()
    {
        if (RecentFilesList != null)
        {
            foreach (FileDetails f in RecentFilesList)
            {
                f.IsChecked = false;
            }
        }
        SelectAllChecked = false;
        SelectedFiles = new List<string>();
        UpdateViewState();
    }

    /// <summary>
    /// Run a context-menu action against the right-clicked target.
    /// • If the target is already one of the checked rows, the action applies
    ///   to the whole multi-selection (standard file-manager behavior).
    /// • Otherwise, the action applies only to the right-clicked row —
    ///   the visible checkbox selection is left untouched.
    /// </summary>
    public async Task RunContextMenuActionForTarget(EventArgs args, SecuredFilesContextMenu action)
    {
        string? target = ContextMenuTarget;
        if (string.IsNullOrEmpty(target))
        {
            await OnContextMenuAction(args, action);
            return;
        }

        bool targetIsChecked = RecentFilesList?.Any(f => f.FilePath == target && f.IsChecked) ?? false;

        if (targetIsChecked)
        {
            // Whole checked-selection path.
            await OnContextMenuAction(args, action);
            ContextMenuTarget = null;
            UpdateViewState();
            return;
        }

        // Single-file path: temporarily push the target into MainViewModel
        // so the existing command pipeline picks it up, then restore the
        // user's actual selection so the checkbox list stays consistent.
        IEnumerable<string> previous = _mainViewModel.SelectedRecentFiles ?? new List<string>();
        _mainViewModel.SelectedRecentFiles = new List<string> { target };
        try
        {
            await OnContextMenuAction(args, action);
        }
        finally
        {
            _mainViewModel.SelectedRecentFiles = previous;
            ContextMenuTarget = null;
            UpdateViewState();
        }
    }

    private void UpdateSelectedFile(string selectedFile, bool isChecked)
    {
        if (RecentFilesList == null || !RecentFilesList.Any())
        {
            return;
        }

        FileDetails? file = RecentFilesList.FirstOrDefault(rf => rf.FilePath.Equals(selectedFile));
        if (file != null)
        {
            file.IsChecked = isChecked;
        }

        UpdateSelectedFileList();
    }

    private void UpdateSelectedFileList()
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

    private string? selectFileOnContextMenuClick;

    public async Task OnContextMenuAction(EventArgs args, SecuredFilesContextMenu securedFilesContextMenu)
    {
        switch (securedFilesContextMenu)
        {
            case SecuredFilesContextMenu.OpenSecured:
                await OpenSecured();
                break;

            case SecuredFilesContextMenu.RemoveFromListButKeepSecured:
                await RemoveFromListKeepSecured();
                break;

            case SecuredFilesContextMenu.StopSecureAndRemoveFromList:
                await DecryptAndRemoveFromList();
                break;

            case SecuredFilesContextMenu.ShareKey:
                await ShareKeyFromRecentFiles(args);
                break;

            case SecuredFilesContextMenu.ShowInFolder:
                await ShowInFolder();
                break;

            case SecuredFilesContextMenu.RenameToOriginal:
                await RestoreToOriginalRecentFiles(args);
                break;

            case SecuredFilesContextMenu.ReEncryptAndClose:
                await ReEncryptAndCloseOpenFiles();
                break;

            case SecuredFilesContextMenu.ClearRecentFiles:
                await ClearAllRecentFiles();
                break;
        }

        if (selectFileOnContextMenuClick != null)
        {
            HandleFileClick(false, selectFileOnContextMenuClick);
        }
    }

    /// <summary>
    /// Encrypt a batch of dropped files. Each file is encrypted in its own
    /// Core call so that one failure (locked file, permission denied,
    /// disk full, …) does not abort the remaining files. Errors are
    /// captured and surfaced via the batch-summary toast.
    /// </summary>
    public async Task EncryptDroppedFiles(IList<string> files)
    {
        if (files == null || !files.Any())
        {
            return;
        }

        int availableEncryptionLimit = AxCServiceProviderExtension.GetService<IFeatureUsageProvider>().GetUsage(FeatureKey.FileEncryption).Remaining;
        if (availableEncryptionLimit <= 0)
        {
            AxCServiceProviderExtension.GetService<PaidFeaturegateService>().ShowPaidGate(
                Texts.QuickActionUnlimitedFileEncryptions,
                Texts.QuickActionEncryptFileHelpText,
                new[] { Texts.QuickActionUnlimitedFileEncryptions, Texts.QuickActionEncryptFilesSeconds, Texts.QuickActionSecureStrongEncryption, Texts.UnlockAdvancedEncryptionFeaturesPopup });
            return;
        }
        
        files = files.Take(availableEncryptionLimit).ToList();

        // FeatureKey.FileEncryption → the batch service reports the
        // successful count to the entitlement provider on finish, so the
        // free-tier usage bar reflects the encryption straight away.
        await _batchService.RunAsync(
            files,
            async (path) => await _fileOperationViewModel.EncryptFiles.ExecuteAsync(new[] { path }),
            "Encrypted",
            FeatureKey.FileEncryption);
    }

    public async Task OpenSecured()
    {
        // Each "Open" goes through its own Core call so a single failure
        // (wrong password, locked file, ...) doesn't stop the rest.
        await _batchService.RunAsync(
            _mainViewModel.SelectedRecentFiles,
            async (path) => await _fileOperationViewModel.OpenFiles.ExecuteAsync(new[] { path }),
            "Opened");
    }

    public async Task OpenSecuredMouseDoubleClick(MouseEventArgs args, string selectedFilePath)
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
            if (selectedFilePath == null)
            {
                throw new NullReferenceException(nameof(selectedFilePath));
            }
            await _fileOperationViewModel.OpenFiles.ExecuteAsync(new List<string> { selectedFilePath });
        }
    }

    private async Task ReEncryptAndCloseOpenFiles()
    {
        await new ApplicationManager().WaitForBackgroundToCompleteAsync();
        await _mainViewModel.EncryptPendingFiles.ExecuteAsync(null!);
        await new ApplicationManager().WaitForBackgroundToCompleteAsync();

        UpdateRecentFiles(_mainViewModel.RecentFiles);
    }

    private async Task RemoveFromListKeepSecured()
    {
        await _batchService.RunAsync(
            _mainViewModel.SelectedRecentFiles,
            async (path) => await _mainViewModel.RemoveRecentFiles.ExecuteAsync(new[] { path }),
            "Removed");
    }

    private async Task DecryptAndRemoveFromList()
    {
        await _batchService.RunAsync(
            _mainViewModel.SelectedRecentFiles,
            async (path) =>
            {
                await _fileOperationViewModel.DecryptFiles.ExecuteAsync(new[] { path });
                // Core commands don't throw on error — verify the encrypted file was
                // actually removed. If it still exists, the operation silently failed
                // (e.g. file in use, permission denied). Throwing pushes it to Failed.
                if (System.IO.File.Exists(path))
                    throw new System.IO.IOException(
                        "The file could not be decrypted. It may be open in another application.");
            },
            "Decrypted");
    }

    private async Task ShareKeyFromRecentFiles(EventArgs args)
    {
        // Show the dialog shell immediately — the compiled service makes
        // an API call before firing OnDialogVisibilityChanged(true), which
        // caused a ~5-second blank delay.  Calling Show() here makes the
        // dialog appear at once; the service populates it when ready.
        _sharekeyViewModel!.LogOnViewModel.ShareKeyDialog.Show();

        await ShareKeyService.ShareKeysAsync(_mainViewModel.SelectedRecentFiles, _sharekeyViewModel!, _fileOperationViewModel);
    }

    private async Task ShowInFolder()
    {
        await _fileOperationViewModel.ShowInFolder.ExecuteAsync(_mainViewModel.SelectedRecentFiles);
    }

    private async Task RestoreToOriginalRecentFiles(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.RandomRename, async (ss, ee) => { await _fileOperationViewModel.RestoreRandomRenameFiles.ExecuteAsync(_mainViewModel.SelectedRecentFiles); }, null, e);
    }

    private async Task ClearAllRecentFiles()
    {
        await _mainViewModel.RemoveRecentFiles.ExecuteAsync(_mainViewModel.RecentFiles.Select(files => files.EncryptedFileInfo.FullName));
    }

    private async Task PremiumFeature_ClickAsync(LicenseCapability requiredCapability, Func<object, EventArgs, Task> realHandler, object sender, EventArgs e)
    {
        if (LogOnViewModel.UserHas(requiredCapability))
        {
            if (realHandler != null)
            {
                await realHandler(sender, e);
            }
            return;
        }

        UpgradePopup();
    }
}
