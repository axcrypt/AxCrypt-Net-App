using AxCrypt.Abstractions;
using AxCrypt.App.Desktop;
using AxCrypt.App.Desktop.Services;
using AxCrypt.App.Desktop.Services.Interface;
using AxCrypt.App.Desktop.ViewModels;
using AxCrypt.App.Shared;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Windows.Components.Pages;
using AxCrypt.App.Windows.Components.Pages.Main;
using AxCrypt.App.Windows.Infrastructure;
using AxCrypt.App.Windows.Services;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Cryptor.Model;
using AxCrypt.Mono;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

namespace AxCrypt.App.Windows
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            MauiAppBuilder builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            IServiceCollection services = builder.Services;
            RegisterSingletons(services);
            TypeMap.Register.Singleton<INotificationService>(() => new NotificationService());

            services.AddMauiBlazorWebView();

            builder.ConfigureLifecycleEvents(lifecycle =>
            {
                lifecycle.AddWindows(lifecycleBuilder => lifecycleBuilder.OnWindowCreated(window =>
                {
                    window.ExtendsContentIntoTitleBar = true;

                    nint handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    Microsoft.UI.WindowId id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                    Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);

                    new AppWindowExtension(appWindow).RegisterChangedEvents();
                }));
            });

#if DEBUG
            services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }

        private static void RegisterSingletons(IServiceCollection services)
        {

            services.AddSingleton<ITrayService, TrayService>();

            services.AddSingleton<IFolderPicker, FolderPickerWindows>();
            services.AddSingleton<IExportKeyManagementFile, ExportKeyManagementFile>();

            services.AddSingleton<MainPage>();
            services.AddSingleton<Home>();
            services.AddSingleton<SecuredFolders>();
            services.AddSingleton<PasswordManagerComponent>();
            services.AddSingleton<Notification>();
            services.AddSingleton<Support>();
            services.AddSingleton<SecuredMessengerComponent>();

            SharedFactory.RegisterSingletons(services);
            AppDesktopFactory.RegisterSingletons(services);

        }
    }
}
