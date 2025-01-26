using Microsoft.Extensions.DependencyInjection;
using AxCrypt.App.Desktop.ViewModels.Main;
using AxCrypt.App.Desktop.ViewModels.Home;
using AxCrypt.App.Desktop.ViewModels;
using AxCrypt.App.Desktop.Components.Home;
using AxCrypt.App.Desktop.Components.Main.Header;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Desktop.Components.Main;

namespace AxCrypt.App.Desktop;

public static class DesktopFactory
{
    public static void RegisterSingletons(IServiceCollection services)
    {
        services.AddSingleton<LogOnViewModel>();
        services.AddSingleton<RegisterViewModel>();

        services.AddSingleton<AppLocalizationOptions>();
        services.AddSingleton<TopMenuViewModel>();
        services.AddSingleton<AppSettingsViewModel>();
        services.AddSingleton<AdvancedOptionsViewModel>();
        services.AddSingleton<ActionsViewModel>();
        services.AddSingleton<HeaderComponentViewModel>();
        services.AddSingleton<RecentFilesViewModel>();

        services.AddSingleton<AppSettingsComponent>();
        services.AddSingleton<NotificationPopup>();
        services.AddSingleton<ProfileViewModel>();
        services.AddSingleton<ProfileOptionComponent>();
        services.AddSingleton<TopMenu>();
        services.AddSingleton<HeaderComponent>();
        services.AddSingleton<ActionsComponent>();
    }

    public static void RegisterTypeFactories()
    {
    }

    public static void RegisterSingletonFactories(IServiceCollection services) 
    {
    }
}
