using AxCrypt.Abstractions;
using AxCrypt.Api;
using AxCrypt.Api.Model;
using AxCrypt.App.Shared.Desktop;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Common;
using AxCrypt.Core;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System.Globalization;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using static AxCrypt.Abstractions.TypeResolve;
using DataPackageOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation;

namespace AxCrypt.App.Windows;

public partial class MainPage : ContentPage, ISignIn
{
    private readonly FileDropService _fileDropService;
    private LogOnViewModel? _logOnViewModel;
    private RegisterViewModel? _registerViewModel;

    private MainViewModel? _mainViewModel;
    private FileOperationViewModel? _fileOperationViewModel;
    private KnownFoldersViewModel? _knownFoldersViewModel;

    private const string? _homePage = "/";
    private const string? _securedFolders = "/securedfolders";
    private const string? _vault = "/Vault";

    private ApiVersion? _apiVersion;

    public MainPage()
    {
        InitializeComponent();
        //new Styling(Resources.axcrypticon).Style(this, _recentFilesContextMenuStrip, _watchedFoldersContextMenuStrip);
    }

    public MainPage(LogOnViewModel logOnViewModel, MainViewModel mainViewModel, FileOperationViewModel fileOperationViewModel, KnownFoldersViewModel knownFoldersViewModel, RegisterViewModel registerViewModel, FileDropService fileDropService) : this()
    {
        _logOnViewModel = logOnViewModel;
        _mainViewModel = mainViewModel;
        _fileOperationViewModel = fileOperationViewModel;
        _knownFoldersViewModel = knownFoldersViewModel;
        _registerViewModel = registerViewModel;
        new AppMain().Initialize(logOnViewModel, mainViewModel, fileOperationViewModel, knownFoldersViewModel, registerViewModel);

        _fileDropService = fileDropService;

#if WINDOWS
        Microsoft.Maui.Handlers.PageHandler.Mapper.AppendToMapping("DragDrop", (handler, view) =>
        {
            Microsoft.Maui.Platform.ContentPanel nativeView = handler.PlatformView;
            nativeView.AllowDrop = true;

            nativeView.DragOver += (sender, e) =>
            {
                string? currentPage = _fileDropService.CurrentPage;
                switch (currentPage)
                {
                    case _homePage:
                    case _securedFolders:
                    case _vault:
                        e.AcceptedOperation = DataPackageOperation.Move;
                        break;
                    default:
                        e.AcceptedOperation = DataPackageOperation.None;
                        break;
                }
                e.Handled = true;
            };

            nativeView.Drop += async (sender, e) =>
            {
                e.Handled = true;
                string? currentPage = _fileDropService.CurrentPage;
                if (currentPage is not (_homePage or _securedFolders or _vault)) return;

                if (!e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    return;
                }

                IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();
                IEnumerable<IStorageItem> draggedItems = items.Where(itm => itm is StorageFile || itm is StorageFolder).ToList();
                if (!draggedItems.Any())
                {
                    return;
                }

                IList<string> filePaths = new List<string>();
                IList<string> folders = new List<string>();

                foreach (IStorageItem item in items)
                {
                    switch (item)
                    {
                        case StorageFile file when !string.IsNullOrWhiteSpace(file.Path):
                            filePaths.Add(file.Path);
                            break;

                        case StorageFolder folder when !string.IsNullOrWhiteSpace(folder.Path):
                            folders.Add(folder.Path);
                            break;

                        default:
                            break;
                    }
                }

                if (filePaths.Any())
                {
                    _fileDropService.NotifyFilesDropped(filePaths);
                }

                else if (folders.Any())
                {
                    _fileDropService.NotifyFoldersDropped(folders);
                }
            };
        });
#endif
    }

    protected override void OnAppearing()
    {
        Task.Run(async () => await InitializeMainPage());

        base.OnAppearing();
    }

    protected override void ChangeVisualState()
    {
        base.ChangeVisualState();
    }

