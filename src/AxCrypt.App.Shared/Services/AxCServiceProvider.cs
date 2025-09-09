using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AxCrypt.App.Shared.Services;

public sealed class AxCServiceProvider
{
    private static IServiceProvider _currentServiceProvider;
    public AxCServiceProvider(IServiceProvider current)
    {
        _currentServiceProvider = current;
    }

    public static TService GetService<TService>() => Current.GetService<TService>()!;

    private static IServiceProvider Current
    {
        get
        {
            if (_currentServiceProvider == null)
            {
                throw new InvalidOperationException("IServiceProvider is not initialized to access registered DI services!");
            }

            return _currentServiceProvider;
        }
    }
}