using AxCrypt.Abstractions;
using AxCrypt.App.Desktop.Components.Pages;
using AxCrypt.App.Desktop.Services;
using AxCrypt.App.Desktop.ViewModels;
using AxCrypt.App.Desktop.ViewModels.Feedback;
using AxCrypt.App.Desktop.ViewModels.Home;
using AxCrypt.App.Desktop.ViewModels.Main;
using AxCrypt.App.Desktop.ViewModels.Notification;
using AxCrypt.App.Desktop.ViewModels.Secret;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Desktop.ViewModels.SecuredMessenger;
using AxCrypt.Core.SecuredMessenger;
using AxCrypt.Core.UI;
using AxCrypt.Cryptor.Model;
using Microsoft.Extensions.DependencyInjection;

namespace AxCrypt.App.Desktop;

public static class AppDesktopFactory
{
    public static void RegisterSingletons(IServiceCollection services)
    {
        services.AddSingleton<ICustomNavigationService, CustomNavigationService>();
        services.AddSingleton<Shared.Services.Interface.ICssService, CssService>();
        services.AddSingleton<IStatusAlertService, StatusAlertService>();
        services.AddSingleton<ProcessIndicatorService>();
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
        services.AddSingleton<RecentFoldersViewModel>();
        services.AddSingleton<AboutViewModel>();
        services.AddSingleton<SuggestionViewModel>();
        services.AddSingleton<InviteViewModel>();
        services.AddSingleton<ImportPrivateKeyViewModel>();
        services.AddSingleton<VerifyAccountDialogViewModel>();
        services.AddSingleton<VerifyPasswordViewModel>();
        services.AddSingleton<GlobalDialogViewModel>();

        services.AddSingleton<NewSecretViewModel>();
        services.AddSingleton<ShareSecretViewModel>();
        services.AddSingleton<ManageSecretViewModel>();
        services.AddSingleton<EditSecretViewModel>();
        services.AddSingleton<ViewSecretViewModel>();
        services.AddSingleton<UserNotificationService>();
        services.AddSingleton<NotificationViewModel>();

        services.AddSingleton<SecuredMessengerListViewModel>();
        services.AddSingleton<ViewSecMsgrViewModel>();
        services.AddSingleton<NewSecMsgrViewModel>();
        services.AddSingleton<SecuredMessengerModel>();
        services.AddSingleton<SecuredMessage>();
        services.AddSingleton<ISecureMessagingService, SecureMessagingService>();

        services.AddTransient<SecretsListViewModel>();

        services.AddSingleton<RecentFoldersComponent>();

        services.AddSingleton<SecretClientModel>();
        services.AddSingleton<SecretsClientModel>();

        services.AddSingleton<LogViewModel>();

        TypeMap.Register.Singleton<IVerifySignInPassword>(() => new VerifySignInPassword());
        TypeMap.Register.Singleton<AccountStatusViewModel>(() => new AccountStatusViewModel());
    }
}