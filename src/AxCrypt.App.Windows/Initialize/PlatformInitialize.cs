#region Coypright and License

/*
 * AxCrypt - Copyright 2016, Svante Seleborg, All Rights Reserved
 *
 * This file is part of AxCrypt.
 *
 * AxCrypt is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * AxCrypt is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with AxCrypt.  If not, see <http://www.gnu.org/licenses/>.
 *
 * The source is maintained at http://bitbucket.org/AxCrypt.Desktop.Window-net please visit for
 * updates, contributions and contact with the author. You may also visit
 * http://www.axcrypt.net for more information about the author.
*/

#endregion Coypright and License

using AxCrypt.Abstractions;
using AxCrypt.Abstractions.Algorithm;
using AxCrypt.Api;
using AxCrypt.App.Windows.Desktop;
using AxCrypt.App.Windows.Infrastructure;
using AxCrypt.Common;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.IO;
using AxCrypt.Core.Ipc;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Service;
using AxCrypt.Core.Service.Secrets;
using AxCrypt.Core.Service.UserNotification;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Mono;
using AxCrypt.Mono.Portable;
using System.Globalization;
using System.Reflection;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.Initialize;

public class PlatformInitializer
{
    private static string _workFolderPath;

    /// <summary>
    /// The main entry point for the application.
    /// </summary>

    public static void Initialize()
    {
        InitializePrivate();
    }

    private static void InitializePrivate()
    {
        _workFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"AxCrypt" + Path.DirectorySeparatorChar);

        TypeMap.Register.Singleton<INow>(() => new Now());
        TypeMap.Register.Singleton<IReport>(() => new Report(_workFolderPath, 1000000));

        string[] commandLineArgs = Environment.GetCommandLineArgs();

        RegisterTypeFactories(commandLineArgs[0]);
        New<IRuntimeEnvironment>().AppPath = commandLineArgs[0];

        RegisterTypeFactories();
        RegisterTypeFactoriesApp();

        CommandLine commandLine = new CommandLine(commandLineArgs.Skip(1));
        //bool isFirstInstance = New<IRuntimeEnvironment>().IsFirstInstance;
        //if (isFirstInstance && commandLine.HasCommands)
        //{
        //    New<IRuntimeEnvironment>().IsFirstInstance = isFirstInstance = false;
        //    New<IRuntimeEnvironment>().RunApp("--start");
        //}

        //WireupEvents();

        //try
        //{
        //    if (isFirstInstance)
        //    {
        //        RunInteractive(commandLine);
        //    }
        //    else
        //    {
        //        RunBackground(commandLine);
        //    }
        //}
        //catch (Exception ex)
        //{
        //    New<IReport>().Exception(ex);
        //    throw;
        //}

        //Resolve.CommandService.Dispose();
        //TypeMap.Register.Clear();

