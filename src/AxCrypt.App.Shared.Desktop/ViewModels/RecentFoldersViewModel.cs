using AxCrypt.Common;
using AxCrypt.App.Entitlement.Services;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.ViewModels;

public class RecentFoldersViewModel : ViewModelBase
{
    private readonly MainViewModel _mainViewModel;
    private readonly FileOperationViewModel _fileOperationViewModel;
    private ShareKeyViewModel? _sharekeyViewModel;
    private FolderSettingsViewModel? _folderSettingsViewModel;
    private UserPromptViewModel _userPromptViewModel;

    private bool _isDescending;
    private bool _folderContextMenu;

    public RecentFoldersViewModel(ShareKeyViewModel sharekeyViewModel, FolderSettingsViewModel folderSettingsViewModel, UserPromptViewModel userPromptVM)
    {
        LogOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
        _mainViewModel = AxCServiceProviderExtension.LogOnViewModel!.MainViewModel;
        _fileOperationViewModel = AxCServiceProviderExtension.LogOnViewModel!.FileOperationViewModel;
        _sharekeyViewModel = sharekeyViewModel;
        _folderSettingsViewModel = folderSettingsViewModel;
        _userPromptViewModel = userPromptVM;
        RecentFoldersList = new ObservableCollection<string>();
        SelectedRecentFolders = new List<string>();
    }

    public LogOnViewModel LogOnViewModel { get; set; }

    public ObservableCollection<string> RecentFoldersList { get; set; }

    public IEnumerable<string> SelectedRecentFolders { get; set; }

    public bool FolderContextMenu
    {
        get => _folderContextMenu;
        set => _folderContextMenu = value;
    }

    public bool HasNoSubscription { get; set; }

