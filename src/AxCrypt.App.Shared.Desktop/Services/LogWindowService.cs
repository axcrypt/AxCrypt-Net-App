using AxCrypt.App.Shared.Desktop.Components.Pages.LogPage;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Maui.Controls;

public static class LogWindowService
{
    private static Window? _logWindow;

    public static void ShowLogWindow()
    {
        if (_logWindow != null)
        {
            Application.Current!.OpenWindow(_logWindow);
            return;
        }

        _logWindow = new Window
        {
            Page = new ContentPage
            {
                Content = new BlazorWebView
                {
                    HostPage = "wwwroot/index.html",
                    RootComponents =
                    {
                        new RootComponent { Selector = "#app", ComponentType = typeof(DebugLogOutput) }
                    }
                }
            },
            Title = "AxCrypt Debug Log Output"
        };

        _logWindow.Destroying += (sender, args) =>
        {
            Application.Current!.CloseWindow(_logWindow);
            _logWindow = null; // Clear reference
        };

        Application.Current!.OpenWindow(_logWindow);
    }

    public static void CloseLogWindow()
    {
        Application.Current!.CloseWindow(_logWindow!);
        _logWindow = null;
    }
}