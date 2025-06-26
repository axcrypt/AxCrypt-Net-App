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

            New<IDebugLoggingWindow>().CloseLogWindow();
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
                New<IDebugLoggingWindow>().CloseLogWindow();

                new ApplicationManager().WaitForBackgroundToComplete();
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
            await new ApplicationManager().ShutdownBackgroundSafe();
            await EncryptPendingFiles();

            finalAction();
        }

        private static async Task EncryptPendingFiles()
        {
            LogOnViewModel? logOnViewModel = AxCServiceProviderExtension.LogOnViewModel;
            if (logOnViewModel?.MainViewModel != null)
            {
                new ApplicationManager().WaitForBackgroundToComplete();
                await logOnViewModel.MainViewModel.EncryptPendingFiles.ExecuteAsync(null);
                new ApplicationManager().WaitForBackgroundToComplete();
            }
        }
    }
}
