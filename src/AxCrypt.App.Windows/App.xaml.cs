using AxCrypt.Abstractions;
using AxCrypt.App.Shared.Desktop;
using AxCrypt.App.Shared.Desktop.Code;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.App.Windows.Infrastructure;
using AxCrypt.App.Windows.Platforms.Windows.Implementation;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.Ipc;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Desktop;
using System.Globalization;
using System.Net;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows;

public partial class App : Application
{
    private readonly ProgressBackgroundComponent _progressBackgroundWorker;

    private CommandLine _commandLine;

    private MainViewModel _mainViewModel;

    private FileOperationViewModel _fileOperationViewModel;

    private LogViewModel _logViewModel;
    private LogOnViewModel _logOnViewModel;

    private readonly IDispatcher _dispatcher;

    public App(IDispatcher dispatcher, LogOnViewModel logOnViewModel, RegisterViewModel registerViewModel, LogViewModel logService, FileDropService fileDropService)
    {
        _dispatcher = dispatcher;
        _logViewModel = logService;
        _logOnViewModel = logOnViewModel;
        InitializeComponent();

        InitializeContentResources();
        RegisterTypeFactories();
        PlatformInitializer.CheckLavasoftWebCompanionExistence();
        EnsureUiContextInitialized();
        AppFactory.EnsureFileAssociation();

        SetupViewModelsAndNotificationsBeforeAnyNotificationsAreSent();

        InitializeServiceDependencyProvider();
        _progressBackgroundWorker = new ProgressBackgroundComponent();

        MainPage = new MainPage(logOnViewModel, _mainViewModel!, _fileOperationViewModel!, registerViewModel, fileDropService);
    }

    private static void InitializeServiceDependencyProvider()
    {
        IServiceProvider service;
#if WINDOWS10_0_17763_0_OR_GREATER
        service = MauiWinUIApplication.Current.Services;
#elif ANDROID
        service = MauiApplication.Current.Services;
#elif IOS || MACCATALYST
        service = MauiUIApplicationDelegate.Current.Services;
#else
        service = null;
#endif

        new AxCServiceProvider(service);
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
        await AttachLogListener();
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
        New<SessionNotify>().AddCommand(async (notification) => await New<SessionNotificationHandler>().HandleNotificationAsync(notification));
    }

    private void CheckOfflineModeFirst()
    {
        if (_commandLine.IsOfflineCommand)
        {
            New<UserSettings>().OfflineMode = true;
        }
    }

    private async Task AttachLogListener()
    {
        Resolve.Log.LoggedAsync += async (loggingEventArgs) =>
        {
            if (_logViewModel == null || !_logViewModel.IsDebugLogsVisible)
            {
                return;
            }
            string formatted = "{0} {1}".InvariantFormat(New<INow>().Utc.ToString("o", CultureInfo.InvariantCulture), loggingEventArgs.Message.TrimLogMessage());
            await _logViewModel.AddLogAsync(formatted);
        };
    }

