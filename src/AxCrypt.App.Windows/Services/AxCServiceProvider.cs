using AxCrypt.App.Components.Models;
using AxCrypt.App.Components.Services;

namespace AxCrypt.App.Windows.Services;

public static class AxCServiceProvider
{
    public static TService GetService<TService>() => Current.GetService<TService>()!;

    public static IServiceProvider Current
        =>
#if WINDOWS10_0_17763_0_OR_GREATER
            MauiWinUIApplication.Current.Services;
#elif ANDROID
            MauiApplication.Current.Services;
#elif IOS || MACCATALYST
			MauiUIApplicationDelegate.Current.Services;
#else
            null;
#endif

    public static LogOnViewModel? LogOnViewModel
    {
        get
        {
            return GetService<LogOnViewModel>();
        }
    }

    public static ProcessIndicatorService? ProcessIndicatorService
    {
        get
        {
            return GetService<ProcessIndicatorService>();
        }
    }

    public static StatusAlertService? StatusAlertService
    {
        get
        {
            return GetService<StatusAlertService>();
        }
    }
}
