using AxCrypt.Abstractions;
using AxCrypt.App.Desktop.Components.Pages;
using AxCrypt.App.Desktop.Services;
using AxCrypt.App.Desktop.ViewModels;
using AxCrypt.App.Desktop.ViewModels.Home;
using AxCrypt.App.Desktop.ViewModels.Main;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.Core.SecuredMessenger;
using AxCrypt.Core.UI;
using Microsoft.Extensions.DependencyInjection;
namespace AxCrypt.App.Desktop;

public static class AppDesktopFactory
{
    public static void RegisterSingletons(IServiceCollection services)
    {
        services.AddSingleton<ICustomNavigationService, CustomNavigationService>();
        services.AddSingleton<ICssService, CssService>();
        services.AddSingleton<IStatusAlertService, StatusAlertService>();
        services.AddSingleton<ProcessIndicatorService>();
        services.AddSingleton<ProgressBarService>();

        services.AddSingleton<TopMenuViewModel>();
        services.AddSingleton<AppSettingsViewModel>();
        services.AddSingleton<AdvancedOptionsViewModel>();
        services.AddSingleton<ActionsViewModel>();
        services.AddSingleton<SubActionViewModel>();
        services.AddSingleton<HeaderComponentViewModel>();
        services.AddSingleton<RecentFilesViewModel>();
        services.AddSingleton<ProfileViewModel>();
        services.AddSingleton<RecentFoldersViewModel>();

        services.AddSingleton<SuggestionViewModel>();
        services.AddSingleton<InviteViewModel>();
        services.AddSingleton<ImportPrivateKeyViewModel>();
        services.AddSingleton<VerifyAccountDialogViewModel>();
        services.AddSingleton<VerifyPasswordViewModel>();
   
        services.AddSingleton<SecuredMessage>();

        services.AddSingleton<RecentFoldersComponent>();

        TypeMap.Register.Singleton<IVerifySignInPassword>(() => new VerifySignInPassword());    
    }
}