using AxCrypt.Abstractions;
using AxCrypt.App.Desktop.Services.Interface;
using AxCrypt.App.Desktop.Helpers;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.Core;
using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI.ViewModel;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.Core.UI;

namespace AxCrypt.App.Desktop.ViewModels;

public class RecentFoldersViewModel : ViewModelBase
{
    private readonly MainViewModel _mainViewModel;
    private readonly FileOperationViewModel _fileOperationViewModel;
    private WatchedFoldersViewModel _viewModel;
    private ShareKeyViewModel? _sharekeyViewModel;

    private bool _isDescending;
    private bool _folderContextMenu;

    public RecentFoldersViewModel(ShareKeyViewModel sharekeyViewModel)
    {
        LogOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
        _mainViewModel = AxCServiceProviderExtension.LogOnViewModel!.MainViewModel;
        _fileOperationViewModel = AxCServiceProviderExtension.LogOnViewModel!.FileOperationViewModel;
        _sharekeyViewModel = sharekeyViewModel;
        RecentFoldersList = new ObservableCollection<string>();
        SelectedRecentFolders = new List<string>();

        Initialize();
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
        LogOnViewModel.BindPropertyAsyncChanged(nameof(LogOnViewModel.License), async (LicenseCapabilities license) => { await ConfigureMenusAccordingToPolicyAsync(license); });
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.WatchedFolders), (IEnumerable<string> folders) => { UpdateWatchedFolders(folders); });
        this.BindPropertyChanged(nameof(SelectedRecentFolders), (IEnumerable<string> files) =>
        {
            if (files != null)
            {
                _mainViewModel.SelectedWatchedFolders = files;
            }
        });
    }

    private async Task ConfigureMenusAccordingToPolicyAsync(LicenseCapabilities license)
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

    public async Task AddSecuredFolder(EventArgs eventArgs)
    {
        FolderContextMenu = false;
        await PremiumFeature_ClickAsync(LicenseCapability.SecureFolders, async (ss, ee) => { await WatchedFoldersAddSecureFolderMenuItem_Click(ss, ee); }, null, eventArgs);
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

        await _mainViewModel.AddWatchedFolders.ExecuteAsync(eventArgs.SelectedFiles);
    }

    private async void WatchedFolderKeySharing(EventArgs args)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, (ss, ee) => { return WatchedFoldersKeySharingAsync(_mainViewModel.SelectedWatchedFolders); }, null, args);
    }

    private async Task WatchedFoldersKeySharingAsync(IEnumerable<string> folderPaths)
    {
        if (!folderPaths.Any()) return;

        SharingListViewModel viewModel = await SharingListViewModel.CreateForFoldersAsync(folderPaths, Resolve.KnownIdentities.DefaultEncryptionIdentity);
        await _sharekeyViewModel.SetSelectedFilesOrFolders(_mainViewModel.SelectedWatchedFolders.Select(e => e), viewModel);

        await viewModel.ShareFolders.ExecuteAsync(null);
    }

    private async void DecryptPermanently()
    {
        await _mainViewModel.DecryptWatchedFolders.ExecuteAsync(_mainViewModel.SelectedWatchedFolders);
    }

    private async void DecryptTemporarily()
    {
        await _fileOperationViewModel.DecryptFolders.ExecuteAsync(_mainViewModel.SelectedWatchedFolders);
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
        if (LogOnViewModel.License.Has(requiredCapability))
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