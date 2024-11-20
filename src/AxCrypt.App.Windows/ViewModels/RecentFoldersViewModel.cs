using AxCrypt.Api.Model;
using AxCrypt.App.Components.Models;
using AxCrypt.App.Components.Services.Interface;
using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Core;
using System.Collections.ObjectModel;
using AxCrypt.App.Components.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using AxCrypt.Abstractions;
using AxCrypt.App.Components.Utility.View;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.ViewModels;

public class RecentFoldersViewModel : ComponentBase
{
    private LogOnViewModel _logOnViewModel;
    private readonly MainViewModel _mainViewModel;
    private readonly FileOperationViewModel _fileOperationViewModel;
    private ProcessIndicatorService _ProcessIndicatorService;

    private WatchedFoldersViewModel _viewModel;

    public ShareKeyViewModel? SharekeysViewModel { get; set; }

    private bool _isDescending;
    private bool _folderContextMenu;

    public ObservableCollection<string> RecentFoldersList { get; set; }

    public IList<string> SelectedRecentFolders { get; set; } = new List<string>();
    public SubscriptionLevel SubscriptionLevel { get; set; }

    public bool FolderContextMenu
    {
        get => _folderContextMenu;
        set => _folderContextMenu = value;
    }

    public RecentFoldersViewModel(ProcessIndicatorService processIndicatorService, LogOnViewModel logOnViewModel)
    {
        _logOnViewModel = logOnViewModel;
        _mainViewModel = logOnViewModel.MainViewModel;
        _fileOperationViewModel = logOnViewModel.FileOperationViewModel;
        _ProcessIndicatorService = processIndicatorService;
    }

    public async Task InitializeAsync()
    {
        SubscriptionLevel = _logOnViewModel.SubscriptionLevel;
        _mainViewModel.LoggedOn = Resolve.KnownIdentities.IsLoggedOn;

        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.WatchedFolders), (IEnumerable<string> folders) => { UpdateWatchedFolders(folders); });
    }

    private void UpdateWatchedFolders(IEnumerable<string> folders)
    {
        using (ProcessIndicator processIndicator = new ProcessIndicator(_ProcessIndicatorService))
        {
            if (RecentFoldersList != null && folders.Count() != RecentFoldersList.Count)
            {
                RecentFoldersList = new ObservableCollection<string>(folders.Select(fld => fld));
                return;
            }

            if (folders != null && folders.Any())
            {
                RecentFoldersList = new ObservableCollection<string>();
                RecentFoldersList = new ObservableCollection<string>(folders.Select(fld => fld));
                return;
            }

            RecentFoldersList = new ObservableCollection<string>();
        }
    }

    public void SortByDate()
    {
        if (RecentFoldersList.Any())
        {
            IEnumerable<IDataContainer> dataContainers = RecentFoldersList.Select(f => New<IDataContainer>(f));
            IEnumerable<IDataStore> data = dataContainers.Select(rf => rf.FileItemInfo(rf.FullName));

            RecentFoldersList = new ObservableCollection<string>(
                data.Where(rf => rf != null)
                    .OrderBy(rf => _isDescending ? rf.CreationTimeUtc : rf.CreationTimeUtc)
                    .Select(f => f.FullName)
            );

            _isDescending = !_isDescending;
        }
    }

    public void HandleFolderClick(MouseEventArgs e)
    {
        FolderContextMenu = false;
    }

    public async void OnContextMenuAction(EventArgs arg, SecuredFolderContextMenu contextMenu)
    {
        if (!SelectedRecentFolders.Any())
        {
            return;
        }

        switch (contextMenu)
        {
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
                DecryptTemporarily();
                break;

            case SecuredFolderContextMenu.ShowInExplorer:
                OpenSelectedFolder();
                break;

            case SecuredFolderContextMenu.RemoveFromListButKeepSecured:
                RemoveWatchedFolders();
                break;

        }
    }

    public void SecuredContextMenu(MouseEventArgs e, string folderPath)
    {
        SelectedRecentFolders.Clear();
        if (folderPath != null)
        {
            SelectedRecentFolders.Add(folderPath);
            FolderContextMenu = !FolderContextMenu;
        }
    }

    public async Task AddSecuredFolder(EventArgs eventArgs)
    {
        FolderContextMenu = false;
        await PremiumFeature_ClickAsync(LicenseCapability.SecureFolders, (ss, ee) => { WatchedFoldersAddSecureFolderMenuItem_Click(ss, ee); return Constant.CompletedTask; }, null, eventArgs);
    }

    private async void WatchedFoldersAddSecureFolderMenuItem_Click(object sender, EventArgs e)
    {
        IFolderPicker folderPicker = new Services.FolderPickerWindows();
        string folder = await folderPicker.PickFolderAsync();
        if (string.IsNullOrEmpty(folder)) return;

        await _mainViewModel.AddWatchedFolders.ExecuteAsync(new string[] { folder });
    }

    private async void WatchedFolderKeySharing(EventArgs args)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, (ss, ee) => { return WatchedFoldersKeySharingAsync(SelectedRecentFolders); }, null, args);
    }

    private async Task WatchedFoldersKeySharingAsync(IEnumerable<string> folderPaths)
    {
        if (!folderPaths.Any()) return;

        SharingListViewModel viewModel = await SharingListViewModel.CreateForFoldersAsync(folderPaths, Resolve.KnownIdentities.DefaultEncryptionIdentity);
        SharekeysViewModel.SetSelectedFilesOrFolders(SelectedRecentFolders.Select(e => e), viewModel, true);

        await viewModel.ShareFolders.ExecuteAsync(null);
    }

    private async void DecryptPermanently()
    {
        await _mainViewModel.DecryptWatchedFolders.ExecuteAsync(SelectedRecentFolders);
    }

    private async void DecryptTemporarily()
    {
        await _fileOperationViewModel.DecryptFolders.ExecuteAsync(SelectedRecentFolders);
    }

    private void OpenSelectedFolder()
    {
        _mainViewModel.OpenSelectedFolder.Execute(SelectedRecentFolders.First());
    }

    private async void RemoveWatchedFolders()
    {
        await _mainViewModel.RemoveWatchedFolders.ExecuteAsync(SelectedRecentFolders);
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
