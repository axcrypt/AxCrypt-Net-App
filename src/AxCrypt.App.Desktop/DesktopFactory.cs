using AxCrypt.Abstractions;
using AxCrypt.App.Desktop.Services;
using AxCrypt.App.Desktop.ViewModels;
using AxCrypt.App.Desktop.ViewModels.Feedback;
using AxCrypt.App.Desktop.ViewModels.Home;
using AxCrypt.App.Desktop.ViewModels.Main;
using AxCrypt.App.Desktop.ViewModels.Notification;
using AxCrypt.App.Desktop.ViewModels.Secret;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AxCrypt.App.Desktop;

public static class DesktopFactory
{
    public static void RegisterSingletons(IServiceCollection services)
    {
        services.AddSingleton<ProgressBarService>();

        services.AddSingleton<LogOnViewModel>();
        services.AddSingleton<RegisterViewModel>();

        services.AddSingleton<AppLocalizationOptions>();
        services.AddSingleton<TopMenuViewModel>();
        services.AddSingleton<AppSettingsViewModel>();
        services.AddSingleton<AdvancedOptionsViewModel>();
        services.AddSingleton<ActionsViewModel>();
        services.AddSingleton<SubActionViewModel>();
        services.AddSingleton<HeaderComponentViewModel>();
        services.AddSingleton<RecentFilesViewModel>();
        services.AddSingleton<ProfileViewModel>();
        services.AddSingleton<NotificationViewModel>();
        services.AddSingleton<ShareKeyViewModel>();
        services.AddSingleton<FeedbackViewModel>();

        services.AddSingleton<NewSecretViewModel>();
        services.AddSingleton<ShareSecretViewModel>();
        services.AddSingleton<ManageSecretViewModel>();
        services.AddSingleton<EditSecretViewModel>();
        services.AddSingleton<ViewSecretViewModel>();
        services.AddSingleton<UserNotificationService>();
        services.AddSingleton<NotificationViewModel>();
        services.AddSingleton<SecretsListViewModel>();

        TypeMap.Register.Singleton<AccountStatusViewModel>(() => new AccountStatusViewModel());
        //services.AddSingleton<AppSettingsComponent>();
        //services.AddSingleton<NotificationPopup>();
        //services.AddSingleton<ProfileOptionComponent>();
        //services.AddSingleton<TopMenu>();
        //services.AddSingleton<HeaderComponent>();
        //services.AddSingleton<ActionsComponent>();
        //services.AddSingleton<SubActionsComponent>();
    }

    public static void RegisterTypeFactories()
    {
    }

    public static void RegisterSingletonFactories(IServiceCollection services)
    {
    }
}