using AxCrypt.App.Shared.Desktop.Components.Pages.LogPage;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.Core.UI;
using Microsoft.AspNetCore.Components.WebView.Maui;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.Platforms.Windows.Implementation;

public class LogWindowService : IDebugLoggingWindow
{
    private Window? _logWindow;

    public void ShowLogWindow()
    {
        if (_logWindow != null)
        {
            New<IPopup>().ShowAsync(PopupButtons.Ok, AxCrypt.Content.Texts.InformationTitle, "Log window is already open!");
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

    public void CloseLogWindow()
    {
        Application.Current!.CloseWindow(_logWindow!);
        _logWindow = null;
    }
}