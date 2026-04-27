using AxCrypt.App.Shared.Desktop.Components.PopupDialog;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.Services;

public class FilePasswordWindowService : IFilePasswordWindowService
{
    private Window? _window;

    private UserFilePasswordViewModel? _userFilePasswordViewModel;

    public async Task<DialogResult> ShowWindow(string? encryptedFileFullName)
    {
        _userFilePasswordViewModel = AxCServiceProviderExtension.GetService<UserFilePasswordViewModel>();

        _userFilePasswordViewModel.ViewModel = new FilePasswordViewModel(encryptedFileFullName!);
        BindPropertyChangedEvents();

        _userFilePasswordViewModel.FilePasswordTcs = new TaskCompletionSource<DialogResult>();

        await Show();

        return _userFilePasswordViewModel.FilePasswordTcs.Task.Result;
    }

    private void BindPropertyChangedEvents()
    {
        _userFilePasswordViewModel!.ViewModel!.BindPropertyChanged(nameof(FilePasswordViewModel.ShowPassword), (bool show) => { _userFilePasswordViewModel.ShowVisible = !show; _userFilePasswordViewModel.DialogResult = DialogResult.None; });
        _userFilePasswordViewModel.ViewModel.BindPropertyChanged(nameof(FilePasswordViewModel.FileName), (string fileName) => { _userFilePasswordViewModel.FileName = fileName; _userFilePasswordViewModel.DialogResult = DialogResult.None; });
        _userFilePasswordViewModel.ViewModel.BindPropertyChanged(nameof(FilePasswordViewModel.IsLegacyFile), (bool isLegacy) => { _userFilePasswordViewModel.IsShowMoreVisible = isLegacy; _userFilePasswordViewModel.DialogResult = DialogResult.None; });
    }

    private async Task Show()
    {
        _userFilePasswordViewModel!.IsWindowActive = true;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            _window = CreateWindow();

            _window.HandlerChanged += OnWindowReady;

            Application.Current!.OpenWindow(_window);
        });
        _userFilePasswordViewModel.UpdateViewState();
    }

    private const string _title = "AxCrypt file encryption";

    private Window CreateWindow()
    {
        Window window = new Window
        {
            Title = _title,
            Width = 650,
            Height = 400,
            Page = new ContentPage
            {
                Content = new BlazorWebView
                {
                    HostPage = "wwwroot/index.html",
                    RootComponents =
                    {
                        new RootComponent
                        {
                            Selector = "#app",
                            ComponentType = typeof(FilePasswordWindow)
                        }
                    }
                }
            }
        };

        window.Destroying += (s, e) =>
        {
            _userFilePasswordViewModel.CancelButton_Click(e);
        };

        return window;
    }

    private void OnWindowReady(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            AxCServiceProvider.GetService<IWindowService>().FocusFilePasswordAndMinimizeMain();

            if (_window != null)
            {
                _window.Destroying += (s, args) => ValidateForRestore();
            }
        });
    }

    public void Close()
    {
        if (_window != null)
        {
            _userFilePasswordViewModel.IsWindowActive = false;

            ValidateForRestore();

            Application.Current!.CloseWindow(_window);
            _window = null;
        }
    }

    private static void ValidateForRestore()
    {
        IWindowService windowService = AxCServiceProvider.GetService<IWindowService>();

        if (New<KnownIdentities>().IsLoggedOn)
        {
            windowService.RestoreWindowWithFocus();
        }
    }
}