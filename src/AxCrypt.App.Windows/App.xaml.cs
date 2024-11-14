using AxCrypt.Abstractions;
using AxCrypt.Core;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Core.UI;
using System.Globalization;
using AxCrypt.App.Windows.Infrastructure;
using AxCrypt.Core.Service;
using AxCrypt.Core.Ipc;
using AxCrypt.App.Windows.Desktop;
using AxCrypt.Core.Crypto;
using AxCrypt.Content;
using AxCrypt.Core.Extensions;
using AxCrypt.Common;
using AxCrypt.App.Windows.Infrastructure.Dialogs;
using AxCrypt.Core.IO;
using System.Text.RegularExpressions;
using AxCrypt.Mono;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows;

public partial class App : Application
{
    private readonly ProgressBackgroundComponent _progressBackgroundWorker;
    private DebugLogOutputDialog _debugOutput;

    private bool _isInitializing = true;
    private CommandLine _commandLine;
    private MainViewModel _mainViewModel;

    private FileOperationViewModel _fileOperationViewModel;

    private KnownFoldersViewModel _knownFoldersViewModel;

    public App()
    {
        InitializeComponent();

        InitializeContentResources();
        RegisterTypeFactories();
        CheckLavasoftWebCompanionExistence();
        EnsureUiContextInitialized();
        EnsureFileAssociation();

        SetupViewModelsAndNotificationsBeforeAnyNotificationsAreSent();

        //HomeViewModel homeModel = new HomeViewModel(navigationManager);
        _progressBackgroundWorker = new ProgressBackgroundComponent(null);

        MainPage = new MainPage(_mainViewModel, _fileOperationViewModel, _knownFoldersViewModel);
    }

    protected override void OnStart()
    {
        base.OnStart();

        Task.Run(async () =>
        {
            try
            {
                await InitializeProgram();
            }
            catch (Exception ex)
            {
                await new ApplicationManager().ClearAllSettings();
                //MessageBox.Show(ex.Message, "AxCrypt failed to start. All Settings cleared.", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Quit();
            }
            finally
            {
                _isInitializing = false;
            }
        });
    }

