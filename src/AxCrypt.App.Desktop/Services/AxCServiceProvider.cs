using AxCrypt.App.Desktop.ViewModels;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AxCrypt.App.Desktop.Services;

public class AxCServiceProvider
{
    public AxCServiceProvider(IServiceProvider current)
    {
        Current = current;
    }

    public static TService GetService<TService>() => Current.GetService<TService>()!;

    private static IServiceProvider _currentServiceProvider;
    public static IServiceProvider Current
    {
        private set; get;
    }
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
}