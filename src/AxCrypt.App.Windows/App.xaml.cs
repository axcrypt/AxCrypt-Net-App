using AxCrypt.Abstractions;
using AxCrypt.App.Desktop;
using AxCrypt.App.Desktop.Code;
using AxCrypt.App.Desktop.ViewModels;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Windows.Infrastructure;
using AxCrypt.App.Windows.Infrastructure.Dialogs;
using AxCrypt.App.Windows.Platforms.Windows.Implementation;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.Ipc;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Desktop;
using System.Globalization;
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

    private readonly IDispatcher _dispatcher;

    public App(IDispatcher dispatcher, LogOnViewModel logOnService, RegisterViewModel registerViewModel)
    {
        _dispatcher = dispatcher;
        InitializeComponent();

        InitializeContentResources();
        RegisterTypeFactories();
        PlatformInitializer.CheckLavasoftWebCompanionExistence();
        EnsureUiContextInitialized();
        AppFactory.EnsureFileAssociation();

        SetupViewModelsAndNotificationsBeforeAnyNotificationsAreSent();

        InitializeServiceDependencyProvider();
        _progressBackgroundWorker = new ProgressBackgroundComponent();

        MainPage = new MainPage(logOnService, _mainViewModel, _fileOperationViewModel, _knownFoldersViewModel, registerViewModel);
    }

    private static void InitializeServiceDependencyProvider()
    {
        IServiceProvider _service;
#if WINDOWS10_0_17763_0_OR_GREATER
        _service = MauiWinUIApplication.Current.Services;
#elif ANDROID
            _service = MauiApplication.Current.Services;
#elif IOS || MACCATALYST
            _service = MauiUIApplicationDelegate.Current.Services;
#else
            _service = null;
#endif

        new AxCServiceProvider(_service);
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
        if (!await new ApplicationManager().ValidateSettings())
        {
            return;
        }

        CheckOfflineModeFirst();
        AppFactory.StartKeyPairService();
        AttachLogListener();
        //ConfigureUiOptions();
        PlatformInitializer.SetupPathFilters();
        IntializeControls();
        WireUpEvents();
        SetupCommandService();
        await Resolve.SessionNotify.NotifyAsync(new SessionNotification(SessionNotificationType.SessionStart));
        AppFactory.StartupProcessMonitor();
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
        TypeMap.Register.Singleton<IDataItemSelection>(() => new FileFolderSelection());
        TypeMap.Register.Singleton<IDeviceLocked>(() => new DeviceLocked());
        TypeMap.Register.Singleton<IKnownFolderImageProvider>(() => new KnownFolderImageProvider());

        //TypeMap.Register.Singleton<IVersion>(() => new DesktopVersion());

        //TypeMap.Register.New<AboutBox>(() => new AboutBox());
        PlatformInitializer.RegisterTypeFactories();
        AppFactory.RegisterTypeFactories();

        FormsTypes.Register(this);
    }


    private static void EnsureUiContextInitialized()
    {
        New<IUIThread>().Yield();
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

    private DeviceLocking _deviceLocking;

    private void WireUpEvents()
    {
        _deviceLocking = new DeviceLocking(
            async () =>
            {
                await EncryptPendingFiles();

                if (await _fileOperationViewModel.IdentityViewModel.LogOffLogOn.CanExecuteAsync(null))
                {
                    await _fileOperationViewModel.IdentityViewModel.LogOffLogOn.ExecuteAsync(null);
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
        string appTitle = await new Display().WindowTitleTextAsync(isLoggedOn);
        SetAppWindowTitle(appTitle);
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
                    AppFactory.RestoreFormConditionally();
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
            await AppFactory.ShowSignedInInformationAsync(e.Verb, e.Arguments);
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

  
    private async Task PremiumFeatureActionAsync(LicenseCapability requiredCapability, Func<Task> realHandler)
    {
        if (_mainViewModel.License.Has(requiredCapability))
        {
            await realHandler();
            return;
        }

        await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.PremiumFeatureToolTipText);
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

    private static Window _window;

    protected override Window CreateWindow(IActivationState? activationState)
    {
        _window = base.CreateWindow(activationState);
        if (_window != null)
        {
            _window.MinimumHeight = AppPreferences.MinimumWindowHeight;
            _window.MinimumWidth = AppPreferences.MinimumWindowWidth;
            _window.Height = AppPreferences.MinimumWindowHeight;
            _window.Width = AppPreferences.MinimumWindowWidth;

            AppFactory.RestoreUserPreferences(_window);
        }

        return _window!;
    }

    public static void SetAppWindowTitle(string titleText)
    {
        if (_window == null)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _window.SetValue(Window.TitleProperty, titleText);
            _window.Title = titleText;
        });
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