    private async Task InitializeMainPage()
    {
        New<IRuntimeEnvironment>().FirstInstanceIsReady();

        await GetApiVersionAsync();
        //SetThisVersion();

        UpdateArabicStyle();

        BindToViewModels();
        BindToFileOperationViewModel();

        _logOnViewModel!.MainViewModel = _mainViewModel!;
        _logOnViewModel.FileOperationViewModel = _fileOperationViewModel!;

        await SignInAsync();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
    private void BindToViewModels()
    {
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.DecryptFileEnabled), (bool enabled) => { _decryptToolStripMenuItem.Enabled = enabled; });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.DownloadVersion), async (DownloadVersion dv) => { await SetSoftwareStatus(); await DisplayUpdateCheckPopups(); });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.EncryptFileEnabled), (bool enabled) => { _encryptToolStripButton.Enabled = enabled; ConfigureEncryptMenu(enabled); });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.EncryptFileEnabled), (bool enabled) => { _encryptToolStripMenuItem.Enabled = enabled; });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.FilesArePending), (bool filesArePending) => { _cleanDecryptedToolStripMenuItem.Enabled = filesArePending; });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.FilesArePending), (bool filesArePending) => { _closeAndRemoveOpenFilesToolStripButton.Visible = filesArePending; _closeAndRemoveOpenFilesToolStripButton.ToolTipText = filesArePending ? Texts.CloseAndRemoveOpenFilesToolStripButtonToolTipText : string.Empty; });
        _mainViewModel!.BindPropertyAsyncChanged(nameof(_mainViewModel.License), async (LicenseCapabilities license) => await _knownFoldersViewModel!.UpdateState.ExecuteAsync(null!));
        _knownFoldersViewModel!.KnownFolders = New<IKnownFoldersDiscovery>().Discover();
        //_mainViewModel.BindPropertyAsyncChanged(nameof(_mainViewModel.License), async (LicenseCapabilities license) => { await ConfigureMenusAccordingToPolicyAsync(license); });
        //_mainViewModel.BindPropertyAsyncChanged(nameof(_mainViewModel.License), async (LicenseCapabilities license) => { await ConfigureLinkLabelAsync(New<KnownIdentities>().DefaultEncryptionIdentity); });
        _mainViewModel.BindPropertyAsyncChanged(nameof(_mainViewModel.License), async (LicenseCapabilities license) => { await SetWindowTitleTextAsync(_mainViewModel.LoggedOn); });
        //_mainViewModel.BindPropertyAsyncChanged(nameof(_mainViewModel.LoggedOn), async (bool loggedOn) => { await ConfigureLinkLabelAsync(New<KnownIdentities>().DefaultEncryptionIdentity); });
        _mainViewModel.BindPropertyAsyncChanged(nameof(_mainViewModel.LoggedOn), async (bool loggedOn) => { await SetSignInSignOutStatusAsync(loggedOn); });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.OpenEncryptedEnabled), (bool enabled) => { _openEncryptedToolStripMenuItem.Enabled = enabled; });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.RandomRenameEnabled), (bool enabled) => { _renameToolStripMenuItem.Enabled = enabled; });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.WatchedFoldersEnabled), (bool enabled) => { ConfigureWatchedFoldersMenus(enabled); });
        //_checkForUpdateToolStripMenuItem.Click += async (sender, e) => { _userInitiatedUpdateCheckPending = true; await _mainViewModel.AxCryptUpdateCheck.ExecuteAsync(DateTime.MinValue); };
        //_debugCheckVersionNowToolStripMenuItem.Click += async (sender, e) => { _userInitiatedUpdateCheckPending = true; await _mainViewModel.AxCryptUpdateCheck.ExecuteAsync(DateTime.MinValue); };
        //_mainToolStripTableLayout.DragOver += async (sender, e) => { _mainViewModel.DragAndDropFiles = e.GetDragged(); e.Effect = await GetEffectsForMainToolStripAsync(e); };
        //_optionsDebugToolStripMenuItem.Click += (sender, e) => { _mainViewModel.DebugMode = !_mainViewModel.DebugMode; };
        //_watchedFoldersListView.DragDrop += async (sender, e) => { await PremiumFeature_ClickAsync(LicenseCapability.SecureFolders, (ss, ee) => { return _mainViewModel.AddWatchedFolders.ExecuteAsync(_mainViewModel.DragAndDropFiles); }, sender, e); };
        //_watchedFoldersListView.DragOver += (sender, e) => { _mainViewModel.DragAndDropFiles = e.GetDragged(); e.Effect = GetEffectsForWatchedFolders(e); };
        //_watchedFoldersListView.MouseDown += (sender, e) => { if (e.Button == MouseButtons.Right) { ShowHideWatchedFoldersContextMenuItems(e.Location); _watchedFoldersContextMenuStrip.Show((Control)sender, e.Location); } };
        //_watchedFoldersListView.SelectedIndexChanged += (sender, e) => { _mainViewModel.SelectedWatchedFolders = _watchedFoldersListView.SelectedItems.Cast<ListViewItem>().Select(lvi => lvi.Text); };
        //_getPremiumToolStripMenuItem.Click += async (sender, e) => { await New<PremiumManager>().BuyPremium(New<KnownIdentities>().DefaultEncryptionIdentity); };

        //_documentsToolStripButton.Click += async (sender, e) => { KnownFolder_OnClick(sender, e); };
        //_oneDriveToolStripButton.Click += async (sender, e) => { KnownFolder_OnClick(sender, e); };
        //_googleDriveToolStripButton.Click += async (sender, e) => { KnownFolder_OnClick(sender, e); };
        //_dropBoxToolStripButton.Click += async (sender, e) => { KnownFolder_OnClick(sender, e); };
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
    private void BindToFileOperationViewModel()
    {
        _logOnViewModel!.OnLogOnOrLogOffAndLogOnAgain = async () => await New<IUIThread>().SendToAsync(async () => await LogOnOrLogOffAndLogOnAgainAsync());
    }

    private async Task LogOnOrLogOffAndLogOnAgainAsync()
    {
        bool wasLoggedOn = Resolve.KnownIdentities.IsLoggedOn;
        if (wasLoggedOn)
        {
            await _fileOperationViewModel!.IdentityViewModel.LogOnLogOff.ExecuteAsync(null!);
        }
        else
        {
            await SignInAsync();
        }
        bool didLogOff = wasLoggedOn && !Resolve.KnownIdentities.IsLoggedOn;
        if (didLogOff)
        {
            await SignInAsync();
        }
    }

    public bool IsSigningIn { get; set; }

    public async Task SignIn()
    {
        await _fileOperationViewModel!.IdentityViewModel.LogOnAsync.ExecuteAsync(null!);
    }

    private async Task SignInAsync()
    {
        SignUpSignIn signUpSignIn = new SignUpSignIn(_registerViewModel!)
        {
            Version = _apiVersion!,
            UserEmail = New<UserSettings>().UserEmail,
        };

        await signUpSignIn.DialogsAsync(this);

        New<UserSettings>().UserEmail = signUpSignIn.UserEmail;

        if (signUpSignIn.StopAndExit)
        {
            await new ApplicationManager().StopAndExit();
            return;
        }

        await SetSignInSignOutStatusAsync(_mainViewModel!.LoggedOn);
        if (_mainViewModel.LoggedOn && Thread.CurrentThread.CurrentUICulture.Name != Resolve.UserSettings.CultureName)
        {
            await SetLanguageAsync(Resolve.UserSettings.CultureName);
        }

        ShowRenewSubscriptionDialog();
    }

    private async Task SetSignInSignOutStatusAsync(bool isSignedIn)
    {
        await SetWindowTitleTextAsync(isSignedIn);
        bool isSignedInWithAxCryptId = New<KnownIdentities>().IsLoggedOnWithAxCryptId;

        //_createAccountToolStripMenuItem.Enabled = !isSignedIn;
        //_debugManageAccountToolStripMenuItem.Enabled = isSignedInWithAxCryptId;
        //_exportMyPrivateKeyToolStripMenuItem.Enabled = isSignedInWithAxCryptId;
        //_exportSharingKeyToolStripMenuItem.Enabled = isSignedInWithAxCryptId;
        //_importMyPrivateKeyToolStripMenuItem.Enabled = !isSignedIn;
        //_importOthersSharingKeyToolStripMenuItem.Enabled = isSignedInWithAxCryptId;
        //_inviteUserToolStripMenuItem.Enabled = New<AxCryptOnlineState>().IsOnline && isSignedIn;
        //_optionsEncryptionUpgradeModeToolStripMenuItem.Enabled = isSignedInWithAxCryptId;
        //_optionsChangePassphraseToolStripMenuItem.Enabled = New<AxCryptOnlineState>().IsOnline;
        //_passwordResetToolStripMenuItem.Enabled = !isSignedIn && !string.IsNullOrEmpty(New<UserSettings>().UserEmail);
        //_signInToolStripMenuItem.Visible = !isSignedIn;
        //_notifySignInToolStripMenuItem.Visible = !isSignedIn;
        //_signOutToolStripMenuItem.Visible = isSignedIn;
        //_notifySignOutToolStripMenuItem.Visible = isSignedIn;
        //_encryptionUpgradeMenuItem.Enabled = isSignedInWithAxCryptId;
    }

    private async Task SetWindowTitleTextAsync(bool isLoggedOn)
    {
        try
        {
            string windowTitle = await new Display().WindowTitleTextAsync(isLoggedOn);
            App.SetAppWindowTitle(windowTitle);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private bool _userInitiatedUpdateCheckPending = false;

    private async Task DisplayUpdateCheckPopups()
    {
        await new Display().UpdateCheckPopups(_userInitiatedUpdateCheckPending, _mainViewModel!.DownloadVersion);
        _userInitiatedUpdateCheckPending = false;
    }

    private async Task SetLanguageAsync(string cultureName)
    {
        Resolve.UserSettings.CultureName = cultureName;
        if (Resolve.Log.IsInfoEnabled)
        {
            Resolve.Log.LogInfo("Set new UI language culture to '{0}'.".InvariantFormat(Resolve.UserSettings.CultureName));
        }

        UpdateArabicStyle();

        InitializeContentResources();
        await SetWindowTitleTextAsync(_mainViewModel!.LoggedOn);
        //_daysLeftPremiumLabel.UpdateText();
        await SetSoftwareStatus();
    }

    private void UpdateArabicStyle()
    {
        //if (Resolve.UserSettings.CultureName == "ar-AR")
        //{
        //    this.RightToLeft = RightToLeft.Yes;
        //    return;
        //}

        //this.RightToLeft = RightToLeft.No;
    }

    private async Task SetSoftwareStatus()
    {
        //_softwareStatusButton.Image = Resources.bulb_green_40px;
        //_softwareStatusButton.Visible = true;
        //VersionUpdateStatus status = _mainViewModel.VersionUpdateStatus;
        //switch (status)
        //{
        //    case VersionUpdateStatus.ShortTimeSinceLastSuccessfulCheck:
        //    case VersionUpdateStatus.IsUpToDate:
        //        _softwareStatusButton.Visible = false;
        //        break;

        //    case VersionUpdateStatus.LongTimeSinceLastSuccessfulCheck:
        //        _softwareStatusButton.ToolTipText = Texts.OldVersionTooltip;
        //        break;

        //    case VersionUpdateStatus.NewerVersionIsAvailable:
        //        _softwareStatusButton.ToolTipText = Texts.NewVersionIsAvailableText.InvariantFormat(_mainViewModel.DownloadVersion.Version) + ' ' + Texts.ClickToDownloadText;
        //        break;

        //    case VersionUpdateStatus.Unknown:
        //        _softwareStatusButton.ToolTipText = Texts.ClickToCheckForNewerVersionTooltip;
        //        break;
        //}
    }

    private void InitializeContentResources()
    {
        SetCulture();
    }

    private static void SetCulture()
    {
        if (String.IsNullOrEmpty(Resolve.UserSettings.CultureName))
        {
            return;
        }

        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Resolve.UserSettings.CultureName);
    }

    private DoNotShowAgainOptions _dontShowAgainFlag;

    private void ShowRenewSubscriptionDialog()
    {
        if (_mainViewModel!.LoggedOn || !AxCryptUserAccountViewModel.HadAnyPaidSubscription)
        {
            return;
        }

        _dontShowAgainFlag = DoNotShowAgainOptions.UpgradeSubscriptionWarning;

        if (_dontShowAgainFlag != DoNotShowAgainOptions.None && New<Core.UI.UserSettings>().DoNotShowAgain.HasFlag(_dontShowAgainFlag))
        {
            return;
        }

        _logOnViewModel!.RenewSubscriptionDialog.Show();

        //using (RenewSubscriptionPromptDialog dialog = new RenewSubscriptionPromptDialog(this))
        //{
        //    if (dialog.HideDialog)
        //    {
        //        return;
        //    }

        //    if (dialog.ShowDialog(this) != DialogResult.OK)
        //    {
        //        return;
        //    }
        //}
    }

    private async Task GetApiVersionAsync()
    {
        try
        {
            _apiVersion = await New<ICache>().GetItemAsync(CacheKey.RootKey.Subkey("WrapMessageDialogsAsync_ApiVersion"), () => New<GlobalApiClient>().ApiVersionAsync(Environment.OSVersion.VersionString, New<AboutAssembly>().AssemblyVersion));
        }
        catch (ApiException aex)
        {
            await aex.HandleApiExceptionAsync();
            _apiVersion = ApiVersion.Zero;
        }
    }
}