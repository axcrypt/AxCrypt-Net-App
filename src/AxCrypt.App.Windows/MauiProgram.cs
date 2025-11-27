using AxCrypt.Abstractions;
using AxCrypt.App.Shared;
using AxCrypt.App.Shared.Desktop;
using AxCrypt.App.Shared.Desktop.Services.Interface;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Windows.Components.Pages;
using AxCrypt.App.Windows.Infrastructure;
using AxCrypt.App.Windows.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using System.Text;

namespace AxCrypt.App.Windows
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            // Register support for Windows-specific encodings
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

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
            services.AddSingleton<IWindowService, WindowService>();

            services.AddSingleton<MainPage>();
            services.AddSingleton<IndexPage>();
            services.AddSingleton<SecuredFolders>();
            services.AddSingleton<PasswordManagerComponent>();
            services.AddSingleton<TextEncryption>();
            services.AddSingleton<Notification>();
            services.AddSingleton<FindFiles>();
            services.AddSingleton<Vault>();
            services.AddSingleton<Support>();
            services.AddSingleton<SecuredMessengerComponent>();

            SharedFactory.RegisterSingletons(services);
            AppDesktopFactory.RegisterSingletons(services);

        }
    }
}