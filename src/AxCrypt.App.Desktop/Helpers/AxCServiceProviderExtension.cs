using AxCrypt.App.Desktop.ViewModels;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Services;

namespace AxCrypt.App.Desktop.Helpers;

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
}
