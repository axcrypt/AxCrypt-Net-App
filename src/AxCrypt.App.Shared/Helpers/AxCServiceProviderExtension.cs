using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Core.UI.ViewModel;

namespace AxCrypt.App.Shared.Helpers;

public static class AxCServiceProviderExtension
{
    public static TService GetService<TService>() => AxCServiceProvider.GetService<TService>()!;

    public static LogOnViewModel? LogOnViewModel
    {
        get
        {
            return GetService<LogOnViewModel>();
        }
    }

    public static RegisterViewModel? RegisterViewModel
    {
        get
        {
            return GetService<RegisterViewModel>();
        }
    }

    public static ProcessIndicatorService? ProcessIndicatorService
    {
        get
        {
            return GetService<ProcessIndicatorService>();
        }
    }

    public static IStatusAlertService? StatusAlertService
    {
        get
        {
            return GetService<IStatusAlertService>();
        }
    }

    public static ProgressBarService? ProgressBarService
    {
        get
        {
            return GetService<ProgressBarService>();
        }
    }

    public static ErrorReportService? ErrorReportService
    {
        get
        {
            return GetService<ErrorReportService>();
        }
    }

    public static LogViewModel? LogViewModel
    {
        get
        {
            return GetService<LogViewModel>();
        }
    }

    public static FileOperationProcessIndicatorService? FileOperationProcessIndicatorService
    {
        get
        {
            return GetService<FileOperationProcessIndicatorService>();
        }
    }

    public static UpgradeSubscriptionViewModel? UpgradeSubscriptionViewModel
    {
        get
        {
            return GetService<UpgradeSubscriptionViewModel>();
        }
    }

    public static AccountSetupViewModel? AccountSetupViewModel
    {
        get
        {
            return GetService<AccountSetupViewModel>();
        }
    }

    public static CreateNewAccountViewModel? CreateNewAccountViewModel
    {
        get
        {
            return GetService<CreateNewAccountViewModel>();
        }
    }
}
