using AxCrypt.Abstractions;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Extensions;
using AxCrypt.Core;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using AxCrypt.Abstractions.Algorithm;
using AxCrypt.Api;
using AxCrypt.App.Windows.Desktop;
using AxCrypt.Common;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.IO;
using AxCrypt.Core.Service;
using AxCrypt.Core.UI;
using AxCrypt.Mono.Portable;
using AxCrypt.Mono;
using System.Reflection;
using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.App.Windows.Infrastructure;
using AxCrypt.Core.Ipc;
using Microsoft.Maui.Controls.Platform;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AxCrypt.App.Windows.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        private static string _workFolderPath;

        private MauiApp _mauiApp;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        protected override MauiApp CreateMauiApp()
        {
            if (!EnsureNetVersionUsingNothingThatCrashesTheProcess())
            {
                return null;
            }

            _workFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"AxCrypt" + Path.DirectorySeparatorChar);

            TypeMap.Register.Singleton<INow>(() => new Now());
            TypeMap.Register.Singleton<IReport>(() => new Report(_workFolderPath, 1000000));

            EmbeddedResourceManager.Initialize();

            string[] commandLineArgs = Environment.GetCommandLineArgs();

            RegisterTypeFactories(commandLineArgs[0]);
            New<IRuntimeEnvironment>().AppPath = commandLineArgs[0];

            CommandLine commandLine = new CommandLine(commandLineArgs.Skip(1));

            bool isFirstInstance = New<IRuntimeEnvironment>().IsFirstInstance;
            if (isFirstInstance && commandLine.HasCommands)
            {
                New<IRuntimeEnvironment>().IsFirstInstance = isFirstInstance = false;
                New<IRuntimeEnvironment>().RunApp("--start");
            }

            WireupEvents();

            try
            {
                if (isFirstInstance)
                {
                    RunInteractive();
                }
                else
                {
                    // RunBackground(commandLine);
                }
            }
            catch (Exception ex)
            {
                New<IReport>().Exception(ex);
                throw;
            }

            UnhandledException += CurrentDomain_UnhandledException;
            return _mauiApp;
        }

        private static bool EnsureNetVersionUsingNothingThatCrashesTheProcess()
        {
            if (Type.GetType("System.Reflection.ReflectionContext", false) != null)
            {
                return true;
            }

            AlertDialog alertDialog = new AlertDialog();
            alertDialog.Title = "AxCrypt";
            alertDialog.Content = "You need .NET 4.5 or higher installed. Click OK to download.";
            //alertDialog.PrimaryButtonClick += ((Microsoft.UI.Xaml.Controls.ContentDialog sender, Microsoft.UI.Xaml.Controls.ContentDialogButtonClickEventArgs args) => 
            //{
            //    if (!args.Cancel)
            //    {
            //        Process.Start("https://www.microsoft.com/download/details.aspx?id=30653");
            //    }
            //});
            ContentDialogResult dialogResult;
            dialogResult = Task.Run(async () =>
            {
                return await alertDialog.ShowAsync();
            }).Result;

            if (dialogResult == ContentDialogResult.Primary)
            {
                Process.Start("https://www.microsoft.com/download/details.aspx?id=30653");
            }
            //DialogResult dr = MessageBox.Show("You need .NET 4.5 or higher installed. Click OK to download.", "AxCrypt", MessageBoxButtons.OKCancel);
            //if (dr == DialogResult.OK)
            //{
            //    Process.Start("https://www.microsoft.com/download/details.aspx?id=30653");
            //}
            return false;
        }

        private static void AlertDialog_PrimaryButtonClick(Microsoft.UI.Xaml.Controls.ContentDialog sender, Microsoft.UI.Xaml.Controls.ContentDialogButtonClickEventArgs args)
        {
            throw new NotImplementedException();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Dependency registration, not real complexity")]
        private static void RegisterTypeFactories(string startPath)
        {
            RuntimeEnvironment.RegisterTypeFactories();
            TypeMap.Register.Singleton<FileLocker>(() => new FileLocker());

            IEnumerable<Assembly> extraAssemblies = LoadFromFiles(new DirectoryInfo(Path.GetDirectoryName(startPath)).GetFiles("*.dll"));
            Resolve.RegisterTypeFactories(_workFolderPath, extraAssemblies);
            DesktopFactory.RegisterTypeFactories();

            TypeMap.Register.New<IProtectedData>(() => new ProtectedDataImplementation(System.Security.Cryptography.DataProtectionScope.CurrentUser));
            TypeMap.Register.New<Core.Runtime.ILauncher>(() => new Mono.Launcher());
            TypeMap.Register.New<AxCryptHMACSHA1>(() => PortableFactory.AxCryptHMACSHA1());
            TypeMap.Register.New<HMACSHA512>(() => new Mono.Cryptography.HMACSHA512Wrapper(new HMACSHA512CryptoServiceProvider()));
            TypeMap.Register.New<Aes>(() => new AxCrypt.Mono.Cryptography.AesWrapper(new System.Security.Cryptography.AesCryptoServiceProvider()));
            TypeMap.Register.New<Sha1>(() => PortableFactory.SHA1Managed());
            TypeMap.Register.New<Sha256>(() => PortableFactory.SHA256Managed());
            TypeMap.Register.New<CryptoStreamBase>(() => PortableFactory.CryptoStream());
            TypeMap.Register.New<RandomNumberGenerator>(() => PortableFactory.RandomNumberGenerator());
            TypeMap.Register.New<LogOnIdentity, IAccountService>((LogOnIdentity identity) => new CachingAccountService(new DeviceAccountService(new LocalAccountService(identity, Resolve.WorkFolder.FileInfo), new ApiAccountService(new AxCryptApiClient(identity.ToRestIdentity(), Resolve.UserSettings.RestApiBaseUrl, Resolve.UserSettings.ApiTimeout)))));
            TypeMap.Register.New<GlobalApiClient>(() => new GlobalApiClient(Resolve.UserSettings.RestApiBaseUrl, Resolve.UserSettings.ApiTimeout));
            TypeMap.Register.New<AxCryptApiClient>(() => new AxCryptApiClient(Resolve.KnownIdentities.DefaultEncryptionIdentity.ToRestIdentity(), Resolve.UserSettings.RestApiBaseUrl, Resolve.UserSettings.ApiTimeout));
            TypeMap.Register.New<ISystemCryptoPolicy>(() => new ProCryptoPolicy());
            TypeMap.Register.New<ICryptoPolicy>(() => New<LicensePolicy>().Capabilities.CryptoPolicy);

            TypeMap.Register.Singleton<LicensePolicy>(() => new LicensePolicy());
            TypeMap.Register.Singleton<FontLoader>(() => new FontLoader());
            TypeMap.Register.Singleton<IEmailParser>(() => new EmailParser());
            TypeMap.Register.Singleton<KeyPairService>(() => new KeyPairService(0, 0, New<UserSettings>().AsymmetricKeyBits));
            TypeMap.Register.Singleton<ICache>(() => new ItemCache());
            TypeMap.Register.Singleton<DummyReferencedType>(() => new DummyReferencedType());
            TypeMap.Register.Singleton<AxCryptOnlineState>(() => new AxCryptOnlineState());
            TypeMap.Register.Singleton<IVersion>(() => new DesktopVersion());
            TypeMap.Register.Singleton<PasswordStrengthEvaluator>(() => new PasswordStrengthEvaluator(100, 8));
            TypeMap.Register.Singleton<IKnownFoldersDiscovery>(() => new KnownFoldersDiscovery());
            TypeMap.Register.Singleton<AxCrypt.Abstractions.IBrowser>(() => new AxCrypt.Mono.Browser());
            TypeMap.Register.Singleton<ILicenseAuthority>(() => new PublicLicenseAuthority());
            TypeMap.Register.Singleton<PremiumManager>(() => new PremiumManagerWithAutoTrial());
            TypeMap.Register.Singleton<AboutAssembly>(() => new AboutAssembly(Assembly.GetExecutingAssembly()));
            TypeMap.Register.Singleton<IProgressDialog>(() => new ProgressDialog());
            TypeMap.Register.Singleton<CultureNameMapper>(() => new CultureNameMapper(New<GlobalApiClient>().GetCultureInfoListAsync));
        }

        private static IEnumerable<Assembly> LoadFromFiles(IEnumerable<FileInfo> files)
        {
            List<Assembly> assemblies = new List<Assembly>();
            foreach (FileInfo file in files)
            {
                switch (file.Name.ToLowerInvariant())
                {
                    case "shellext.dll":
                    case "messages.dll":
                        continue;
                }

                try
                {
                    assemblies.Add(Assembly.LoadFrom(file.FullName));
                }
                catch (BadImageFormatException bifex)
                {
                    New<IReport>().Exception(bifex);
                    continue;
                }
                catch (FileLoadException flex)
                {
                    New<IReport>().Exception(flex);
                    continue;
                }
            }
            return assemblies;
        }

        private static void WireupEvents()
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void RunInteractive()
        {
            //EnableVisualStyles();
            //SetCompatibleTextRenderingDefault(false);
            //ThreadException += Application_ThreadException;
            //SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            try
            {
                _mauiApp = MauiProgram.CreateMauiApp();
                return;
                //Application.Run(new AxCryptMainForm(commandLine));
            }
            catch (Exception ex)
            {
                ExceptionMessageAndReport(ex);
            }
            finally
            {
                AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
                //Application.ThreadException -= Application_ThreadException;
            }
        }

        private static async void ExceptionMessageAndReport(Exception ex)
        {
            New<IReport>().Exception(ex);
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }

            //AlertDialog alertDialog = new AlertDialog();
            //alertDialog.Title = "Unhandled Exception";
            //alertDialog.Content = ex.Message;
            //await alertDialog.ShowAsync();
        }

        private void CurrentDomain_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            if (e.Exception is ApplicationExitException)
            {
                Exit();
            }
            ExceptionMessageAndReport(e.Exception as Exception);
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is ApplicationExitException)
            {
                Exit();
            }
            ExceptionMessageAndReport(e.ExceptionObject as Exception);
        }

        //private void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        //{
        //    if (e.Exception is ApplicationExitException)
        //    {
        //        Exit();
        //    }
        //    ExceptionMessageAndReport(e.Exception as Exception);
        //}

        private static void RunBackground(CommandLine commandLine)
        {
            if (!commandLine.HasCommands)
            {
                Resolve.CommandService.Call(CommandVerb.Show, -1);
                return;
            }
            commandLine.Execute();
            new ExplorerRefresh().Notify();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Microsoft.Windows.AppLifecycle.AppActivationArguments appActivationArguments = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
            if(appActivationArguments.Kind == Microsoft.Windows.AppLifecycle.ExtendedActivationKind.ToastNotification)
            {
                return;
            }

            base.OnLaunched(args);
        }
    }
}