        //Environment.ExitCode = 0;
    }

    //private static void RunBackground(CommandLine commandLine)
    //{
    //    if (!commandLine.HasCommands)
    //    {
    //        Resolve.CommandService.Call(CommandVerb.Show, -1);
    //        return;
    //    }
    //    commandLine.Execute();
    //    //new ExplorerRefresh().Notify();
    //}

    //private static Task<bool> Ensure()
    //{
    //    return EnsureNetVersionUsingNothingThatCrashesTheProcess();
    //}

    //private static async Task<bool> EnsureNetVersionUsingNothingThatCrashesTheProcess()
    //{
    //    // Check if the type "System.Reflection.ReflectionContext" exists (indicating .NET 4.5 or higher)
    //    if (Type.GetType("System.Reflection.ReflectionContext", false) != null)
    //    {
    //        return true;
    //    }

    //    bool result = await Application.Current.MainPage.DisplayAlert("AxCrypt", "You need .NET 4.5 or higher installed. Click OK to download.", "OK", "Cancel");

    //    if (result)
    //    {
    //        await Microsoft.Maui.ApplicationModel.Launcher.OpenAsync(new Uri("https://www.microsoft.com/download/details.aspx?id=30653"));
    //    }
    //    return false;
    //}

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Dependency registration, not real complexity")]
    private static void RegisterTypeFactories(string startPath)
    {
        RuntimeEnvironment.RegisterTypeFactories();
        DesktopFactory.RegisterTypeFactories();

        IEnumerable<Assembly> extraAssemblies = LoadFromFiles(new DirectoryInfo(Path.GetDirectoryName(startPath)).GetFiles("*.dll"));
        Resolve.RegisterTypeFactories(_workFolderPath, extraAssemblies);

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
        TypeMap.Register.New<LogOnIdentity, ISecretsService>((LogOnIdentity identity) => new DeviceSecretsService(new LocalSecretsService(identity, Resolve.WorkFolder.FileInfo), new NullSecretsService(identity)));
        TypeMap.Register.New<LogOnIdentity, INotificationService>((LogOnIdentity identity) => new DeviceNotificationService(new LocalNotificationService(), new NullNotificationService(identity)));

        TypeMap.Register.New<LogOnIdentity, ISecretsService>((LogOnIdentity identity) => new CachingSecretsService(new DeviceSecretsService(new LocalSecretsService(identity, Resolve.WorkFolder.FileInfo), new ApiSecretsService(new AxSecretsApiClient(identity.ToRestIdentity(), Resolve.UserSettings.RestApiBaseUrl, Resolve.UserSettings.ApiTimeout)))));
        TypeMap.Register.New<LogOnIdentity, INotificationService>((LogOnIdentity identity) => new CachingNotificationService(new DeviceNotificationService(new LocalNotificationService(), new ApiNotificationService(new AxNotificationApiClient(identity.ToRestIdentity(), Resolve.UserSettings.RestApiBaseUrl, Resolve.UserSettings.ApiTimeout)))));

        TypeMap.Register.New<GlobalApiClient>(() => new GlobalApiClient(Resolve.UserSettings.RestApiBaseUrl, Resolve.UserSettings.ApiTimeout));
        TypeMap.Register.New<AxCryptApiClient>(() => new AxCryptApiClient(Resolve.KnownIdentities.DefaultEncryptionIdentity.ToRestIdentity(), Resolve.UserSettings.RestApiBaseUrl, Resolve.UserSettings.ApiTimeout));
        TypeMap.Register.New<ISystemCryptoPolicy>(() => new ProCryptoPolicy());
        TypeMap.Register.New<ICryptoPolicy>(() => New<LicensePolicy>().Capabilities.CryptoPolicy);

        TypeMap.Register.Singleton<LicensePolicy>(() => new LicensePolicy());
        //TypeMap.Register.Singleton<FontLoader>(() => new FontLoader());
        TypeMap.Register.Singleton<IEmailParser>(() => new EmailParser());
        TypeMap.Register.Singleton<KeyPairService>(() => new KeyPairService(0, 0, New<UserSettings>().AsymmetricKeyBits));
        TypeMap.Register.Singleton<ICache>(() => new ItemCache());
        TypeMap.Register.Singleton<DummyReferencedType>(() => new DummyReferencedType());
        TypeMap.Register.Singleton<AxCryptOnlineState>(() => new AxCryptOnlineState());
        TypeMap.Register.Singleton<IVersion>(() => new DesktopVersion());
        TypeMap.Register.Singleton<PasswordStrengthEvaluator>(() => new PasswordStrengthEvaluator(100, 8));
        TypeMap.Register.Singleton<IKnownFoldersDiscovery>(() => new KnownFoldersDiscovery());
        TypeMap.Register.Singleton<Abstractions.IBrowser>(() => new Mono.Browser());
        TypeMap.Register.Singleton<ILicenseAuthority>(() => new PublicLicenseAuthority());
        TypeMap.Register.Singleton<PremiumManager>(() => new PremiumManagerWithAutoTrial());
        TypeMap.Register.Singleton<AboutAssembly>(() => new AboutAssembly(Assembly.GetExecutingAssembly()));
        TypeMap.Register.Singleton<FileLocker>(() => new FileLocker());
        TypeMap.Register.Singleton<IProgressDialog>(() => new ProgressDialog());
        TypeMap.Register.Singleton<CultureNameMapper>(() => new CultureNameMapper(New<GlobalApiClient>().GetCultureInfoListAsync));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "It's not actually complex since it's just a registry.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "It's not actually complex since it's just a registry.")]
    private static void RegisterTypeFactories()
    {
        TypeMap.Register.Singleton<IVersion>(() => new DesktopVersion());
        TypeMap.Register.Singleton<IPopup>(() => new PopupService());
        TypeMap.Register.Singleton<InactivitySignOut>(() => new InactivitySignOut(TimeSpan.Zero));
        TypeMap.Register.Singleton<IStatusChecker>(() => new StatusChecker());

        TypeMap.Register.New<LogOnIdentity, AdditionalUserSettings>((LogOnIdentity identity) => new AdditionalUserSettings(identity));
        TypeMap.Register.Singleton<InactivitySignOut>(() => new InactivitySignOut(New<UserSettings>().InactivitySignOutTime));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "It's not actually complex since it's just a registry.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "It's not actually complex since it's just a registry.")]
    private static void RegisterTypeFactoriesApp()
    {
        TypeMap.Register.Singleton<IStatusChecker>(() => new StatusChecker());
        TypeMap.Register.Singleton<IDataItemSelection>(() => new FileFolderSelection());
        //TypeMap.Register.Singleton<IDeviceLocked>(() => new DeviceLocked());
        TypeMap.Register.Singleton<IInternetState>(() => new InternetState());
        //TypeMap.Register.Singleton<InstallationVerifier>(() => new InstallationVerifier());
        //TypeMap.Register.Singleton<IKnownFolderImageProvider>(() => new KnownFolderImageProvider());
        TypeMap.Register.Singleton<InactivitySignOut>(() => new InactivitySignOut(New<UserSettings>().InactivitySignOutTime));
        //TypeMap.Register.Singleton<MouseDownFilter>(() => new MouseDownFilter(this));
        TypeMap.Register.Singleton<IGlobalNotification>(() => new NotifyIconGlobalNotification());

        TypeMap.Register.New<SessionNotificationHandler>(() => new SessionNotificationHandler(Resolve.FileSystemState, Resolve.KnownIdentities, New<ActiveFileAction>(), New<AxCryptFile>(), New<IStatusChecker>()));
        TypeMap.Register.New<IdentityViewModel>(() => new IdentityViewModel(Resolve.FileSystemState, Resolve.KnownIdentities, Resolve.UserSettings, Resolve.SessionNotify));
        TypeMap.Register.New<FileOperationViewModel>(() => new FileOperationViewModel(Resolve.FileSystemState, Resolve.SessionNotify, Resolve.KnownIdentities, Resolve.ParallelFileOperation, New<IStatusChecker>(), New<IdentityViewModel>()));
        TypeMap.Register.New<MainViewModel>(() => new MainViewModel(Resolve.FileSystemState, Resolve.UserSettings));
        TypeMap.Register.New<KnownFoldersViewModel>(() => new KnownFoldersViewModel(Resolve.FileSystemState, Resolve.SessionNotify, Resolve.KnownIdentities));
        TypeMap.Register.New<WatchedFoldersViewModel>(() => new WatchedFoldersViewModel(Resolve.FileSystemState));

        //TypeMap.Register.New<AboutBox>(() => new AboutBox());

        //FormsTypes.Register(this);
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
                //New<IReport>().Exception(bifex);
                continue;
            }
            catch (FileLoadException flex)
            {
                //New<IReport>().Exception(flex);
                continue;
            }
        }
        return assemblies;
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
}
