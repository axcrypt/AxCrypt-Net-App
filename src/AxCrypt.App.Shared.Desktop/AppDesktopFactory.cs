using AxCrypt.Abstractions;
using AxCrypt.App.Shared.CloudCore.DropBox;
using AxCrypt.App.Shared.CloudCore.GoogleDrive;
using AxCrypt.App.Shared.CloudCore.OneDrive;
using AxCrypt.App.Shared.Desktop.CloudCore;
using AxCrypt.App.Shared.Desktop.Code;
using AxCrypt.App.Shared.Desktop.Components.Pages;
using AxCrypt.App.Shared.Desktop.Services;
using AxCrypt.App.Shared.Desktop.ViewModels;
using AxCrypt.App.Shared.Desktop.ViewModels.FileBrowser;
using AxCrypt.App.Shared.Desktop.ViewModels.Home;
using AxCrypt.App.Shared.Desktop.ViewModels.Main;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Core.Authenticator.Service;
using AxCrypt.Core.SecuredMessenger;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace AxCrypt.App.Shared.Desktop;

public static class AppDesktopFactory
{
    public static void RegisterSingletons(IServiceCollection services)
    {
        services.AddSingleton<ICustomNavigationService, CustomNavigationService>();

        services.AddSingleton<FileDetails>();

        services.AddSingleton<TopMenuViewModel>();
        services.AddSingleton<AppSettingsViewModel>();
        services.AddSingleton<AdvancedOptionsViewModel>();
        services.AddSingleton<ActionsViewModel>();
        services.AddSingleton<SubActionViewModel>();
        services.AddSingleton<HeaderComponentViewModel>();
        services.AddSingleton<RecentFilesViewModel>();
        services.AddSingleton<ProfileViewModel>();
        services.AddSingleton<RecentFoldersViewModel>();
        services.AddSingleton<VaultSettingsViewModel>();

        services.AddSingleton<SwitchUserViewModel>();

        services.AddSingleton<SuggestionViewModel>();
        services.AddSingleton<InviteViewModel>();
        services.AddSingleton<ImportPrivateKeyViewModel>();
        services.AddSingleton<VerifyAccountDialogViewModel>();
        services.AddSingleton<VerifyPasswordViewModel>();
        services.AddSingleton<TextEncryptionViewModel>();
        services.AddSingleton<ConfirmWipeDialogViewModel>();
        services.AddSingleton<TextShareViewModel>();

        services.AddSingleton<SecuredMessage>();

        services.AddSingleton<RecentFoldersComponent>();
        services.AddSingleton<CopyToClipboardUtility>();
        services.AddSingleton<PasswordStrengthMeterViewModel>();
        services.AddSingleton<DesktopFilePickerViewModel>();

        TypeMap.Register.Singleton<IVerifySignInPassword>(() => new VerifySignInPassword());
        TypeMap.Register.Singleton<IMultiFactorAuthService>(() => new MultiFactorAuthService());
        TypeMap.Register.Singleton<ICloudPlatformService>(() => new CloudPlatformService());

        services.AddSingleton<ICloudDriveConfiguration, CloudDriveConfiguration>();
        TypeMap.Register.Singleton<ICloudDriveConfiguration>(() => new CloudDriveConfiguration());

        ICloudDriveConfiguration cloudConfig = services.BuildServiceProvider().GetRequiredService<ICloudDriveConfiguration>();
        InitializeCloudDrive(cloudConfig);
    }

    private static void InitializeCloudDrive(ICloudDriveConfiguration cloudConfig)
    {
        DropBoxConfiguration.Initialize(cloudConfig);
        new GoogleDriveConfiguration(cloudConfig);
        OneDriveConfiguration.Initialize(cloudConfig);
    }
}