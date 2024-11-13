namespace AxCrypt.App.Windows.Services;

using Microsoft.AspNetCore.Components;

public interface ICustomNavigationService
{
    void NavigateTo(string uri);
}

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