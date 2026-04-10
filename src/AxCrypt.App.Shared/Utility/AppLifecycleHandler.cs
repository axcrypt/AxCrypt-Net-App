using AxCrypt.Abstractions;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Content;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Utility
{
    public static class AppLifecycleHandler
    {
        public static async Task SignOutSignIn()
        {
            LogOnViewModel? logOnViewModel = AxCServiceProviderExtension.LogOnViewModel;
            bool isPending = await WarnIfAnyPendingFiles(logOnViewModel!);
            if (!isPending)
            {
                return;
            }

            await logOnViewModel!.InvokeLogOnOrLogOffAndLogOnAgainAsync();
        }

        public static async Task ExitApplication()
        {
            LogOnViewModel? logOnViewModel = AxCServiceProviderExtension.LogOnViewModel;
            bool isPending = await WarnIfAnyPendingFiles(logOnViewModel!);
            if (!isPending)
            {
                return;
            }

            New<IDebugLoggingWindow>().CloseAllLogWindows();
            await ShutDownAnd(New<IUIThread>().ExitApplication);
        }

        public static async Task RestartApplication()
        {
            LogOnViewModel? logOnViewModel = AxCServiceProviderExtension.LogOnViewModel;
            bool isPending = await WarnIfAnyPendingFiles(logOnViewModel!);
            if (!isPending)
            {
                return;
            }

            PopupButtons result = await New<IPopup>().ShowAsync(PopupButtons.OkCancel, Texts.WarningTitle, Texts.ResetAllSettingsWarningText);
            if (result == PopupButtons.Ok)
            {
                New<IDebugLoggingWindow>().CloseAllLogWindows();

                await new ApplicationManager().WaitForBackgroundToCompleteAsync();
                await new ApplicationManager().ClearAllSettings();
                await ShutDownAnd(New<IUIThread>().RestartApplication);
            }
        }

        private static async Task<bool> WarnIfAnyPendingFiles(LogOnViewModel logOnViewModel)
        {
            if (logOnViewModel!.IsLoggedOn && logOnViewModel!.MainViewModel!.DecryptedFiles.Any())
            {
                await logOnViewModel.MainViewModel.WarnIfAnyDecryptedFiles.ExecuteAsync(null);
                return false;
            }

            return true;
        }

        private static async Task ShutDownAnd(Action finalAction)
        {
            // Background-shutdown + pending-file flush can occasionally hang
            // (e.g. a vault sync awaiting a disposed cancellation token, or
            // EncryptPendingFiles stuck on an aborted file). Without a
            // timeout, finalAction() — the call that actually terminates
            // the process — never runs, leaving the app window closed but
            // the underlying process alive (this is what the user reported
            // as "the app closes but the debug window stays open").
            //
            // Race the shutdown work against a hard deadline so the kill
            // always fires within a bounded window.
            const int shutdownTimeoutMs = 3500;

            Task shutdownTask = Task.Run(async () =>
            {
                try
                {
                    await new ApplicationManager().ShutdownBackgroundSafe();
                    await EncryptPendingFiles();
                }
                catch
                {
                    // Best-effort shutdown — if anything throws we still
                    // want the exit to proceed.
                }
            });

            await Task.WhenAny(shutdownTask, Task.Delay(shutdownTimeoutMs));

            // Always run the final action (Process.GetCurrentProcess().Kill()
            // on Windows; equivalent on other platforms). Wrapped so a
            // platform that ungracefully throws still doesn't strand us.
            try
            {
                finalAction();
            }
            catch
            {
                // Belt-and-suspenders: if the platform exit threw, kill
                // the process directly so the user never sees a zombie
                // window.
                System.Environment.Exit(0);
            }
        }

        private static async Task EncryptPendingFiles()
        {
            LogOnViewModel? logOnViewModel = AxCServiceProviderExtension.LogOnViewModel;
            if (logOnViewModel?.MainViewModel != null)
            {
                await new ApplicationManager().WaitForBackgroundToCompleteAsync();
                await logOnViewModel.MainViewModel.EncryptPendingFiles.ExecuteAsync(null);
                await new ApplicationManager().WaitForBackgroundToCompleteAsync();
            }
        }
    }
}
