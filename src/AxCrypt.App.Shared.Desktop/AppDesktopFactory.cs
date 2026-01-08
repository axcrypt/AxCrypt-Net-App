using AxCrypt.Abstractions;
using AxCrypt.Core.Authenticator.Service;
using AxCrypt.App.Shared.Desktop.Code;
using AxCrypt.App.Shared.Desktop.Components.Pages;
using AxCrypt.App.Shared.Desktop.Services;
using AxCrypt.App.Shared.Desktop.ViewModels;
using AxCrypt.App.Shared.Desktop.ViewModels.Home;
using AxCrypt.App.Shared.Desktop.ViewModels.Main;
using AxCrypt.Core.SecuredMessenger;
using AxCrypt.Core.UI;
using Microsoft.Extensions.DependencyInjection;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.App.Shared.ViewModels;
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

        services.AddSingleton<SecuredMessage>();

        services.AddSingleton<RecentFoldersComponent>();
        services.AddSingleton<CopyToClipboardUtility>();
        services.AddSingleton<PasswordStrengthMeterViewModel>();

        TypeMap.Register.Singleton<IVerifySignInPassword>(() => new VerifySignInPassword());
        TypeMap.Register.Singleton<IMultiFactorAuthService>(() => new MultiFactorAuthService());
    }
}