    private async Task InitializeProgram()
    {
        //InitializeContentResources();
        //RegisterTypeFactories();
        //CheckLavasoftWebCompanionExistence();
        //EnsureUiContextInitialized();
        //EnsureFileAssociation();

        //if (!await new ApplicationManager().ValidateSettings())
        //{
        //    return;
        //}

        //SetupViewModelsAndNotificationsBeforeAnyNotificationsAreSent();
        CheckOfflineModeFirst();
        //await GetApiVersionAsync(); - moved
        //SetThisVersion(); - moved
        StartKeyPairService();
        AttachLogListener();
        //ConfigureUiOptions();
        SetupPathFilters();
        IntializeControls();
        InitializeMouseDownFilter();
        RestoreUserPreferences();
        BindToViewModels();
        BindToFileOperationViewModel();
        WireUpEvents();
        SetupCommandService();
        await Resolve.SessionNotify.NotifyAsync(new SessionNotification(SessionNotificationType.SessionStart));
        StartupProcessMonitor();
        ExecuteCommandLine();
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "It's not actually complex since it's just a registry.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "It's not actually complex since it's just a registry.")]
    private void RegisterTypeFactories()
    {
        TypeMap.Register.Singleton<IUIThread>(() => new UIThread(this));
        TypeMap.Register.Singleton<IProgressBackground>(() => _progressBackgroundWorker);
        TypeMap.Register.Singleton<IStatusChecker>(() => new StatusChecker());
        TypeMap.Register.Singleton<IDataItemSelection>(() => new FileFolderSelection());
        TypeMap.Register.Singleton<IDeviceLocked>(() => new DeviceLocked());
        TypeMap.Register.Singleton<IInternetState>(() => new InternetState());
        TypeMap.Register.Singleton<InstallationVerifier>(() => new InstallationVerifier());
        TypeMap.Register.Singleton<IKnownFolderImageProvider>(() => new KnownFolderImageProvider());
        TypeMap.Register.Singleton<InactivitySignOut>(() => new InactivitySignOut(New<UserSettings>().InactivitySignOutTime));
        //TypeMap.Register.Singleton<MouseDownFilter>(() => new MouseDownFilter(this));
        TypeMap.Register.Singleton<IGlobalNotification>(() => new NotifyIconGlobalNotification());

        TypeMap.Register.New<SessionNotificationHandler>(() => new SessionNotificationHandler(Resolve.FileSystemState, Resolve.KnownIdentities, New<ActiveFileAction>(), New<AxCryptFile>(), New<IStatusChecker>()));
        TypeMap.Register.New<IdentityViewModel>(() => new IdentityViewModel(Resolve.FileSystemState, Resolve.KnownIdentities, Resolve.UserSettings, Resolve.SessionNotify));
        TypeMap.Register.New<FileOperationViewModel>(() => new FileOperationViewModel(Resolve.FileSystemState, Resolve.SessionNotify, Resolve.KnownIdentities, Resolve.ParallelFileOperation, New<IStatusChecker>(), New<IdentityViewModel>()));
        TypeMap.Register.New<MainViewModel>(() => new MainViewModel(Resolve.FileSystemState, Resolve.UserSettings));
        TypeMap.Register.New<KnownFoldersViewModel>(() => new KnownFoldersViewModel(Resolve.FileSystemState, Resolve.SessionNotify, Resolve.KnownIdentities));
        TypeMap.Register.New<WatchedFoldersViewModel>(() => new WatchedFoldersViewModel(Resolve.FileSystemState));

        //TypeMap.Register.Singleton<IVersion>(() => new DesktopVersion());
        //TypeMap.Register.New<LogOnIdentity, AdditionalUserSettings>((LogOnIdentity identity) => new AdditionalUserSettings(identity));

        //TypeMap.Register.New<AboutBox>(() => new AboutBox());

        FormsTypes.Register(this);
    }

    private static void CheckLavasoftWebCompanionExistence()
    {
        if (New<InstallationVerifier>().IsLavasoftApplicationInstalled)
        {
            Texts.LavasoftWebCompanionExistenceWarning.ShowWarning(Texts.WarningTitle, DoNotShowAgainOptions.LavasoftWebCompanionExistenceWarning);
        }
    }

    private static void EnsureUiContextInitialized()
    {
        New<IUIThread>().Yield();
    }

    private static void EnsureFileAssociation()
    {
        if (New<InstallationVerifier>().IsApplicationInstalled && !New<InstallationVerifier>().IsFileAssociationOk)
        {
            Texts.FileAssociationBrokenWarning.ShowWarning(Texts.WarningTitle, DoNotShowAgainOptions.FileAssociationBrokenWarning);
        }
    }

    private void SetupViewModelsAndNotificationsBeforeAnyNotificationsAreSent()
    {
        New<LicensePolicy>();
        _mainViewModel = New<MainViewModel>();
        _fileOperationViewModel = New<FileOperationViewModel>();
        _knownFoldersViewModel = New<KnownFoldersViewModel>();
        New<SessionNotify>().AddCommand(async (notification) => await New<SessionNotificationHandler>().HandleNotificationAsync(notification));
    }

    private void CheckOfflineModeFirst()
    {
        if (_commandLine.IsOfflineCommand)
        {
            New<UserSettings>().OfflineMode = true;
        }
    }

    private static void StartKeyPairService()
    {
        if (!String.IsNullOrEmpty(Resolve.UserSettings.UserEmail))
        {
            return;
        }
        New<KeyPairService>().Start();
    }

    private void AttachLogListener()
    {
        Resolve.Log.Logged += (logger, loggingEventArgs) =>
        {
            Resolve.UIThread.PostTo(() =>
            {
                if (_debugOutput == null || !_debugOutput.IsVisible)
                {
                    return;
                }
                string formatted = "{0} {1}".InvariantFormat(New<INow>().Utc.ToString("o", CultureInfo.InvariantCulture), loggingEventArgs.Message.TrimLogMessage());
                _debugOutput.AppendText(formatted);
            });
        };
    }

    //private void ConfigureUiOptions()
    //{
    //    MessageBoxOptions = RightToLeft == RightToLeft.Yes ? MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading : 0;
    //}

    private static void SetupPathFilters()
    {
        if (OS.Current.Platform != Core.Runtime.Platform.WindowsDesktop)
        {
            return;
        }

        New<FileFilter>().AddUnencryptable(new Regex(@"\\\.dropbox$"));
        New<FileFilter>().AddUnencryptable(new Regex(@"\\desktop\.ini$"));
        New<FileFilter>().AddUnencryptable(new Regex(@".*\.tmp$"));
        New<FileFilter>().AddUnencryptable(new Regex(@"^.*\\~\$[^\\]*$"));

        AddEnvironmentVariableBasedFilePathFilter(@"^{0}(?!Temp$)", "SystemRoot");
        AddEnvironmentVariableBasedFilePathFilter(@"^{0}(?!Temp$)", "windir");
        AddEnvironmentVariableBasedFilePathFilter(@"^{0}", "ProgramFiles");
        AddEnvironmentVariableBasedFilePathFilter(@"^{0}", "ProgramFiles(x86)");
        AddEnvironmentVariableBasedFilePathFilter(@"^{0}$", "SystemDrive");

        New<FileFilter>().AddPlatformIndependent();

        AddEnvironmentVariableBasedFolderPathFilter("ProgramData");
        AddEnvironmentVariableBasedFolderPathFilter("ProgramFiles(x86)");
        AddEnvironmentVariableBasedFolderPathFilter("ProgramFiles");
        AddEnvironmentVariableBasedFolderPathFilter("SystemRoot");
        AddEnvironmentVariableBasedFolderPathFilter("APPDATA");
        AddEnvironmentVariableBasedFolderPathFilter("LOCALAPPDATA");
        AddEnvironmentVariableBasedFolderPathFilter("windir");
        AddEnvironmentVariableBasedFolderPathFilter("ProgramW6432");
    }

    private static void AddEnvironmentVariableBasedFilePathFilter(string formatRegularExpression, string name)
    {
        IDataContainer folder = name.FolderFromEnvironment();
        if (folder == null)
        {
            return;
        }
        string escapedPath = folder.FullName.Replace(@"\", @"\\");
        New<FileFilter>().AddUnencryptable(new Regex(formatRegularExpression.InvariantFormat(escapedPath)));
    }

    private static void AddEnvironmentVariableBasedFolderPathFilter(string name)
    {
        IDataContainer folder = name.FolderFromEnvironment();
        if (folder == null)
        {
            return;
        }
        New<FileFilter>().AddForbiddenFolderFilters(folder.FullName);


        //CoreWindowResizeManager coreWindowResizeManager = new CoreWindowResizeManager();
    }

    private void IntializeControls()
    {
        if (OS.Current.Platform == Core.Runtime.Platform.WindowsDesktop)
        {
            InitializeNotifyIcon();
        }

        //ResizeEnd += (sender, e) =>
        //{
        //    if (WindowState == FormWindowState.Normal)
        //    {
        //        Preferences.MainWindowHeight = Height;
        //        Preferences.MainWindowWidth = Width;
        //    }
        //};
        //Move += (sender, e) =>
        //{
        //    if (WindowState == FormWindowState.Normal)
        //    {
        //        Preferences.MainWindowLocation = Location;
        //    }
        //};
    }

    private void InitializeNotifyIcon()
    {
        //_notifyIcon.Icon = Resources.axcrypticon;
        //_notifyIcon.Visible = false;

        //_notifyIcon.DoubleClick += (object sender, EventArgs e) =>
        //{
        //    Styling.RestoreWindowWithFocus(this);
        //    New<UserSettings>().RestoreFullWindow = true;
        //};

        //_notifyAdvancedToolStripMenuItem.Click += (sender, e) =>
        //{
        //    Styling.RestoreWindowWithFocus(this);
        //    New<UserSettings>().RestoreFullWindow = true;
        //};

        //_notifyIcon.MouseClick += (sender, e) =>
        //{
        //    if (e.Button == MouseButtons.Left)
        //    {
        //        MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
        //        mi.Invoke(sender, null);
        //    }
        //};

        //Resize += (sender, e) =>
        //{
        //    switch (WindowState)
        //    {
        //        case FormWindowState.Minimized:
        //            ShowNotifyIcon();
        //            New<UserSettings>().RestoreFullWindow = false;
        //            break;

        //        case FormWindowState.Normal:
        //            _notifyIcon.Visible = false;
        //            break;
        //    }
        //};
    }

    private void ShowNotifyIcon()
    {
        //_notifyIcon.Visible = true;

        //if (!_balloonTipShown)
        //{
        //    _notifyIcon.BalloonTipTitle = Texts.AxCryptFileEncryption;
        //    _notifyIcon.BalloonTipText = Texts.TrayBalloonTooltip;
        //    _notifyIcon.ShowBalloonTip(500);

        //    _balloonTipShown = true;
        //}

        //Hide();
    }

    private void InitializeMouseDownFilter()
    {
        New<MouseDownFilter>().FormClicked += AxCryptMainForm_ClickAsync;
    }

    private async void AxCryptMainForm_ClickAsync(object sender, EventArgs e)
    {
        New<InactivitySignOut>().RestartInactivityTimer();
    }

    private void RestoreUserPreferences()
    {
        //Height = Preferences.MainWindowHeight.Fallback(Height);
        //Width = Preferences.MainWindowWidth.Fallback(Width);
        //Location = Preferences.MainWindowLocation.Fallback(Location).Safe();

        //_mainViewModel.RecentFilesComparer = GetComparer(Preferences.RecentFilesSortColumn, !Preferences.RecentFilesAscending);
        //_alwaysOfflineToolStripMenuItem.Checked = New<UserSettings>().OfflineMode;

        //ConfigureShowHideRecentFiles(New<UserSettings>().HideRecentFiles);
    }

    private void ConfigureShowHideRecentFiles(bool hideRecentFiles)
    {
        //_optionsHideRecentFilesToolStripMenuItem.Checked = hideRecentFiles;
        //_recentFilesListView.Enabled = !hideRecentFiles;
        //_recentFilesTabPage.ToolTipText = hideRecentFiles ? Texts.HideRecentFilesListTabToolTipText : string.Empty;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
    private void BindToViewModels()
    {
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.DebugMode), (bool enabled) => { UpdateDebugMode(enabled); });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.DecryptFileEnabled), (bool enabled) => { _decryptToolStripMenuItem.Enabled = enabled; });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.DownloadVersion), async (DownloadVersion dv) => { await SetSoftwareStatus(); await DisplayUpdateCheckPopups(); });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.EncryptFileEnabled), (bool enabled) => { _encryptToolStripButton.Enabled = enabled; ConfigureEncryptMenu(enabled); });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.EncryptFileEnabled), (bool enabled) => { _encryptToolStripMenuItem.Enabled = enabled; });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.FilesArePending), (bool filesArePending) => { _cleanDecryptedToolStripMenuItem.Enabled = filesArePending; });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.FilesArePending), (bool filesArePending) => { _closeAndRemoveOpenFilesToolStripButton.Visible = filesArePending; _closeAndRemoveOpenFilesToolStripButton.ToolTipText = filesArePending ? Texts.CloseAndRemoveOpenFilesToolStripButtonToolTipText : string.Empty; });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.EncryptionUpgradeMode), (EncryptionUpgradeMode mode) => _optionsEncryptionUpgradeModeToolStripMenuItem.Checked = mode == EncryptionUpgradeMode.AutoUpgrade);
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.License), async (LicenseCapabilities license) => await _knownFoldersViewModel.UpdateState.ExecuteAsync(null));
        //_mainViewModel.BindPropertyAsyncChanged(nameof(_mainViewModel.License), async (LicenseCapabilities license) => { await ConfigureMenusAccordingToPolicyAsync(license); });
        //_mainViewModel.BindPropertyAsyncChanged(nameof(_mainViewModel.License), async (LicenseCapabilities license) => { await ConfigureLinkLabelAsync(New<KnownIdentities>().DefaultEncryptionIdentity); });
        //_mainViewModel.BindPropertyAsyncChanged(nameof(_mainViewModel.License), async (LicenseCapabilities license) => { await SetWindowTitleTextAsync(_mainViewModel.LoggedOn); });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.License), (LicenseCapabilities license) => { _recentFilesListView.UpdateRecentFiles(_mainViewModel.RecentFiles); });
        //_mainViewModel.BindPropertyAsyncChanged(nameof(_mainViewModel.LoggedOn), async (bool loggedOn) => { await ConfigureLinkLabelAsync(New<KnownIdentities>().DefaultEncryptionIdentity); });
        //_mainViewModel.BindPropertyAsyncChanged(nameof(_mainViewModel.LoggedOn), async (bool loggedOn) => { if (loggedOn) New<InactivitySignOut>().RestartInactivityTimer(); });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.LoggedOn), async (bool loggedOn) => { await SetSignInSignOutStatusAsync(loggedOn); });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.LoggedOn), async (bool loggedOn) => { await new Display().LocalSignInWarningPopUpAsync(loggedOn); });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.OpenEncryptedEnabled), (bool enabled) => { _openEncryptedToolStripMenuItem.Enabled = enabled; });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.RandomRenameEnabled), (bool enabled) => { _renameToolStripMenuItem.Enabled = enabled; });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.RecentFiles), (IEnumerable<ActiveFile> files) => { _recentFilesListView.UpdateRecentFiles(files); ShowRecentFilesBackgroundImage(); });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.WatchedFolders), (IEnumerable<string> folders) => { UpdateWatchedFolders(folders); });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.WatchedFoldersEnabled), (bool enabled) => { ConfigureWatchedFoldersMenus(enabled); });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.FolderOperationMode), (FolderOperationMode SecureFolderLevel) => { _optionsIncludeSubfoldersToolStripMenuItem.Checked = SecureFolderLevel == FolderOperationMode.IncludeSubfolders ? true : false; });
        //_checkForUpdateToolStripMenuItem.Click += async (sender, e) => { _userInitiatedUpdateCheckPending = true; await _mainViewModel.AxCryptUpdateCheck.ExecuteAsync(DateTime.MinValue); };
        //_debugCheckVersionNowToolStripMenuItem.Click += async (sender, e) => { _userInitiatedUpdateCheckPending = true; await _mainViewModel.AxCryptUpdateCheck.ExecuteAsync(DateTime.MinValue); };
        //_debugOpenReportToolStripMenuItem.Click += (sender, e) => { New<IReport>().Open(); };
        //_knownFoldersViewModel.BindPropertyChanged(nameof(_knownFoldersViewModel.KnownFolders), (IEnumerable<KnownFolder> folders) => UpdateKnownFolders(folders));
        //_knownFoldersViewModel.KnownFolders = New<IKnownFoldersDiscovery>().Discover();
        //_mainToolStripTableLayout.DragOver += async (sender, e) => { _mainViewModel.DragAndDropFiles = e.GetDragged(); e.Effect = await GetEffectsForMainToolStripAsync(e); };
        //_optionsEncryptionUpgradeModeToolStripMenuItem.Click += (sender, e) => ToggleEncryptionUpgradeMode();
        //_optionsClearAllSettingsAndRestartToolStripMenuItem.Click += async (sender, e) => { if (_mainViewModel.DecryptedFiles.Any()) { await _mainViewModel.WarnIfAnyDecryptedFiles.ExecuteAsync(null); return; } await new ApplicationManager().ClearAllSettings(); await ShutDownAnd(New<IUIThread>().RestartApplication); };
        //_optionsDebugToolStripMenuItem.Click += (sender, e) => { _mainViewModel.DebugMode = !_mainViewModel.DebugMode; };
        //_optionsHideRecentFilesToolStripMenuItem.Click += (sender, e) => { SetRecentFilesHiddenState(!New<UserSettings>().HideRecentFiles); };
        //_optionsIncludeSubfoldersToolStripMenuItem.Click += async (sender, e) => { await PremiumFeature_ClickAsync(LicenseCapability.IncludeSubfolders, (ss, ee) => { return ToggleIncludeSubfoldersOption(); }, sender, e); };
        //_inactivitySignOutToolStripMenuItem.Click += async (sender, e) => { await PremiumFeature_ClickAsync(LicenseCapability.InactivitySignOut, async (ss, ee) => { }, sender, e); };
        //_recentFilesListView.ColumnClick += (sender, e) => { SetSortOrder(e.Column); };
        //_recentFilesListView.DragOver += (sender, e) => { _mainViewModel.DragAndDropFiles = e.GetDragged(); e.Effect = GetEffectsForRecentFiles(e); };
        //_recentFilesListView.MouseClick += (sender, e) => { if (e.Button == MouseButtons.Right) _recentFilesContextMenuStrip.Show((Control)sender, e.Location); };
        //_recentFilesListView.SelectedIndexChanged += (sender, e) => { _mainViewModel.SelectedRecentFiles = _recentFilesListView.SelectedItems.Cast<ListViewItem>().Select(lvi => RecentFilesListView.EncryptedPath(lvi)); };
        //_removeRecentFileToolStripMenuItem.Click += async (sender, e) => { await _mainViewModel.RemoveRecentFiles.ExecuteAsync(_mainViewModel.SelectedRecentFiles); };
        //_clearRecentFilesToolStripMenuItem.Click += async (sender, e) => { await _mainViewModel.RemoveRecentFiles.ExecuteAsync(_mainViewModel.RecentFiles.Select(files => files.EncryptedFileInfo.FullName)); };
        //_shareKeysToolStripMenuItem.Click += async (sender, e) => { await ShareKeysAsync(_mainViewModel.SelectedRecentFiles); };
        //_watchedFoldersAddSecureFolderMenuItem.Click += async (sender, e) => { await PremiumFeature_ClickAsync(LicenseCapability.SecureFolders, (ss, ee) => { WatchedFoldersAddSecureFolderMenuItem_Click(ss, ee); return Constant.CompletedTask; }, sender, e); };
        //_watchedFoldersKeySharingMenuItem.Click += async (sender, e) => { await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, (ss, ee) => { return WatchedFoldersKeySharingAsync(_mainViewModel.SelectedWatchedFolders); }, sender, e); };
        //_watchedFoldersListView.DragDrop += async (sender, e) => { await PremiumFeature_ClickAsync(LicenseCapability.SecureFolders, (ss, ee) => { return _mainViewModel.AddWatchedFolders.ExecuteAsync(_mainViewModel.DragAndDropFiles); }, sender, e); };
        //_watchedFoldersListView.DragOver += (sender, e) => { _mainViewModel.DragAndDropFiles = e.GetDragged(); e.Effect = GetEffectsForWatchedFolders(e); };
        //_watchedFoldersListView.MouseDown += (sender, e) => { if (e.Button == MouseButtons.Right) { ShowHideWatchedFoldersContextMenuItems(e.Location); _watchedFoldersContextMenuStrip.Show((Control)sender, e.Location); } };
        //_watchedFoldersListView.SelectedIndexChanged += (sender, e) => { _mainViewModel.SelectedWatchedFolders = _watchedFoldersListView.SelectedItems.Cast<ListViewItem>().Select(lvi => lvi.Text); };
        //_watchedFoldersOpenExplorerHereMenuItem.Click += (sender, e) => { _mainViewModel.OpenSelectedFolder.Execute(_mainViewModel.SelectedWatchedFolders.First()); };
        //_watchedFoldersDecryptMenuItem.Click += async (sender, e) => { await _mainViewModel.DecryptWatchedFolders.ExecuteAsync(_mainViewModel.SelectedWatchedFolders); };
        //_watchedFoldersRemoveMenuItem.Click += async (sender, e) => { await _mainViewModel.RemoveWatchedFolders.ExecuteAsync(_mainViewModel.SelectedWatchedFolders); };
        //_getPremiumToolStripMenuItem.Click += async (sender, e) => { await New<PremiumManager>().BuyPremium(New<KnownIdentities>().DefaultEncryptionIdentity); };
        //_recentFilesRestoreAnonymousNamesMenuItem.Click += async (sender, e) => await PremiumFeature_ClickAsync(LicenseCapability.RandomRename, async (ss, ee) => { await _fileOperationViewModel.RestoreRandomRenameFiles.ExecuteAsync(_mainViewModel.SelectedRecentFiles); }, sender, e);
        //_manageAccountToolStripMenuItem.Click += async (sender, e) => { RedirectToMyAxCryptIDPage(); };

        //_documentsToolStripButton.Click += async (sender, e) => { KnownFolder_OnClick(sender, e); };
        //_oneDriveToolStripButton.Click += async (sender, e) => { KnownFolder_OnClick(sender, e); };
        //_googleDriveToolStripButton.Click += async (sender, e) => { KnownFolder_OnClick(sender, e); };
        //_dropBoxToolStripButton.Click += async (sender, e) => { KnownFolder_OnClick(sender, e); };
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
    private void BindToFileOperationViewModel()
    {
        //_addSecureFolderToolStripMenuItem.Click += async (sender, e) => await PremiumFeature_ClickAsync(LicenseCapability.SecureFolders, (ss, ee) => { WatchedFoldersAddSecureFolderMenuItem_Click(ss, ee); return Task.FromResult<object>(null); }, sender, e);
        //_decryptAndRemoveFromListToolStripMenuItem.Click += async (sender, e) => { await _fileOperationViewModel.DecryptFiles.ExecuteAsync(_mainViewModel.SelectedRecentFiles); };
        //_decryptToolStripMenuItem.Click += async (sender, e) => { await _fileOperationViewModel.DecryptFiles.ExecuteAsync(null); };
        //_stopSecuringToolStripButton.Click += async (sender, e) => { await _fileOperationViewModel.DecryptFiles.ExecuteAsync(null); };
        //_encryptedFoldersToolStripMenuItem.Click += async (sender, e) => await PremiumFeature_ClickAsync(LicenseCapability.SecureFolders, (ss, ee) => { encryptedFoldersToolStripMenuItem_Click(ss, ee); return Task.FromResult<object>(null); }, sender, e);
        //_encryptToolStripButton.Click += async (sender, e) => await _fileOperationViewModel.EncryptFiles.ExecuteAsync(null);
        //_encryptToolStripButton.Tag = _fileOperationViewModel.EncryptFiles;
        //_encryptToolStripMenuItem.Click += async (sender, e) => { await _fileOperationViewModel.EncryptFiles.ExecuteAsync(null); };
        //_fileOperationViewModel.FirstLegacyOpen += (sender, e) => New<IUIThread>().SendTo(async () => await SetLegacyOpenMode(e));
        //_fileOperationViewModel.IdentityViewModel.LoggingOnAsync = async (e) => await New<IUIThread>().SendToAsync(async () => await HandleLogOn(e));
        //_fileOperationViewModel.SelectingFiles += (sender, e) => New<IUIThread>().SendTo(() => New<IDataItemSelection>().HandleSelection(e));
        //_fileOperationViewModel.ToggleEncryptionUpgradeMode += (sender, e) => New<IUIThread>().SendTo(() => ToggleEncryptionUpgradeMode());
        //_inviteUserToolStripMenuItem.Click += async (sender, e) => { await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, async (ss, ee) => { await InviteUserAsync(); }, sender, e); };
        //_keyShareToolStripButton.Click += async (sender, e) => { await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, async (ss, ee) => { await ShareKeysWithFileSelectionAsync(_mainViewModel.SelectedRecentFiles); }, sender, e); };
        //_openEncryptedToolStripButton.Click += async (sender, e) => { await _fileOperationViewModel.OpenFilesFromFolder.ExecuteAsync(string.Empty); };
        //_openEncryptedToolStripMenuItem.Click += async (sender, e) => { await _fileOperationViewModel.OpenFilesFromFolder.ExecuteAsync(string.Empty); };
        //_recentFilesListView.DragDrop += async (sender, e) => { await DropFilesOrFoldersInRecentFilesListViewAsync(); };
        //_recentFilesListView.MouseDoubleClick += async (sender, e) => { await _fileOperationViewModel.OpenFiles.ExecuteAsync(_mainViewModel.SelectedRecentFiles); };
        //_recentFilesOpenToolStripMenuItem.Click += async (sender, e) => { await _fileOperationViewModel.OpenFiles.ExecuteAsync(_mainViewModel.SelectedRecentFiles); };
        //_renameToolStripMenuItem.Click += async (sender, e) => await PremiumFeature_ClickAsync(LicenseCapability.RandomRename, async (ss, ee) => { await _fileOperationViewModel.RandomRenameFiles.ExecuteAsync(null); }, sender, e);
        //_restoreAnonymousNamesToolStripMenuItem.Click += async (sender, e) => await PremiumFeature_ClickAsync(LicenseCapability.RandomRename, async (ss, ee) => { await _fileOperationViewModel.RestoreRandomRenameFiles.ExecuteAsync(null); }, sender, e);
        //_secretsToolStripButton.Click += async (sender, e) => { await PremiumFeature_ClickAsync(LicenseCapability.PasswordManagement, (ss, ee) => { BrowseUtility.RedirectToSecretsUrl(Resolve.KnownIdentities.DefaultEncryptionIdentity.UserEmail.Address); return Task.FromResult<object>(null); }, sender, e); };
        //_secureDeleteToolStripMenuItem.Click += async (sender, e) => await PremiumFeature_ClickAsync(LicenseCapability.SecureWipe, async (ss, ee) => { await _fileOperationViewModel.WipeFiles.ExecuteAsync(null); }, sender, e);
        //_tryBrokenFileToolStripMenuItem.Click += async (sender, e) => { await _fileOperationViewModel.TryBrokenFiles.ExecuteAsync(null); };
        //_encryptionUpgradeMenuItem.Click += async (sender, e) => await _fileOperationViewModel.AsyncEncryptionUpgrade.ExecuteAsync(null);
        //_VerifyFileToolStripMenuItem.Click += async (sender, e) => { await _fileOperationViewModel.VerifyFiles.ExecuteAsync(null); };
        //_axcryptFileFormatCheckToolStripMenuItem.Click += async (sender, e) => { await _fileOperationViewModel.IntegrityCheckFiles.ExecuteAsync(null); };
        //_watchedFoldersdecryptTemporarilyMenuItem.Click += async (sender, e) => { await _fileOperationViewModel.DecryptFolders.ExecuteAsync(_mainViewModel.SelectedWatchedFolders); };
        //_watchedFoldersListView.MouseDoubleClick += async (sender, e) => { await _fileOperationViewModel.OpenFilesFromFolder.ExecuteAsync(_mainViewModel.SelectedWatchedFolders.FirstOrDefault()); };
        //_recentFilesShowInFolderToolStripMenuItem.Click += async (sender, e) => { await _fileOperationViewModel.ShowInFolder.ExecuteAsync(_mainViewModel.SelectedRecentFiles); };
    }

    private DeviceLocking _deviceLocking;

    private void WireUpEvents()
    {
        _deviceLocking = new DeviceLocking(
            async () =>
            {
                await EncryptPendingFiles();

                if (await _fileOperationViewModel.IdentityViewModel.LogOff.CanExecuteAsync(null))
                {
                    await _fileOperationViewModel.IdentityViewModel.LogOff.ExecuteAsync(null);
                }
            },
            async () =>
            {
                await ShutDownAnd(New<IUIThread>().ExitApplication);
            }
        );

        New<AxCryptOnlineState>().OnlineStateChanged += async (sender, e) =>
        {
            AxCryptOnlineState onLineState = (AxCryptOnlineState)sender;
            if (onLineState.IsOnline)
            {
                New<ICache>().RemoveItem(CacheKey.RootKey);
                New<IInternetState>().Clear();

                await New<SessionNotify>().NotifyAsync(new SessionNotification(SessionNotificationType.RefreshLicensePolicy, New<KnownIdentities>().DefaultEncryptionIdentity));
                await _mainViewModel.AxCryptUpdateCheck.ExecuteAsync(DateTime.MinValue);
            }
            New<IUIThread>().PostTo(async () =>
            {
                await SetWindowTitleTextAsync(_mainViewModel.LoggedOn);
                //await _daysLeftPremiumLabel.ConfigureAsync(New<KnownIdentities>().DefaultEncryptionIdentity);
            });
        };
        New<AxCryptOnlineState>().RaiseOnlineStateChanged();
    }
    private async Task EncryptPendingFiles()
    {
        if (_mainViewModel != null)
        {
            new ApplicationManager().WaitForBackgroundToComplete();
            await _mainViewModel.EncryptPendingFiles.ExecuteAsync(null);
            new ApplicationManager().WaitForBackgroundToComplete();
        }
    }
    private async Task ShutDownAnd(Action finalAction)
    {
        await new ApplicationManager().ShutdownBackgroundSafe();
        await EncryptPendingFiles();

        finalAction();
    }

    private async Task SetWindowTitleTextAsync(bool isLoggedOn)
    {
        //set app windowtitle
        await new Display().WindowTitleTextAsync(isLoggedOn);
    }

    private static void WireDownEvents()
    {
    }

    private void SetupCommandService()
    {
        Resolve.CommandService.Received += New<CommandHandler>().RequestReceived;
        Resolve.CommandService.StartListening();
        New<CommandHandler>().CommandComplete += AxCryptMainForm_CommandComplete;
    }
    private void AxCryptMainForm_CommandComplete(object sender, CommandCompleteEventArgs e)
    {
        Resolve.UIThread.SendToAsync(async () => await DoRequestAsync(e));
    }

    private async Task DoRequestAsync(CommandCompleteEventArgs e)
    {
        switch (e.Verb)
        {
            case CommandVerb.About:
                //New<AboutBox>().ShowNow();
                return;

            case CommandVerb.Exit:
                await new ApplicationManager().StopAndExit();
                return;
        }

        bool wasSignedIn = New<KnownIdentities>().IsLoggedOn;
        if (!wasSignedIn)
        {
            switch (e.Verb)
            {
                case CommandVerb.Show:
                    New<UserSettings>().RestoreFullWindow = true;
                    //Styling.RestoreWindowWithFocus(this);
                    break;

                case CommandVerb.ShowLogOn:
                    RestoreFormConditionally();
                    break;
            }

            switch (e.Verb)
            {
                case CommandVerb.Open:
                    await _fileOperationViewModel.OpenFiles.ExecuteAsync(e.Arguments);
                    return;

                case CommandVerb.Decrypt:
                    await _fileOperationViewModel.DecryptFiles.ExecuteAsync(e.Arguments);
                    return;

                case CommandVerb.Encrypt:
                case CommandVerb.Show:
                case CommandVerb.RandomRename:
                case CommandVerb.Wipe:
                case CommandVerb.ShowLogOn:
                    await SignInAsync();
                    break;

                default:
                    break;
            }

            switch (e.Verb)
            {
                case CommandVerb.Show:
                case CommandVerb.ShowLogOn:
                    return;
            }
        }

        if (!New<KnownIdentities>().IsLoggedOn)
        {
            return;
        }

        if (wasSignedIn)
        {
            await ShowSignedInInformationAsync(e.Verb, e.Arguments);
        }

        switch (e.Verb)
        {
            case CommandVerb.Encrypt:
                await _fileOperationViewModel.EncryptFiles.ExecuteAsync(e.Arguments);
                break;

            case CommandVerb.Decrypt:
                await _fileOperationViewModel.DecryptFiles.ExecuteAsync(e.Arguments);
                break;

            case CommandVerb.Open:
                await _fileOperationViewModel.OpenFiles.ExecuteAsync(e.Arguments);
                break;

            case CommandVerb.Wipe:
                await PremiumFeatureActionAsync(LicenseCapability.SecureWipe, () => _fileOperationViewModel.WipeFiles.ExecuteAsync(e.Arguments));
                break;

            case CommandVerb.RandomRename:
                await PremiumFeatureActionAsync(LicenseCapability.RandomRename, () => _fileOperationViewModel.RandomRenameFiles.ExecuteAsync(e.Arguments));
                break;

            case CommandVerb.Show:
                //Styling.RestoreWindowWithFocus(this);
                break;

            case CommandVerb.SetOfflineMode:
                New<UserSettings>().OfflineMode = true;
                break;

            case CommandVerb.SignOut:
                if (New<KnownIdentities>().IsLoggedOn)
                {
                    await New<KnownIdentities>().SetDefaultEncryptionIdentity(LogOnIdentity.Empty);
                }
                break;

            case CommandVerb.Register:
                BrowseUtility.RedirectToAccountWebUrl(Texts.LinkToSignUpWebPage);
                break;

            default:
                break;
        }
    }

    private void RestoreFormConditionally()
    {
        if (!New<UserSettings>().RestoreFullWindow)
        {
            return;
        }
        //Styling.RestoreWindowWithFocus(this);
    }

    private async Task SignInAsync()
    {
        //SignUpSignIn signUpSignIn = new SignUpSignIn()
        //{
        //    Version = _apiVersion,
        //    UserEmail = New<UserSettings>().UserEmail,
        //};

        //await signUpSignIn.DialogsAsync(this, this);

        //New<UserSettings>().UserEmail = signUpSignIn.UserEmail;

        //if (signUpSignIn.StopAndExit)
        //{
        //    await new ApplicationManager().StopAndExit();
        //    return;
        //}

        //await SetSignInSignOutStatusAsync(_mainViewModel.LoggedOn);
        //if (_mainViewModel.LoggedOn && Thread.CurrentThread.CurrentUICulture.Name != Resolve.UserSettings.CultureName)
        //{
        //    await SetLanguageAsync(Resolve.UserSettings.CultureName);
        //}

        //ShowRenewSubscriptionDialog();
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

    private static Task ShowSignedInInformationAsync(CommandVerb verb, IEnumerable<string> files)
    {
        if (New<UserSettings>().DoNotShowAgain.HasFlag(DoNotShowAgainOptions.SignedInSoNoPasswordRequired))
        {
            return Constant.CompletedTask;
        }

        switch (verb)
        {
            case CommandVerb.Encrypt:
                return ShowSignedInInformationAlert();

            case CommandVerb.Decrypt:
            case CommandVerb.Open:
                bool isAnyFileKeyKnown = files.Select(f => New<IDataStore>(f)).IsAnyFileKeyKnown();
                if (isAnyFileKeyKnown)
                {
                    return ShowSignedInInformationAlert();
                }
                break;

            default:
                break;
        }
        return Constant.CompletedTask;
    }

    private static Task ShowSignedInInformationAlert()
    {
        return New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.InformationTitle, Texts.NoPasswordRequiredInformationText, DoNotShowAgainOptions.SignedInSoNoPasswordRequired);
    }

    private async Task PremiumFeatureActionAsync(LicenseCapability requiredCapability, Func<Task> realHandler)
    {
        if (_mainViewModel.License.Has(requiredCapability))
        {
            await realHandler();
            return;
        }

        await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.PremiumFeatureToolTipText);
    }

    private static void StartupProcessMonitor()
    {
        TypeMap.Register.Singleton(() => new ProcessMonitor());
        New<ProcessMonitor>();
    }

    private void ExecuteCommandLine()
    {
        if (!_commandLine.CommandItems.Any() || _commandLine.IsOfflineCommand || _commandLine.IsStartCommand)
        {
            return;
        }

        Task.Run(() =>
        {
            _commandLine.Execute();
            new ExplorerRefresh().Notify();
        });
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Window window = base.CreateWindow(activationState);
        if (window != null)
        {
            window.Title = "AxCrypt 2.0.0.0 Premium";
        }

        return window;
    }

    public override void CloseWindow(Window window)
    {
        base.CloseWindow(window);
    }

    protected override void OnAppLinkRequestReceived(Uri uri)
    {
        base.OnAppLinkRequestReceived(uri);
    }

    protected override void OnResume()
    {
        //Check subscription level and resume operation
        base.OnResume();
    }

    protected override void OnSleep()
    {
        // on minimize the window
        base.OnSleep();
    }
}
