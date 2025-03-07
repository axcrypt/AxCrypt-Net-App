using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Models.Notification;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AxCrypt.App.Shared
{
    public static class SharedFactory
    {
        public static void RegisterSingletons(IServiceCollection services)
        {
            services.AddSingleton<FileDetails>();
            services.AddSingleton<SupportViewModel>();
            services.AddSingleton<NotificationItemViewModel>();
            services.AddSingleton<FilePasswordDialogViewModel>();

            services.AddSingleton<SecretService>();
            services.AddSingleton<SupportService>();
        }
    }
}