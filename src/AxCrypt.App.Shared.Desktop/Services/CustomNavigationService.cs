using AxCrypt.App.Shared.Services.Interface;
using Microsoft.AspNetCore.Components;

namespace AxCrypt.App.Shared.Desktop.Services;

public class CustomNavigationService : ICustomNavigationService
{
    private readonly NavigationManager _navigationManager;

    public CustomNavigationService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    public void NavigateTo(string uri)
    {
        _navigationManager.NavigateTo(uri);
    }
}