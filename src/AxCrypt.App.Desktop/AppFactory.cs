using AxCrypt.Abstractions;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Service.Secrets;
using AxCrypt.Core.Service;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Core.UI;
using AxCrypt.Core;
using AxCrypt.Core.Extensions;
using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.Core.Service.UserNotification;
using INotificationService = AxCrypt.Core.Service.UserNotification.INotificationService;
using AxCrypt.Api;
using AxCrypt.Core.Runtime;
using AxCrypt.Desktop;
using System.Threading;
using AxCrypt.Common;
using AxCrypt.Content;
using System;
using AxCrypt.Core.IO;
using System.Text.RegularExpressions;
using AxCrypt.App.Desktop.Code;
using Microsoft.Maui.Controls;
using AxCrypt.Core.Ipc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AxCrypt.Mono;

namespace AxCrypt.App.Desktop;

public class AppFactory
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "It's not actually complex since it's just a registry.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "It's not actually complex since it's just a registry.")]
    public static void RegisterTypeFactories()
    {
        TypeMap.Register.Singleton<IStatusChecker>(() => new StatusChecker());
        TypeMap.Register.Singleton<IInternetState>(() => new InternetState());
        TypeMap.Register.Singleton<InstallationVerifier>(() => new InstallationVerifier());
        TypeMap.Register.Singleton<InactivitySignOut>(() => new InactivitySignOut(New<UserSettings>().InactivitySignOutTime));

        TypeMap.Register.New<SessionNotificationHandler>(() => new SessionNotificationHandler(Resolve.FileSystemState, Resolve.KnownIdentities, New<ActiveFileAction>(), New<AxCryptFile>(), New<IStatusChecker>()));
        TypeMap.Register.New<IdentityViewModel>(() => new IdentityViewModel(Resolve.FileSystemState, Resolve.KnownIdentities, Resolve.UserSettings, Resolve.SessionNotify));
        TypeMap.Register.New<FileOperationViewModel>(() => new FileOperationViewModel(Resolve.FileSystemState, Resolve.SessionNotify, Resolve.KnownIdentities, Resolve.ParallelFileOperation, New<IStatusChecker>(), New<IdentityViewModel>()));
        TypeMap.Register.New<MainViewModel>(() => new MainViewModel(Resolve.FileSystemState, Resolve.UserSettings));
        TypeMap.Register.New<KnownFoldersViewModel>(() => new KnownFoldersViewModel(Resolve.FileSystemState, Resolve.SessionNotify, Resolve.KnownIdentities));
        TypeMap.Register.New<WatchedFoldersViewModel>(() => new WatchedFoldersViewModel(Resolve.FileSystemState));

        TypeMap.Register.New<LogOnIdentity, AdditionalUserSettings>((LogOnIdentity identity) => new AdditionalUserSettings(identity));
        TypeMap.Register.New<LogOnIdentity, IAccountService>((LogOnIdentity identity) => new CachingAccountService(new DeviceAccountService(new LocalAccountService(identity, Resolve.WorkFolder.FileInfo), new ApiAccountService(new AxCryptApiClient(identity.ToRestIdentity(), Resolve.UserSettings.RestApiBaseUrl, Resolve.UserSettings.ApiTimeout)))));
        TypeMap.Register.New<LogOnIdentity, ISecretsService>((LogOnIdentity identity) => new DeviceSecretsService(new LocalSecretsService(identity, Resolve.WorkFolder.FileInfo), new NullSecretsService(identity)));
        TypeMap.Register.New<LogOnIdentity, INotificationService>((LogOnIdentity identity) => new DeviceNotificationService(new LocalNotificationService(), new NullNotificationService(identity)));
        TypeMap.Register.New<LogOnIdentity, ISecretsService>((LogOnIdentity identity) => new CachingSecretsService(new DeviceSecretsService(new LocalSecretsService(identity, Resolve.WorkFolder.FileInfo), new ApiSecretsService(new AxSecretsApiClient(identity.ToRestIdentity(), Resolve.UserSettings.RestApiBaseUrl, Resolve.UserSettings.ApiTimeout)))));
        TypeMap.Register.New<LogOnIdentity, INotificationService>((LogOnIdentity identity) => new CachingNotificationService(new DeviceNotificationService(new LocalNotificationService(), new ApiNotificationService(new AxNotificationApiClient(identity.ToRestIdentity(), Resolve.UserSettings.RestApiBaseUrl, Resolve.UserSettings.ApiTimeout)))));
    }

    public static void CheckLavasoftWebCompanionExistence()
    {
        if (New<InstallationVerifier>().IsLavasoftApplicationInstalled)
        {
            Texts.LavasoftWebCompanionExistenceWarning.ShowWarning(Texts.WarningTitle, DoNotShowAgainOptions.LavasoftWebCompanionExistenceWarning);
        }
    }

    public static void EnsureFileAssociation()
    {
        if (New<InstallationVerifier>().IsApplicationInstalled && !New<InstallationVerifier>().IsFileAssociationOk)
        {
            Texts.FileAssociationBrokenWarning.ShowWarning(Texts.WarningTitle, DoNotShowAgainOptions.FileAssociationBrokenWarning);
        }
    }

    public static void StartKeyPairService()
    {
        if (!String.IsNullOrEmpty(Resolve.UserSettings.UserEmail))
        {
            return;
        }
        New<KeyPairService>().Start();
    }

    public static void SetupPathFilters()
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

    public static void RestoreUserPreferences(Window currentAppWindow)
    {
        if (currentAppWindow != null)
        {
            double height = currentAppWindow.Height == double.NaN ? 0 : currentAppWindow.Height;
            currentAppWindow.Height = AppPreferences.MainWindowHeight.Fallback(height);
            double width = currentAppWindow.Width == double.NaN ? 0 : currentAppWindow.Width;
            currentAppWindow.Width = AppPreferences.MainWindowWidth.Fallback(width);

            System.Drawing.Point currentLocation = new System.Drawing.Point(0, 0);
            if (!double.IsNaN(currentAppWindow.X))
            {
                currentLocation = new System.Drawing.Point((int)currentAppWindow.X, (int)currentAppWindow.Y);
            }
            System.Drawing.Point location = AppPreferences.MainWindowLocation == default(System.Drawing.Point) ? currentLocation : AppPreferences.MainWindowLocation;
            currentAppWindow.X = location.X;
            currentAppWindow.Y = location.Y;
        }

        //_mainViewModel.RecentFilesComparer = GetComparer(AppPreferences.RecentFilesSortColumn, !AppPreferences.RecentFilesAscending);
        //_alwaysOfflineToolStripMenuItem.Checked = New<UserSettings>().OfflineMode;

        //ConfigureShowHideRecentFiles(New<UserSettings>().HideRecentFiles);
    }

    public static void RestoreFormConditionally()
    {
        if (!New<UserSettings>().RestoreFullWindow)
        {
            return;
        }
        //Styling.RestoreWindowWithFocus(this);
    }

    public static Task ShowSignedInInformationAsync(CommandVerb verb, IEnumerable<string> files)
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

    public static void StartupProcessMonitor()
    {
        TypeMap.Register.Singleton(() => new ProcessMonitor());
        New<ProcessMonitor>();
    }

}
