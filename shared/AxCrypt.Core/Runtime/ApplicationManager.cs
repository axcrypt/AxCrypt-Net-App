using AxCrypt.Abstractions;
using AxCrypt.Content;
using AxCrypt.Core.Service;
using AxCrypt.Core.Service.Secrets;
using AxCrypt.Core.Service.SecuredMessenger;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.FindFilesActivity;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.Runtime
{
    public class ApplicationManager
    {
        public async Task<bool> ValidateSettings()
        {
            if (Resolve.UserSettings.SettingsVersion >= New<UserSettingsVersion>().Current)
            {
                return true;
            }

            await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.UserSettingsFormatChangeNeedsReset);
            await ClearAllSettings();
            await StopAndExit();
            return false;
        }

        public async Task ClearAllSettings()
        {
            await ShutdownBackgroundSafe();

            Resolve.UserSettings.Clear();
            Resolve.FileSystemState.Delete();
            Resolve.WorkFolder.FileInfo.FileItemInfo(LocalAccountService.FileName).Delete();
            Resolve.WorkFolder.FileInfo.FileItemInfo(LocalSecretsService.SecretsFileName).Delete();
            Resolve.WorkFolder.FileInfo.FileItemInfo(LocalSecretsService.SharedSecretsFileName).Delete();
            Resolve.WorkFolder.FileInfo.FileItemInfo(LocalSecuredMessengerService.InboxMessageFileName).Delete();
            Resolve.WorkFolder.FileInfo.FileItemInfo(LocalSecuredMessengerService.SentMessageFileName).Delete();
            Resolve.WorkFolder.FileInfo.FileItemInfo(UI.FileActivity.FileActivityStore.FileActivityLogFileName).Delete();
            Resolve.WorkFolder.FileInfo.FileItemInfo(FindFilesStore.FINDFILESLOGFILENAME).Delete();
            New<KnownPublicKeys>().Delete();
            Resolve.UserProfileStore.RemoveUserProfile();
        }

        public async Task StopAndExit()
        {
            await ShutdownBackgroundSafe();

            New<IUIThread>().ExitApplication();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public async Task WaitForBackgroundToCompleteAsync()
        {
            await New<IProgressBackground>().WaitForIdleAsync();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public async Task ShutdownBackgroundSafe()
        {
            try
            {
                New<WorkFolderWatcher>().Dispose();
            }
            catch
            {
            }
            try
            {
                New<ActiveFileWatcher>().Dispose();
            }
            catch
            {
            }

            try
            {
                await New<IProgressBackground>().WaitForIdleAsync();
            }
            catch
            {
            }
        }
    }
}