using AxCrypt.Abstractions;
using AxCrypt.Api;
using AxCrypt.App.Windows.Desktop;
using AxCrypt.App.Windows.Infrastructure;
using AxCrypt.App.Windows.Infrastructure.Dialogs;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.IO;
using AxCrypt.Core.Ipc;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Session;
using AxCrypt.Core.Service;
using AxCrypt.Core.Service.Secrets;
using AxCrypt.Core.Service.UserNotification;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Mono;
using System.Globalization;
using System.Text.RegularExpressions;

using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.App.Components.Models;
using Windows.Graphics;
using AxCrypt.App.Windows.Code;

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

    private readonly IDispatcher _dispatcher;

    public App(IDispatcher dispatcher, LogOnViewModel logOnService, RegisterViewModel registerViewModel)
    {
        _dispatcher = dispatcher;
        InitializeComponent();

        InitializeContentResources();
        RegisterTypeFactories();
        CheckLavasoftWebCompanionExistence();
        EnsureUiContextInitialized();
        EnsureFileAssociation();

        SetupViewModelsAndNotificationsBeforeAnyNotificationsAreSent();

        _progressBackgroundWorker = new ProgressBackgroundComponent(null);

        MainPage = new MainPage(logOnService, _mainViewModel, _fileOperationViewModel, _knownFoldersViewModel, registerViewModel);
    }

    protected override void OnStart()
    {
        base.OnStart();

        string[] commandLineArgs = Environment.GetCommandLineArgs();
        _commandLine = new CommandLine(commandLineArgs.Skip(1));

        Task.Run(async () =>
        {
            try
            {
                await InitializeProgram();
            }
            catch (Exception ex)
            {
                await new ApplicationManager().ClearAllSettings();
                await Application.Current?.MainPage?.DisplayAlert("AxCrypt failed to start. All Settings cleared.", ex.Message, "OK")!;
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
        //BindToViewModels();
        //BindToFileOperationViewModel();
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
        TypeMap.Register.Singleton<IUIThread>(() => new UIThread(_dispatcher));
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
        TypeMap.Register.New<LogOnIdentity, AdditionalUserSettings>((LogOnIdentity identity) => new AdditionalUserSettings(identity));
        TypeMap.Register.New<LogOnIdentity, IAccountService>((LogOnIdentity identity) => new CachingAccountService(new DeviceAccountService(new LocalAccountService(identity, Resolve.WorkFolder.FileInfo), new ApiAccountService(new AxCryptApiClient(identity.ToRestIdentity(), Resolve.UserSettings.RestApiBaseUrl, Resolve.UserSettings.ApiTimeout)))));
        TypeMap.Register.New<LogOnIdentity, ISecretsService>((LogOnIdentity identity) => new DeviceSecretsService(new LocalSecretsService(identity, Resolve.WorkFolder.FileInfo), new NullSecretsService(identity)));
        TypeMap.Register.New<LogOnIdentity, INotificationService>((LogOnIdentity identity) => new DeviceNotificationService(new LocalNotificationService(), new NullNotificationService(identity)));
        TypeMap.Register.New<LogOnIdentity, ISecretsService>((LogOnIdentity identity) => new CachingSecretsService(new DeviceSecretsService(new LocalSecretsService(identity, Resolve.WorkFolder.FileInfo), new ApiSecretsService(new AxSecretsApiClient(identity.ToRestIdentity(), Resolve.UserSettings.RestApiBaseUrl, Resolve.UserSettings.ApiTimeout)))));
        TypeMap.Register.New<LogOnIdentity, INotificationService>((LogOnIdentity identity) => new CachingNotificationService(new DeviceNotificationService(new LocalNotificationService(), new ApiNotificationService(new AxNotificationApiClient(identity.ToRestIdentity(), Resolve.UserSettings.RestApiBaseUrl, Resolve.UserSettings.ApiTimeout)))));

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
    }

    private void IntializeControls()
    {
        if (OS.Current.Platform == Core.Runtime.Platform.WindowsDesktop)
        {
            InitializeNotifyIcon();
        }
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
        //New<MouseDownFilter>().FormClicked += AxCryptMainForm_ClickAsync;
    }

    private async void AxCryptMainForm_ClickAsync(object sender, EventArgs e)
    {
        New<InactivitySignOut>().RestartInactivityTimer();
    }


    private void RestoreUserPreferences(Window currentAppWindow)
    {
        if (currentAppWindow != null)
        {
            double height = currentAppWindow.Height == double.NaN ? 0 : currentAppWindow.Height;
            currentAppWindow.Height = AppPreferences.MainWindowHeight.Fallback(height);
            double width = currentAppWindow.Width == double.NaN ? 0 : currentAppWindow.Width;
            currentAppWindow.Width = AppPreferences.MainWindowWidth.Fallback(width);

            PointInt32 currentLocation = new PointInt32(0, 0);
            if (!double.IsNaN(currentAppWindow.X))
            {
                currentLocation = new PointInt32((int)currentAppWindow.X, (int)currentAppWindow.Y);
            }
            PointInt32 location = AppPreferences.MainWindowLocation == default(PointInt32) ? currentLocation : AppPreferences.MainWindowLocation;
            currentAppWindow.X = location.X;
            currentAppWindow.Y = location.Y;
        }

        //_mainViewModel.RecentFilesComparer = GetComparer(AppPreferences.RecentFilesSortColumn, !AppPreferences.RecentFilesAscending);
        //_alwaysOfflineToolStripMenuItem.Checked = New<UserSettings>().OfflineMode;

        //ConfigureShowHideRecentFiles(New<UserSettings>().HideRecentFiles);
    }

    private void ConfigureShowHideRecentFiles(bool hideRecentFiles)
    {
        //_optionsHideRecentFilesToolStripMenuItem.Checked = hideRecentFiles;
        //_recentFilesListView.Enabled = !hideRecentFiles;
        //_recentFilesTabPage.ToolTipText = hideRecentFiles ? Texts.HideRecentFilesListTabToolTipText : string.Empty;
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
                    //RestoreWindowWithFocus(this);
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
                //RestoreWindowWithFocus(this);
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

    public static void RestoreWindowWithFocus()
    {
        //if (form == null)
        //{
        //    throw new ArgumentNullException(nameof(form));
        //}

        //form.Show();
        //form.WindowState = FormWindowState.Normal;
        //form.Activate();
        //form.Focus();
        //form.BringToFront();

        //foreach (Form owned in form.OwnedForms)
        //{
        //    RestoreWindowWithFocus(owned);
        //}
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Window window = base.CreateWindow(activationState);
        if (window != null)
        {
            window.MinimumHeight = AppPreferences.MinimumWindowHeight;
            window.MinimumWidth = AppPreferences.MinimumWindowWidth;
            window.Height = AppPreferences.MinimumWindowHeight;
            window.Width = AppPreferences.MinimumWindowWidth;
            //window.Title = $"AxCrypt 2.0.0.0 File encryption made easy";
            window.Title = Task.Run(async () => { return await new Display().WindowTitleTextAsync(false); }).Result;

            RestoreUserPreferences(window);
        }

        return window;
    }

    protected override void OnAppLinkRequestReceived(Uri uri)
    {
        base.OnAppLinkRequestReceived(uri);
    }

    public override void OpenWindow(Window window)
    {
        base.OpenWindow(window);
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