    public void Initialize()
    {
        _mainViewModel.LoggedOn = Resolve.KnownIdentities.IsLoggedOn;
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.License), (LicenseCapabilities license) => { ConfigureMenusAccordingToPolicy(license); });
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.WatchedFolders), (IEnumerable<string> folders) => { UpdateWatchedFolders(folders); });
        this.BindPropertyChanged(nameof(SelectedRecentFolders), (IEnumerable<string> files) =>
        {
            if (files != null)
            {
                _mainViewModel.SelectedWatchedFolders = files;
            }
        });
        RecentFoldersList = new ObservableCollection<string>(_mainViewModel.WatchedFolders);
    }

    private void ConfigureMenusAccordingToPolicy(LicenseCapabilities license)
    {
        HasNoSubscription = license.CryptoPolicy.Name == "Free";
    }

    public void SortByDate()
    {
        if (RecentFoldersList.Any())
        {
            IEnumerable<IDataContainer> dataContainers = RecentFoldersList.Select(f => New<IDataContainer>(f));
            IEnumerable<IDataStore> data = dataContainers.Select(rf => rf.FileItemInfo(rf.FullName));

            RecentFoldersList = new ObservableCollection<string>(_isDescending ? data.Where(rf => rf != null).OrderByDescending(rf => rf.LastWriteTimeUtc).Select(f => f.FullName) : data.Where(rf => rf != null).OrderBy(rf => rf.LastWriteTimeUtc).Select(f => f.FullName));

            _isDescending = !_isDescending;
        }
    }

    public void HandleSortChange(ChangeEventArgs e)
    {
        string? selectedValue = e.Value?.ToString();
        if (!string.IsNullOrEmpty(selectedValue))
        {
            SortBy(selectedValue);
            UpdateViewState();
        }
    }

    public void SortBy(string sortOption)
    {
        if (RecentFoldersList.Any())
        {
            IEnumerable<IDataContainer> dataContainers = RecentFoldersList.Select(f => New<IDataContainer>(f));
            IEnumerable<IDataContainer> sortedData = sortOption switch
            {
                "Date" =>
                   _isDescending
                        ? dataContainers.Where(rf => rf != null).OrderByDescending(rf => rf.LastAccessTime)
                        : dataContainers.Where(rf => rf != null).OrderBy(rf => rf.LastAccessTime),

                "Content" => _isDescending
                    ? dataContainers.OrderByDescending(rf => rf.Name)
                    : dataContainers.OrderBy(rf => rf.Name),

                _ => dataContainers
            };

            RecentFoldersList = new ObservableCollection<string>(sortedData.Select(f => f.FullName));
            _isDescending = !_isDescending;
        }
    }

    private void UpdateWatchedFolders(IEnumerable<string> folders)
    {
        if (RecentFoldersList != null)
        {
            RecentFoldersList = new ObservableCollection<string>();
        }

        RecentFoldersList = new ObservableCollection<string>(folders.Select(fld => fld));

        UpdateViewState();
    }

    public void SecuredContextMenu(MouseEventArgs e, string folderPath)
    {
        SelectedRecentFolders = new List<string>();
        if (folderPath != null)
        {
            SelectedRecentFolders = RecentFoldersList.Where(sf => sf.Equals(folderPath));
            _mainViewModel.SelectedWatchedFolders = SelectedRecentFolders;
            FolderContextMenu = !FolderContextMenu;
        }
    }

    public void HandleDoubleClick(MouseEventArgs e, string folderPath)
    {
        SecuredContextMenu(e, folderPath);

        _mainViewModel.OpenSelectedFolder.Execute(_mainViewModel.SelectedWatchedFolders.First());
    }

    public async void OnContextMenuAction(EventArgs arg, SecuredFolderContextMenu contextMenu)
    {
        if (!SelectedRecentFolders.Any())
        {
            return;
        }

        switch (contextMenu)
        {
            case SecuredFolderContextMenu.FolderSettings:
                await FolderSettings(arg);
                break;

            case SecuredFolderContextMenu.AddSecuredFolder:
                await AddSecuredFolder(arg);
                break;

            case SecuredFolderContextMenu.ShareKey:
                WatchedFolderKeySharing(arg);
                break;

            case SecuredFolderContextMenu.DecryptPermanently:
                DecryptPermanently();
                break;

            case SecuredFolderContextMenu.DecryptTemporarily:
                await DecryptTemporarily();
                break;

            case SecuredFolderContextMenu.ShowInExplorer:
                OpenSelectedFolder();
                break;

            case SecuredFolderContextMenu.RemoveFromListButKeepSecured:
                RemoveWatchedFolders();
                break;
        }
    }

    private FileSelectionEventArgs? AddedFoldersEvent { get; set; }

    public async Task EncryptDroppedFolders(IList<string> folders)
    {
        if (!folders.Any())
        {
            return;
        }

        AddedFoldersEvent = new FileSelectionEventArgs(new string[] { })
        {
            FileSelectionType = FileSelectionType.Folder
        };

        for (int i = 0; i < folders.Count; i++)
        {
            AddedFoldersEvent.SelectedFiles.Add(folders[i]);
        }

        await PremiumFeature_ClickAsync(LicenseCapability.SecureFolders, async (ss, ee) => { await DragAndDroppedToSecureFolderAsync(ss, ee); }, null!, AddedFoldersEvent);
    }

    private async Task DragAndDroppedToSecureFolderAsync(object sender, EventArgs e)
    {
        if (AddedFoldersEvent!.SelectedFiles == null || !AddedFoldersEvent.SelectedFiles.Any())
        {
            return;
        }

        await _mainViewModel.AddWatchedFolders.ExecuteAsync(AddedFoldersEvent.SelectedFiles);
    }

    public async Task AddSecuredFolder(EventArgs eventArgs)
    {
        FolderContextMenu = false;
        await PremiumFeature_ClickAsync(LicenseCapability.SecureFolders, async (ss, ee) => { await WatchedFoldersAddSecureFolderMenuItem_Click(ss, ee); }, null!, eventArgs);
    }

    public async Task FolderSettings(EventArgs eventArgs)
    {
        FolderContextMenu = false;
        await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, (ss, ee) => { return WatchedFoldersSettingsAsync(_mainViewModel.SelectedWatchedFolders); }, null!, eventArgs);
    }

    private async Task WatchedFoldersAddSecureFolderMenuItem_Click(object sender, EventArgs e)
    {

        FileSelectionEventArgs eventArgs = new FileSelectionEventArgs(new string[] { })
        {
            FileSelectionType = FileSelectionType.Folder
        };

        await New<IDataItemSelection>().HandleSelection(eventArgs);
        if (eventArgs.SelectedFiles == null || !eventArgs.SelectedFiles.Any())
        {
            return;
        }

        int availableCount = await New<UserEntitlementService>().GetRemainingCount(LimitedCapability.SecureFolders, New<AccountStatusViewModel>().SubscriptionLevel, eventArgs.SelectedFiles.Count());
        if (availableCount <= 0)
        {
            return;
        }

        await _mainViewModel.AddWatchedFolders.ExecuteAsync(eventArgs.SelectedFiles.Take(availableCount));
        await New<UserEntitlementService>().InsertUserUsageCount(LimitedCapability.SecureFolders, LogOnViewModel.SubscriptionLevel);
    }

    private async void WatchedFolderKeySharing(EventArgs args)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, (ss, ee) => { return WatchedFoldersKeySharingAsync(_mainViewModel.SelectedWatchedFolders); }, null!, args);
    }

    private async Task WatchedFoldersKeySharingAsync(IEnumerable<string> folderPaths)
    {
        if (!folderPaths.Any()) return;

        // Show the dialog shell immediately so the user sees it at once
        // while CreateForFoldersAsync makes its API call in the background.
        _sharekeyViewModel!.LogOnViewModel.ShareKeyDialog.Show();

        SharingListViewModel viewModel = await SharingListViewModel.CreateForFoldersAsync(folderPaths, Resolve.KnownIdentities.DefaultEncryptionIdentity);
        await _sharekeyViewModel!.SetSelectedFilesOrFolders(_mainViewModel.SelectedWatchedFolders.Select(e => e), viewModel);

        await viewModel.ShareFolders.ExecuteAsync(null!);
    }

    private async Task WatchedFoldersSettingsAsync(IEnumerable<string> folderPaths)
    {
        if (!folderPaths.Any()) return;

        FolderSettingViewModel viewModel = FolderSettingViewModel.CreateForSetting(folderPaths, Resolve.KnownIdentities.DefaultEncryptionIdentity);
        await _folderSettingsViewModel!.SetFolderSettings(_mainViewModel.SelectedWatchedFolders.Select(e => e), viewModel, async () =>
        {
            await viewModel.SaveFolderSetings.ExecuteAsync(null!);
            
            if (viewModel.IgnoredFolders.Count() > 0 && New<UserSettings>().FolderOperationMode == Common.FolderOperationMode.IncludeSubfolders)
            {
                await _userPromptViewModel.SetUserPrompt(Texts.UserPromptOnAddingExcludeFolder, [Texts.YesDecryptText, Texts.NoKeepEncryptedText], "/Images/UserPromptUnSecureExcludedFolder.svg", async () =>
                {
                    await viewModel.DecryptFilesInExcludedFolderTask.ExecuteAsync(null);
                });
            }

        });
    }

    private async void DecryptPermanently()
    {
        // Snapshot which folders the user asked to stop securing. Core's
        // DecryptWatchedFolders command mutates MainViewModel.WatchedFolders
        // before it finishes the actual decrypt, so the row vanishes from
        // the UI immediately. If the user then right-clicks the progress
        // bar and chooses Cancel, the decrypt aborts but Core does NOT
        // restore the watched-folder entry — leaving the user with a
        // partially-decrypted folder that's no longer in the secured list.
        //
        // Capture the list, run the command, then check whether the
        // operation was cancelled. If it was, re-add anything that got
        // pulled but never finished decrypting so the secured list lines
        // up with reality.
        List<string> toStop = _mainViewModel.SelectedWatchedFolders?.ToList() ?? new List<string>();

        IProgressContext? ctx = AxCServiceProviderExtension.ProgressBarService?.ProgressContext;

        await _mainViewModel.DecryptWatchedFolders.ExecuteAsync(toStop);

        if (toStop.Count == 0)
        {
            return;
        }

        bool wasCancelled = ctx != null && ctx.Cancel;
        if (!wasCancelled)
        {
            return;
        }

        // Refresh the secured list from the source of truth — any folder
        // the user asked to stop that is still actually secured (i.e.
        // wasn't fully unsecured before the cancel landed) should remain
        // in the list. The simplest way to converge is to re-add the
        // missing entries: Core's AddWatchedFolders is a no-op for
        // already-watched folders, and for the ones it dropped optimistically
        // it'll put them back.
        IEnumerable<string> stillSecured = _mainViewModel.WatchedFolders ?? Array.Empty<string>();
        List<string> missing = toStop
            .Where(p => !string.IsNullOrEmpty(p))
            .Where(p => !stillSecured.Any(s => string.Equals(s, p, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (missing.Count > 0)
        {
            await _mainViewModel.AddWatchedFolders.ExecuteAsync(missing);
        }
    }

    private async Task DecryptTemporarily()
    {
        IEnumerable<string> selectedFolders = _mainViewModel.SelectedWatchedFolders?.ToList() ?? new List<string>();
        await _fileOperationViewModel.DecryptFolders.ExecuteAsync(selectedFolders);

        await Resolve.SessionNotify.NotifyAsync(new SessionNotification(SessionNotificationType.WatchedFolderChange));
        LogOnViewModel.UIStateChanged();
        UpdateViewState();
    }

    public bool IsTemporarilyDecrypted(string folderPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return false;
            }

            IDataContainer container = New<IDataContainer>(folderPath);
            if (!container.IsAvailable)
            {
                return false;
            }

            WatchedFolder? watchedFolder = Resolve.KnownIdentities.LoggedOnWatchedFolders
                .FirstOrDefault(wf => string.Equals(
                    wf.Path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase));

            List<IDataContainer> ignoredFolders = watchedFolder?.IgnoredFolders
                .Select(ignored => New<IDataContainer>(ignored))
                .ToList() ?? new List<IDataContainer>();

            List<IDataStore> files = container
                .ListOfFiles(ignoredFolders, New<UserSettings>().FolderOperationMode.Policy())
                .ToList();

            if (!New<UserSettings>().DoNotShowAgain.HasFlag(DoNotShowAgainOptions.IgnoreFileWarning))
            {
                return files.Any(file => !file.IsEncrypted());
            }

            return files.Any(file => file.IsEncryptable());
        }
        catch
        {
            return false;
        }
    }

    private void OpenSelectedFolder()
    {
        _mainViewModel.OpenSelectedFolder.Execute(_mainViewModel.SelectedWatchedFolders.First());
    }

    private async void RemoveWatchedFolders()
    {
        await _mainViewModel.RemoveWatchedFolders.ExecuteAsync(_mainViewModel.SelectedWatchedFolders);
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

        AxCServiceProviderExtension.UpgradeSubscriptionViewModel!.ShowUpgradeDialog();
    }
}
