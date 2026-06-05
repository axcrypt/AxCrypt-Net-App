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
    private DateTime _suppressMainWindowReloadUntilUtc = DateTime.MinValue;

    private UserFilePasswordViewModel? _userFilePasswordViewModel;

    public bool ShouldSuppressMainWindowReload =>
        _window != null || DateTime.UtcNow < _suppressMainWindowReloadUntilUtc;

    public async Task<DialogResult> ShowWindow(string? encryptedFileFullName)
    {
        // Idempotency guard — when the caller path fires the file-password
        // prompt twice in quick succession (e.g. double-click on Recent
        // Files + a concurrent decrypt-on-resume), we previously called
        // Application.Current.OpenWindow on a brand-new window while the
        // old one was still alive. MAUI then threw the "already created"
        // exception. Close the prior window first; cancel its pending
        // TaskCompletionSource so callers waiting on the earlier result
        // don't deadlock.
        if (_window != null)
        {
            try
            {
                _userFilePasswordViewModel?.FilePasswordTcs?.TrySetResult(DialogResult.Cancel);
                Close();
            }
            catch
            {
                // Best-effort cleanup — if the previous window is in a
                // broken state we still want the new request to land.
                _window = null;
            }
        }

        _userFilePasswordViewModel = AxCServiceProviderExtension.GetService<UserFilePasswordViewModel>();

        _userFilePasswordViewModel.ViewModel = new FilePasswordViewModel(encryptedFileFullName!);

        // Always start with the password obscured. The VM is freshly
        // instantiated here, but the property's default isn't
        // guaranteed across versions of FilePasswordViewModel — set
        // it explicitly so an "eye toggled" state from a previous
        // open never carries over.
        _userFilePasswordViewModel.ViewModel.ShowPassword = false;
        _userFilePasswordViewModel.ErrorMessage = string.Empty;

        BindPropertyChangedEvents();

        _userFilePasswordViewModel.FilePasswordTcs = new TaskCompletionSource<DialogResult>();

        await Show();

        // ⚠ Must await — never `.Task.Result`. ShowWindow gets called from
        // UI-dispatcher contexts (file open, recent-files double-click).
        // Blocking on the TCS there used to deadlock the dispatcher: the
        // BlazorWebView inside the new window can't complete its render
        // (and therefore can't fire the click that resolves the TCS) until
        // the UI thread unwinds — which it can't, because it's parked on
        // .Task.Result. End result: "app becomes unresponsive". Plain await
        // releases the thread back to the dispatcher and the modal renders.
        return await _userFilePasswordViewModel.FilePasswordTcs.Task.ConfigureAwait(false);
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
        // Window sized to fit the modal content — earlier the OS-level
        // window was 650×400 while the rendered modal is only ~440px
        // wide, leaving a wide empty scrim around it. Pick the modal
        // width (440) + scrim padding on each side (16 + 16) for the
        // window width, and the actual modal height (~360) + 24 for
        // the OS chrome / scrim top-bottom. Min/Max prevent the user
        // from resizing this dialog to something unreadable.
        const double targetWidth  = 480;
        const double targetHeight = 420;

        Window window = new Window
        {
            Title = _title,
            Width = targetWidth,
            Height = targetHeight,
            MinimumWidth = targetWidth,
            MinimumHeight = targetHeight,
            MaximumWidth = targetWidth,
            MaximumHeight = targetHeight,
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
        _suppressMainWindowReloadUntilUtc = DateTime.UtcNow.AddSeconds(3);

        if (_window == null)
        {
            return;
        }

        if (_userFilePasswordViewModel != null)
        {
            _userFilePasswordViewModel.IsWindowActive = false;
        }

        ValidateForRestore();

        try
        {
            Application.Current!.CloseWindow(_window);
        }
        catch
        {
            // Window may already be destroying when Close() fires from
            // the OnDestroy handler — swallow so the guard in
            // ShowWindow can still clear the slot.
        }
        _window = null;
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
