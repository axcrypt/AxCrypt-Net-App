using AxCrypt.App.Shared.Desktop.Components.Pages.LogPage;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Core.UI;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.Services
{
    public class LogWindowService : IDebugLoggingWindow
    {
        private readonly Dictionary<LogType, Window> _logWindows = new();

        public void ShowLogWindow(LogType logType)
        {
            if (_logWindows.ContainsKey(logType))
            {
                New<IPopup>().ShowAsync(PopupButtons.Ok, AxCrypt.Content.Texts.InformationTitle, $"{logType} Log window is already open!");
                return;
            }

            Type? rootWindow = logType switch
            {
                LogType.Debug => typeof(DebugLogOutput),
                LogType.FileActivity => typeof(UserActivityLog),
                _ => null
            };

            if (rootWindow == null)
            {
                return;
            }

            Window window = new Window
            {
                Page = new ContentPage
                {
                    Content = new BlazorWebView
                    {
                        HostPage = "wwwroot/index.html",
                        RootComponents =
                    {
                        new RootComponent { Selector = "#app", ComponentType = rootWindow },
                    }
                    }
                },
                Title = $"AxCrypt {logType} Log Output"
            };

            window.Destroying += (sender, args) =>
            {
                Application.Current!.CloseWindow(window);
                _logWindows.Remove(logType);
            };

            _logWindows[logType] = window;
            Application.Current!.OpenWindow(window);
        }

        public void CloseLogWindow(LogType logType)
        {
            if (_logWindows.TryGetValue(logType, out var window))
            {
                Application.Current!.CloseWindow(window);
                _logWindows.Remove(logType);
            }
        }

        public void CloseAllLogWindows()
        {
            foreach (Window window in _logWindows.Values.ToList()) // ToList to avoid collection modification
            {
                Application.Current!.CloseWindow(window);
            }

            _logWindows.Clear();
        }
    }
}
