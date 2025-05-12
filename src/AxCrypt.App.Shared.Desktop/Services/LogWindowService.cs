#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
#endif
using AxCrypt.App.Shared.Desktop.Components.Pages.LogPage;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Maui.Controls;

public static class LogWindowService
{
    private static Window _logWindow;

    public static void ShowLogWindow()
    {
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
            }
        };

        Application.Current!.OpenWindow(_logWindow);

#if WINDOWS
        object mauiWindow = _logWindow.Handler.PlatformView!;
        nint hwnd = WindowNative.GetWindowHandle(mauiWindow);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

        appWindow.Title = "Log Viewer";
        appWindow.Resize(new(500, 600));
        appWindow.Move(new(200, 200));

        OverlappedPresenter presenter = (OverlappedPresenter)appWindow.Presenter;
        presenter.IsResizable = true;
        presenter.IsMinimizable = true;

         _appWindow.Closed += (_, _) =>
        {
            _logWindow = null;
            _appWindow = null;
        };
#endif
    }

    public static void CloseLogWindow()
    {
#if WINDOWS
        _appWindow?.Close();
        _appWindow = null;
#endif
        _logWindow = null!;
        Application.Current!.CloseWindow(_logWindow);
    }
}