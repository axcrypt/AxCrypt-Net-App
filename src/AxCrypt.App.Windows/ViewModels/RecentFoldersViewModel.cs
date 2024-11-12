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
using AxCrypt.App.Windows.Helpers;

using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.App.Windows.Services;

namespace AxCrypt.App.Windows.ViewModels;

public class RecentFoldersViewModel : ComponentBase
{
    private readonly MainViewModel _mainViewModel;
    private readonly FileOperationViewModel _fileOperationViewModel;

    [Inject]
    private ShareKeyViewModel _shareKeyViewModel { get; set; }

    private bool _loading;
    private bool _isDescending;
    private bool _folderContextMenu;
    private bool _showPopup;
    private bool _showUpgradePopup;

    public ObservableCollection<string> RecentFoldersList { get; set; } = new ObservableCollection<string>();
    public IList<string> SelectedRecentFolders { get; set; } = new List<string>();
    public List<string> SelectedShareKeyFolders { get; set; } = new List<string>();
    public SubscriptionLevel SubscriptionLevel { get; set; }

    public bool Loading
    {
        get => _loading;
        set
        {
            _loading = value;
            OnStateChange?.Invoke();
        }
    }

    public bool showPopup
    {
        get => _showPopup;
        set => _showPopup = value;
    }

    public bool showUpgradePopup
    {
        get => _showUpgradePopup;
        set => _showUpgradePopup = value;
    }

    public bool FolderContextMenu
    {
        get => _folderContextMenu;
        set => _folderContextMenu = value;
    }

    public Action OnStateChange { get; set; }

    public RecentFoldersViewModel()
    {
        _mainViewModel = New<MainViewModel>();
        _fileOperationViewModel = New<FileOperationViewModel>();
        _shareKeyViewModel = new ShareKeyViewModel();
    }

    public async Task InitializeAsync()
    {
        SubscriptionLevel = New<AccountStatusViewModel>().SubscriptionLevel;
        _mainViewModel.LoggedOn = Resolve.KnownIdentities.IsLoggedOn;

        Progress<LoadingModel> progress = new Progress<LoadingModel>(p =>
        {
            Loading = p.IsLoading;
            //InvokeAsync(StateHasChanged);
        });

        IEnumerable<string> folders = await RecentFoldersAsync(progress);
        RecentFoldersList = new ObservableCollection<string>(folders);
    }

    public async Task<IEnumerable<string>> RecentFoldersAsync(IProgress<LoadingModel> progress = null)
    {

        return await LoadingProgressHelper.ExecuteLoadingProgress(async () =>
        {
            return await Task.FromResult<IEnumerable<string>>(SetWatchedFoldersAsync());
        }, progress);
    }

    public IEnumerable<string> SetWatchedFoldersAsync()
    {
        _mainViewModel.WatchedFoldersEnabled = _mainViewModel.License.Has(LicenseCapability.SecureFolders);
        if (!_mainViewModel.WatchedFoldersEnabled)
        {
            _mainViewModel.WatchedFolders = new string[0];
            return new List<string>();
        }

        return Resolve.KnownIdentities.LoggedOnWatchedFolders.Select(wf => wf.Path).ToList();
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

    public void ClosePopup()
    {
        showPopup = false;
    }

    public void HandleFileClick(MouseEventArgs e)
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

            case SecuredFolderContextMenu.AddSecuredFolder:
                await AddSecuredFolder(arg);
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

    public async Task PremiumFeature_ClickAsync(LicenseCapability requiredCapability, Func<object, EventArgs, Task> realHandler, object sender, EventArgs e)
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

    private async void WatchedFoldersAddSecureFolderMenuItem_Click(object sender, EventArgs e)
    {
        IFolderPicker folderPicker = new FolderPickerWindows(); 
        string folder = await folderPicker.PickFolderAsync();
        if (string.IsNullOrEmpty(folder)) return;

        await _mainViewModel.AddWatchedFolders.ExecuteAsync(new string[] { folder });
    }

    private async void WatchedFolderKeySharing(EventArgs args)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, (ss, ee) => { return WatchedFoldersKeySharingAsync(SelectedRecentFolders); }, null, args);
    }

    public async Task WatchedFoldersKeySharingAsync(IEnumerable<string> folderPaths)
    {
        if (!folderPaths.Any()) return;

        SharingListViewModel viewModel = await SharingListViewModel.CreateForFoldersAsync(folderPaths, Resolve.KnownIdentities.DefaultEncryptionIdentity);
        _shareKeyViewModel.SetSelectedFilesOrFolders(SelectedRecentFolders.Select(e => e), viewModel, true);
        showPopup = true;
        StateHasChanged();
        //await viewModel.ShareFolders.ExecuteAsync(null);
    }

    public async void DecryptPermanently()
    {
        await _mainViewModel.DecryptWatchedFolders.ExecuteAsync(SelectedRecentFolders);
    }

    public async void DecryptTemporarily()
    {
        await _fileOperationViewModel.DecryptFolders.ExecuteAsync(SelectedRecentFolders);
    }

    public void OpenSelectedFolder()
    {
        _mainViewModel.OpenSelectedFolder.Execute(SelectedRecentFolders.First());
    }

    public async void RemoveWatchedFolders()
    {
        await _mainViewModel.RemoveWatchedFolders.ExecuteAsync(SelectedRecentFolders);
    }
}