    private void IntializeControls()
    {
        if (OS.Current.Platform == Core.Runtime.Platform.WindowsDesktop)
        {
            // Notify-icon setup is handled by the platform-specific shell integration.
        }
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

                if (_mainViewModel.LoggedOn)
                    await _mainViewModel.AxCryptUpdateCheck.ExecuteAsync(DateTime.MinValue);
            }
            New<IUIThread>().PostTo(async () =>
            {
                await SetWindowTitleTextAsync(_mainViewModel.LoggedOn);
            });
        };
        New<AxCryptOnlineState>().RaiseOnlineStateChanged();
    }

    private async Task EncryptPendingFiles()
    {
        if (_mainViewModel != null)
        {
            await new ApplicationManager().WaitForBackgroundToCompleteAsync();
            await _mainViewModel.EncryptPendingFiles.ExecuteAsync(null);
            await new ApplicationManager().WaitForBackgroundToCompleteAsync();
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

    /// <summary>
    /// Starts the IPC command listener. If the HTTP prefix is already registered
    /// (another instance running, or the OS port is still held after a crash), we
    /// log a warning and continue — the UI remains fully functional, only the
    /// single-instance IPC channel is unavailable.
    /// </summary>
    private void SetupCommandService()
    {
        Resolve.CommandService.Received += New<CommandHandler>().RequestReceived;

        try
        {
            Resolve.CommandService.StartListening();
        }
        catch (HttpListenerException ex)
        {
            // Error 183 = ERROR_ALREADY_EXISTS  (URL prefix already claimed)
            // Error 5   = ERROR_ACCESS_DENIED   (no netsh URL reservation)
            // Error 32  = ERROR_SHARING_VIOLATION (port in use by another process)
            // In all cases the app can still run — just without IPC.
            Resolve.Log.LogWarning($"IPC listener could not start (HttpListenerException {ex.ErrorCode}): {ex.Message}. " +
                "Another instance may already be running.");
        }
        catch (IOException ex)
        {
            Resolve.Log.LogWarning($"IPC listener could not start (IOException): {ex.Message}. " +
                "Another instance may already be running.");
        }

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
                _logOnViewModel.AboutDialog.Show();
                AxCServiceProvider.GetService<IWindowService>().RestoreWindowWithFocus();
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
                    AxCServiceProvider.GetService<IWindowService>().RestoreWindowWithFocus();
                    break;

                case CommandVerb.ShowLogOn:
                    AppFactory.RestoreFormConditionally();
                    AxCServiceProvider.GetService<IWindowService>().RestoreWindowWithFocus();
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
                AxCServiceProvider.GetService<IWindowService>().RestoreWindowWithFocus();
                break;

            case CommandVerb.SetOfflineMode:
                New<UserSettings>().OfflineMode = true;
                break;

            case CommandVerb.SignOut:
                AxCServiceProvider.GetService<IWindowService>().RestoreWindowWithFocus();
                if (New<KnownIdentities>().IsLoggedOn)
                {
                    await AppLifecycleHandler.SignOutSignIn();
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
        AxCServiceProvider.GetService<IWindowService>().RestoreWindowWithFocus();

        // Poll at 100 ms so a command-triggered flow resumes promptly once
        // sign-in completes, instead of waiting up to a second per tick.
        while (AxCServiceProviderExtension.LogOnViewModel!.IsVisible || !AxCServiceProviderExtension.LogOnViewModel!.IsLoggedOn)
        {
            await Task.Delay(100);
        }
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

    private static Window? _window;

    public static Window? Window => _window;

    // Preferred initial window size on first launch — large enough to
    // show the entire dashboard (TopBar + content-grid + right-column)
    // comfortably on a 1440×900 or 1600×900 screen, while still
    // shrinkable down to the minimums declared in AppPreferences.
    private const int DefaultWindowWidth = 1280;
    private const int DefaultWindowHeight = 800;

    protected override Window CreateWindow(IActivationState? activationState)
    {
        _window = base.CreateWindow(activationState);
        if (_window != null)
        {
            _window.MinimumHeight = AppPreferences.MinimumWindowHeight;
            _window.MinimumWidth = AppPreferences.MinimumWindowWidth;
            _window.Height = DefaultWindowHeight;
            _window.Width = DefaultWindowWidth;

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

        string titlePrefix = GetBuildTypeTitle();
        string appTitle = $"{titlePrefix}{titleText}";

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _window.SetValue(Window.TitleProperty, appTitle);
            _window.Title = appTitle;
        });
    }

    private static string GetBuildTypeTitle()
    {
#if AX_DEBUG_BUILD
        string buildType = "Debug";
        AppConstant.AppEnvironment = "beta";
#elif AX_BETA_BUILD
        string buildType = "Beta";
        AppConstant.AppEnvironment = "beta";
#else
        string buildType = "";
        AppConstant.AppEnvironment = "";
#endif
        return string.IsNullOrWhiteSpace(buildType) ? "" : $"[{buildType}] ";
    }
}
