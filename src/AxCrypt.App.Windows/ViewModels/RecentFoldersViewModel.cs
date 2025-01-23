using AxCrypt.Api.Model;
using AxCrypt.App.Desktop.Models;
using AxCrypt.App.Desktop.Services.Interface;
using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Core;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components.Web;
using AxCrypt.Abstractions;
using AxCrypt.App.Desktop.Utility.View;
using AxCrypt.App.Windows.Services;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.ViewModels;

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
        LogOnViewModel = AxCServiceProvider.LogOnViewModel!;
        _mainViewModel = AxCServiceProvider.LogOnViewModel!.MainViewModel;
        _fileOperationViewModel = AxCServiceProvider.LogOnViewModel!.FileOperationViewModel;
        _sharekeyViewModel = sharekeyViewModel;
        RecentFoldersList = new ObservableCollection<string>();
        SelectedRecentFolders = new List<string>();

        Initialize();
    }

    public LogOnViewModel LogOnViewModel { get; set; }

    public ObservableCollection<string> RecentFoldersList { get; set; }

    public IEnumerable<string> SelectedRecentFolders { get; set; }

    public SubscriptionLevel SubscriptionLevel { get; set; }

    public bool FolderContextMenu
    {
        get => _folderContextMenu;
        set => _folderContextMenu = value;
    }

    public void Initialize()
    {
        SubscriptionLevel = LogOnViewModel.SubscriptionLevel;
        _mainViewModel.LoggedOn = Resolve.KnownIdentities.IsLoggedOn;

        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.WatchedFolders), (IEnumerable<string> folders) => { UpdateWatchedFolders(folders); });
        this.BindPropertyChanged(nameof(SelectedRecentFolders), (IEnumerable<string> files) => { _mainViewModel.SelectedWatchedFolders = files; });
    }

    public void SortByDate()
    {
        if (RecentFoldersList.Any())
        {
            IEnumerable<IDataContainer> dataContainers = RecentFoldersList.Select(f => New<IDataContainer>(f));
            IEnumerable<IDataStore> data = dataContainers.Select(rf => rf.FileItemInfo(rf.FullName));

            RecentFoldersList = new ObservableCollection<string>(data.Where(rf => rf != null).OrderBy(rf => _isDescending ? rf.CreationTimeUtc : rf.CreationTimeUtc).Select(f => f.FullName));

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

        Task.Run(() => { LogOnViewModel.UIStateChanged(); });
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
        await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, (ss, ee) => { return WatchedFoldersKeySharingAsync(_mainViewModel.SelectedWatchedFolders); }, null, args);
    }

    private async Task WatchedFoldersKeySharingAsync(IEnumerable<string> folderPaths)
    {
        if (!folderPaths.Any()) return;

        SharingListViewModel viewModel = await SharingListViewModel.CreateForFoldersAsync(folderPaths, Resolve.KnownIdentities.DefaultEncryptionIdentity);
        _sharekeyViewModel.SetSelectedFilesOrFolders(_mainViewModel.SelectedWatchedFolders.Select(e => e), viewModel, true);

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
