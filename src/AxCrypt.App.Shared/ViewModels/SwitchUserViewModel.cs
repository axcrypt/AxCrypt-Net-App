using AxCrypt.Abstractions;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI.User;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.ViewModels;

public class SwitchUserViewModel : ViewModelBase
{
    public int MaxUserProfileLimit { get; } = 5;

    public LogOnViewModel LogOnViewModel { get; set; }

    public IEnumerable<UserProfile> UserProfilesList = new List<UserProfile>();

    public string SelectedProfileUserEmail { get; set; } = "";

    public SwitchUserViewModel()
    {
        LogOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
    }

    public void InitializeUserProfiles()
    {
        UserProfilesList = New<IUserProfilesStore>().Profiles;
    }

    public void SwitchUser(string userEmail)
    {
        UserProfile? currectActiveUser = UserProfilesList.FirstOrDefault(up => up.Active);
        if (currectActiveUser != null)
        {
            currectActiveUser.Active = false;
            Update(currectActiveUser);
        }

        UserProfile? selectedUser = UserProfilesList.FirstOrDefault(up => up.UserEmail == userEmail);
        if (selectedUser != null)
        {
            selectedUser.Active = true;
            Update(selectedUser);
        }

        SelectedProfileUserEmail = userEmail;
    }

    public bool AddNewUser(string userEmail, string subsType)
    {
        if (UserProfilesList.Any(up => up.UserEmail == userEmail))
        {
            return false;
        }

        if (UserProfilesList.Count() > MaxUserProfileLimit)
        {
            return false;
        }

        DateTime currentDateTime = New<INow>().Utc;
        UserProfile? tempUserProfile = UserProfilesList.FirstOrDefault(wup => wup.Active && wup.UserEmail == "" && wup.BasePath != "");
        if (tempUserProfile != null)
        {
            return UpdateExistingProfile(userEmail, subsType, currentDateTime, tempUserProfile);
        }

        return AddNewUserProfile(userEmail, subsType, currentDateTime);
    }

    private static bool UpdateExistingProfile(string userEmail, string subsType, DateTime currentDateTime, UserProfile tempUserProfile)
    {
        tempUserProfile.UserEmail = userEmail;
        tempUserProfile.SubsType = subsType;
        tempUserProfile.LastUpdateUtc = currentDateTime;
        tempUserProfile.LastLogOnUtc = currentDateTime;

        return New<IUserProfilesStore>().UpdateUser(tempUserProfile);
    }

    private bool AddNewUserProfile(string userEmail, string subsType, DateTime currentDateTime)
    {
        string folderPath = WorkUserProfile.CreateTemporaryFolder(New<IUserProfilesStore>().AppRootFolder).FullName;
        UserProfile userProfile = new UserProfile()
        {
            Active = true,
            BasePath = folderPath,
            SubsType = subsType,
            UserEmail = userEmail,
            LastUpdateUtc = currentDateTime,
            LastLogOnUtc = currentDateTime,
        };

        bool created = AddNewUser(userProfile);
        if (created)
        {
            SwitchUser(userEmail);
        }

        return created;
    }

    private bool AddNewUser(UserProfile user)
    {
        if (user == null)
        {
            return false;
        }

        return New<IUserProfilesStore>().AddUser(user);
    }

    public bool Update(UserProfile userProfile)
    {
        if (userProfile == null)
        {
            return false;
        }

        return New<IUserProfilesStore>().UpdateUser(userProfile);
    }

    public async Task<bool> RemoveUserProfileAsync(string userEmail)
    {
        if (string.IsNullOrEmpty(userEmail) || !UserProfilesList.Any(up => up.UserEmail == userEmail))
        {
            return false;
        }

        UserProfile? userProfile = UserProfilesList.FirstOrDefault(up => up.UserEmail == userEmail);
        if (userProfile == null)
        {
            return false;
        }

        if (userProfile.Active)
        {
            return false;
        }

        bool deleted = New<IUserProfilesStore>().RemoveUser(userProfile);
        if (deleted)
        {
            New<IDebugLoggingWindow>().CloseAllLogWindows();

            await new ApplicationManager().WaitForBackgroundToCompleteAsync();
            SafeDeleteFilesInFolder(userProfile.BasePath);
        }

        return deleted;
    }

    private void SetLoginUserProfile(string userEmail, bool readonlyEmail)
    {
        LogOnViewModel.LogOnAccountModel.UserEmail = userEmail;
        LogOnViewModel.LogOnAccountModel.ReadOnlyUserEmail = readonlyEmail;
    }

    public void OnCancel_Clicked()
    {
        ClosePopup();
    }

    public async Task OnNewEmail_Clicked()
    {
        WorkUserProfile.AddTempUserProfile(New<IUserProfilesStore>().AppRootFolder);
        SwitchUser(string.Empty);

        SetLoginUserProfile(string.Empty, false);
        ClosePopup();

        await SwitchAllUserSettingsAsync();
    }

    public async Task OnConfirm_Clicked()
    {
        if (LogOnViewModel.LogOnAccountModel.UserEmail == SelectedProfileUserEmail)
        {
            LogOnViewModel.SwitchUserDialog.Close();
            return;
        }

        SetLoginUserProfile(SelectedProfileUserEmail, true);
        LogOnViewModel.SwitchUserDialog.Close();

        UserProfile? selectedUserProfile = UserProfilesList.FirstOrDefault(up => up.UserEmail == SelectedProfileUserEmail);
        if (selectedUserProfile == null)
        {
            return;
        }

        New<IUserProfilesStore>().UpdateUser(selectedUserProfile);

        await SwitchAllUserSettingsAsync();
    }

    private void ClosePopup()
    {
        LogOnViewModel.SwitchUserDialog.Close();
    }

    private async Task SwitchAllUserSettingsAsync()
    {
        UpdateViewState();

        New<IDebugLoggingWindow>().CloseAllLogWindows();

        await new ApplicationManager().WaitForBackgroundToCompleteAsync();
        await ShutDownAnd(New<IUIThread>().RestartApplication);
    }

    private static async Task ShutDownAnd(Action finalAction)
    {
        await new ApplicationManager().ShutdownBackgroundSafe();

        finalAction();
    }

    private bool IsFileNotInUse(string filePath)
    {
        try
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    private void SafeDeleteFilesInFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return;

        string[] files = Directory.GetFiles(folderPath);

        foreach (string file in files)
        {
            if (!IsFileNotInUse(file))
            {
                Console.WriteLine($"File in use, skipped: {file}");
                continue;
            }

            try
            {
                File.Delete(file);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting {file}: {ex.Message}");
            }
        }

        files = Directory.GetFiles(folderPath);
        if (!files.Any())
        {
            try
            {
                Directory.Delete(folderPath, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting {folderPath}: {ex.Message}");
            }
        }
    }
}