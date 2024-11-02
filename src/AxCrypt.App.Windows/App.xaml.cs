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
using Microsoft.AspNetCore.Components;
using AxCrypt.App.Windows.ViewModels;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows
{
    public partial class App : Application
    {
        private readonly ProgressBackgroundComponent _progressBackgroundWorker;


        private bool _isInitializing = true;
        private CommandLine _commandLine;
        private MainViewModel _mainViewModel;

        private FileOperationViewModel _fileOperationViewModel;

        private KnownFoldersViewModel _knownFoldersViewModel;

        public App(NavigationManager navigationManager, HomeViewModel homeModel)
        {
            InitializeComponent();

            MainPage = new MainPage(navigationManager, homeModel);

            _progressBackgroundWorker = new ProgressBackgroundComponent(null);


            //PlatformInitializer.Initialize();
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
            InitializeContentResources();
            //RegisterTypeFactories();
            CheckLavasoftWebCompanionExistence();
            EnsureUiContextInitialized();
            EnsureFileAssociation();

            if (!await new ApplicationManager().ValidateSettings())
            {
                return;
            }

            SetupViewModelsAndNotificationsBeforeAnyNotificationsAreSent();
            CheckOfflineModeFirst();
            await GetApiVersionAsync();
            SetThisVersion();
            StartKeyPairService();
            //AttachLogListener();
            //ConfigureUiOptions();
            //SetupPathFilters();
            //IntializeControls();
            //InitializeMouseDownFilter();
            //RestoreUserPreferences();
            //BindToViewModels();
            //BindToFileOperationViewModel();
            //WireUpEvents();
            //SetupCommandService();
            await Resolve.SessionNotify.NotifyAsync(new SessionNotification(SessionNotificationType.SessionStart));
            //StartupProcessMonitor();
            //ExecuteCommandLine();
        }

        private void InitializeContentResources()
        {
            SetCulture();

            TypeMap.Register.Singleton<IUIThread>(() => new UIThread(this));
            TypeMap.Register.Singleton<IProgressBackground>(() => _progressBackgroundWorker);
        }

        private static void SetCulture()
        {
            if (String.IsNullOrEmpty(Resolve.UserSettings.CultureName))
            {
                return;
            }

            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Resolve.UserSettings.CultureName);
        }

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "It's not actually complex since it's just a registry.")]
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "It's not actually complex since it's just a registry.")]
        //private void RegisterTypeFactories()
        //{
        //    TypeMap.Register.Singleton<IUIThread>(() => new UIThread(this));
        //    TypeMap.Register.Singleton<IProgressBackground>(() => _progressBackgroundWorker);
        //    TypeMap.Register.Singleton<IStatusChecker>(() => new StatusChecker());
        //    TypeMap.Register.Singleton<IDataItemSelection>(() => new FileFolderSelection());
        //    //TypeMap.Register.Singleton<IDeviceLocked>(() => new DeviceLocked());
        //    TypeMap.Register.Singleton<IInternetState>(() => new InternetState());
        //    //TypeMap.Register.Singleton<InstallationVerifier>(() => new InstallationVerifier());
        //    //TypeMap.Register.Singleton<IKnownFolderImageProvider>(() => new KnownFolderImageProvider());
        //    TypeMap.Register.Singleton<InactivitySignOut>(() => new InactivitySignOut(New<UserSettings>().InactivitySignOutTime));
        //    //TypeMap.Register.Singleton<MouseDownFilter>(() => new MouseDownFilter(this));
        //    TypeMap.Register.Singleton<IGlobalNotification>(() => new NotifyIconGlobalNotification());

        //    TypeMap.Register.New<SessionNotificationHandler>(() => new SessionNotificationHandler(Resolve.FileSystemState, Resolve.KnownIdentities, New<ActiveFileAction>(), New<AxCryptFile>(), New<IStatusChecker>()));
        //    TypeMap.Register.New<IdentityViewModel>(() => new IdentityViewModel(Resolve.FileSystemState, Resolve.KnownIdentities, Resolve.UserSettings, Resolve.SessionNotify));
        //    TypeMap.Register.New<FileOperationViewModel>(() => new FileOperationViewModel(Resolve.FileSystemState, Resolve.SessionNotify, Resolve.KnownIdentities, Resolve.ParallelFileOperation, New<IStatusChecker>(), New<IdentityViewModel>()));
        //    TypeMap.Register.New<MainViewModel>(() => new MainViewModel(Resolve.FileSystemState, Resolve.UserSettings));
        //    TypeMap.Register.New<KnownFoldersViewModel>(() => new KnownFoldersViewModel(Resolve.FileSystemState, Resolve.SessionNotify, Resolve.KnownIdentities));
        //    TypeMap.Register.New<WatchedFoldersViewModel>(() => new WatchedFoldersViewModel(Resolve.FileSystemState));

        //    //TypeMap.Register.New<AboutBox>(() => new AboutBox());

        //    //FormsTypes.Register(this);
        //}

        private static void CheckLavasoftWebCompanionExistence()
        {
            //if (New<InstallationVerifier>().IsLavasoftApplicationInstalled)
            //{
            //    Texts.LavasoftWebCompanionExistenceWarning.ShowWarning(Texts.WarningTitle, DoNotShowAgainOptions.LavasoftWebCompanionExistenceWarning);
            //}
        }
        private static void EnsureUiContextInitialized()
        {
            New<IUIThread>().Yield();
        }
        private static void EnsureFileAssociation()
        {
            //if (New<InstallationVerifier>().IsApplicationInstalled && !New<InstallationVerifier>().IsFileAssociationOk)
            //{
            //    Texts.FileAssociationBrokenWarning.ShowWarning(Texts.WarningTitle, DoNotShowAgainOptions.FileAssociationBrokenWarning);
            //}
        }

        private void CheckOfflineModeFirst()
        {
            if (_commandLine.IsOfflineCommand)
            {
                New<UserSettings>().OfflineMode = true;
            }
        }

        private async Task GetApiVersionAsync()
        {
            //try
            //{
            //    _apiVersion = await New<ICache>().GetItemAsync(CacheKey.RootKey.Subkey("WrapMessageDialogsAsync_ApiVersion"), () => New<GlobalApiClient>().ApiVersionAsync(Environment.OSVersion.VersionString, New<AboutAssembly>().AssemblyVersion));
            //}
            //catch (ApiException aex)
            //{
            //    await aex.HandleApiExceptionAsync();
            //    _apiVersion = ApiVersion.Zero;
            //}
        }

        private static void SetThisVersion()
        {
            New<UserSettings>().ThisVersion = New<IVersion>().Current.ToString();
        }
        private static void StartKeyPairService()
        {
            if (!String.IsNullOrEmpty(Resolve.UserSettings.UserEmail))
            {
                return;
            }
            New<KeyPairService>().Start();
        }

        private void SetupViewModelsAndNotificationsBeforeAnyNotificationsAreSent()
        {
            New<LicensePolicy>();
            _mainViewModel = New<MainViewModel>();
            _fileOperationViewModel = New<FileOperationViewModel>();
            _knownFoldersViewModel = New<KnownFoldersViewModel>();
            New<SessionNotify>().AddCommand(async (notification) => await New<SessionNotificationHandler>().HandleNotificationAsync(notification));
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

        #region From PlatformInitializer

        private static void RunBackground(CommandLine commandLine)
        {
            if (!commandLine.HasCommands)
            {
                Resolve.CommandService.Call(CommandVerb.Show, -1);
                return;
            }
            commandLine.Execute();
            //new ExplorerRefresh().Notify();
        }

        private static Task<bool> Ensure()
        {
            return EnsureNetVersionUsingNothingThatCrashesTheProcess();
        }

        private static async Task<bool> EnsureNetVersionUsingNothingThatCrashesTheProcess()
        {
            // Check if the type "System.Reflection.ReflectionContext" exists (indicating .NET 4.5 or higher)
            if (Type.GetType("System.Reflection.ReflectionContext", false) != null)
            {
                return true;
            }

            bool result = await Application.Current.MainPage.DisplayAlert("AxCrypt", "You need .NET 4.5 or higher installed. Click OK to download.", "OK", "Cancel");

            if (result)
            {
                await Microsoft.Maui.ApplicationModel.Launcher.OpenAsync(new Uri("https://www.microsoft.com/download/details.aspx?id=30653"));
            }
            return false;
        }

        private static void WireupEvents()
        {
        }

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        //private static void RunInteractive(CommandLine commandLine)
        //{
        //    AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        //    TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        //    try
        //    {
        //        MainPage mainPage = new MainPage();
        //        //mainPage.Initialize(commandLine);
        //        Application.Current.MainPage = mainPage;
        //    }
        //    catch (Exception ex)
        //    {
        //        ExceptionMessageAndReport(ex);
        //    }
        //    finally
        //    {
        //        AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
        //        TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;
        //    }
        //}

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
        }

        private static void ExceptionMessageAndReport(Exception ex)
        {
            New<IReport>().Exception(ex);
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }
            New<IPopup>().ShowAsync(PopupButtons.Ok, "Exception", ex.Message);
        }

        //private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        //{
        //    if (e.ExceptionObject is ApplicationExitException)
        //    {
        //        Application.Exit();
        //    }
        //    ExceptionMessageAndReport(e.ExceptionObject as Exception);
        //}

        //private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        //{
        //    if (e.Exception is ApplicationExitException)
        //    {
        //        Application.Exit();
        //    }
        //    ExceptionMessageAndReport(e.Exception as Exception);
        //}

        #endregion
    }

}
