using AxCrypt.Abstractions;
using AxCrypt.Api;
using AxCrypt.Api.SecuredMessenger;
using AxCrypt.App.Shared.Desktop.Code;
using AxCrypt.App.Shared.Desktop.Data;
using AxCrypt.App.Shared.Desktop.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.IO;
using AxCrypt.Core.Ipc;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Service;
using AxCrypt.Core.Service.Secrets;
using AxCrypt.Core.Service.SecuredMessenger;
using AxCrypt.Core.Service.TextEncryption;
using AxCrypt.Core.Service.UserNotification;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Desktop;
using AxCrypt.Mono;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;
using INotificationService = AxCrypt.Core.Service.UserNotification.INotificationService;

namespace AxCrypt.App.Shared.Desktop;

public class AppFactory
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "It's not actually complex since it's just a registry.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "It's not actually complex since it's just a registry.")]
    public static void RegisterTypeFactories()
    {
        TypeMap.Register.Singleton<IGlobalNotification>(() => new NotifyIconGlobalNotification());
        TypeMap.Register.Singleton<InactivitySignOut>(() => new InactivitySignOut(New<UserSettings>().InactivitySignOutTime, null));
        TypeMap.Register.Singleton<IStatusChecker>(() => new StatusChecker());

        TypeMap.Register.Singleton<SecretSecureStorage>(() => new SecretSecureStorage());
        TypeMap.Register.Singleton<TransientProtectedData>(() => new TransientProtectedData(New<SecretSecureStorage>().AppUserSecretKey));

        TypeMap.Register.New<SessionNotificationHandler>(() => new SessionNotificationHandler(Resolve.FileSystemState, Resolve.KnownIdentities, New<ActiveFileAction>(), New<AxCryptFile>(), New<IStatusChecker>()));
        TypeMap.Register.New<IdentityViewModel>(() => new IdentityViewModel(Resolve.FileSystemState, Resolve.KnownIdentities, Resolve.UserSettings, Resolve.SessionNotify));
        TypeMap.Register.New<FileOperationViewModel>(() => new FileOperationViewModel(Resolve.FileSystemState, Resolve.SessionNotify, Resolve.KnownIdentities, Resolve.ParallelFileOperation, New<IStatusChecker>(), New<IdentityViewModel>()));
        TypeMap.Register.New<MainViewModel>(() => new MainViewModel(Resolve.FileSystemState, Resolve.UserSettings));
        TypeMap.Register.New<KnownFoldersViewModel>(() => new KnownFoldersViewModel(Resolve.FileSystemState, Resolve.SessionNotify, Resolve.KnownIdentities));
        TypeMap.Register.New<WatchedFoldersViewModel>(() => new WatchedFoldersViewModel(Resolve.FileSystemState));

        TypeMap.Register.New<LogOnIdentity, AdditionalUserSettings>((LogOnIdentity identity) => new AdditionalUserSettings(identity));
        TypeMap.Register.New<LogOnIdentity, IAccountService>((LogOnIdentity identity) => new CachingAccountService(new DeviceAccountService(new LocalAccountService(identity, Resolve.WorkFolder.FileInfo), new ApiAccountService(new AxCryptApiClient(identity.ToRestIdentity(), Resolve.UserSettings.RestApiBaseUrl, Resolve.UserSettings.ApiTimeout)))));
        TypeMap.Register.New<LogOnIdentity, ISecretsService>((LogOnIdentity identity) => new DeviceSecretsService(new LocalSecretsService(identity, Resolve.WorkFolder.FileInfo), new NullSecretsService(identity)));
        TypeMap.Register.New<LogOnIdentity, ISecretsService>((LogOnIdentity identity) => new CachingSecretsService(new DeviceSecretsService(new LocalSecretsService(identity, Resolve.WorkFolder.FileInfo), new ApiSecretsService(new AxSecretsApiClient(identity.ToRestIdentity(), Resolve.UserSettings.RestApiBaseUrl, Resolve.UserSettings.ApiTimeout)))));
        TypeMap.Register.New<LogOnIdentity, INotificationService>((LogOnIdentity identity) => new DeviceNotificationService(new LocalNotificationService(), new NullNotificationService(identity)));
        TypeMap.Register.New<LogOnIdentity, INotificationService>((LogOnIdentity identity) => new CachingNotificationService(new DeviceNotificationService(new LocalNotificationService(), new ApiNotificationService(new AxNotificationApiClient(identity.ToRestIdentity(), Resolve.UserSettings.RestApiBaseUrl, Resolve.UserSettings.ApiTimeout)))));
        TypeMap.Register.New<LogOnIdentity, ISecuredMessengerService>((LogOnIdentity identity) => new DeviceSecuredMessengerService(new LocalSecuredMessengerService(identity, Resolve.WorkFolder.FileInfo), new NullSecuredMessengerService(identity)));
        TypeMap.Register.New<LogOnIdentity, ISecuredMessengerService>((LogOnIdentity identity) => new CachingSecuredMessengerService(new DeviceSecuredMessengerService(new LocalSecuredMessengerService(identity, Resolve.WorkFolder.FileInfo), new ApiSecuredMessengerService(new SecureMsgrDbApiClient(identity.ToRestIdentity(), Resolve.UserSettings.RestApiBaseUrl, Resolve.UserSettings.ApiTimeout)))));
        TypeMap.Register.New<LogOnIdentity, ITextEncryptionService>((LogOnIdentity identity) => new DeviceTextEncryptionService(new LocalTextEncryptionService(), new NullTextEncryptionService(identity)));
        TypeMap.Register.New<LogOnIdentity, ITextEncryptionService>((LogOnIdentity identity) => new CachingTextEncryptionService(new DeviceTextEncryptionService(new LocalTextEncryptionService(), new ApiTextEncryptionService(new AxTextEncryptionApiClient(identity.ToRestIdentity(), Resolve.UserSettings.RestApiBaseUrl, Resolve.UserSettings.ApiTimeout)))));

        TypeMap.Register.Singleton<IDebugLoggingWindow>(() => new LogWindowService());
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