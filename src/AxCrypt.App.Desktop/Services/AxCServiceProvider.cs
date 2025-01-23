using AxCrypt.App.Desktop.ViewModels;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AxCrypt.App.Desktop.Services;

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

    public static StatusAlertService? StatusAlertService
    {
        get
        {
            return GetService<StatusAlertService>();
        }
    }
}