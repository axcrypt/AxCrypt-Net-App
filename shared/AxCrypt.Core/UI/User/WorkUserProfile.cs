using AxCrypt.Abstractions;
using AxCrypt.Core.IO;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.UI.User
{
    public static class WorkUserProfile
    {
        public static string UserEmail
        {
            get { return New<IUserProfilesStore>().ActiveProfile.UserEmail; }
        }

        public static string WorkFolderPath
        {
            get
            {
                return New<IUserProfilesStore>().ActiveProfile.BasePath;
            }
        }

        public static bool IsFirstSignIn
        {
            get { return !New<IUserProfilesStore>().Profiles.Any(); }
        }

        public static string? GetUserWorkFolderOnAppStart(string workPath)
        {
            if (!IsFirstSignIn)
            {
                return WorkFolderPath;
            }

            if (Directory.Exists(workPath))
            {
                string? childDir = Directory.GetDirectories(workPath).FirstOrDefault();
                if (childDir != null)
                {
                    InternalAddUser("", childDir);
                    return childDir;
                }
            }

            return AddTempUserProfile(workPath);
        }

        public static IDataContainer CreateTemporaryFolder(string basePath)
        {
            string destinationFolder = Resolve.Portable.Path().Combine(basePath, Resolve.Portable.Path().GetFileNameWithoutExtension(Resolve.Portable.Path().GetRandomFileName()) + Resolve.Portable.Path().DirectorySeparatorChar);
            IDataContainer destinationFolderInfo = New<IDataContainer>(destinationFolder);
            destinationFolderInfo.CreateFolder();

            return destinationFolderInfo;
        }

        public static string AddTempUserProfile(string workPath, string userEmail = "")
        {
            string folderPath = CreateTemporaryFolder(workPath).FullName;
            return InternalAddUser(userEmail, folderPath);
        }

        private static string InternalAddUser(string userEmail, string folderPath)
        {
            UserProfile userProfile = new UserProfile()
            {
                Active = true,
                BasePath = folderPath,
                LastUpdateUtc = New<INow>().Utc,
            };

            if (userEmail != "")
            {
                userProfile.UserEmail = userEmail;
            }

            bool created = New<IUserProfilesStore>().AddUser(userProfile);
            if (created)
            {
                return folderPath;
            }

            return null;
        }

        public static void SetUser(string basePath, string userEmail)
        {
            if (New<IUserProfilesStore>().Profiles.Any(up=> up.UserEmail == userEmail))
            {
                return;
            }

            UserProfile? tempProfile = New<IUserProfilesStore>().Profiles.FirstOrDefault(up => up.BasePath != "" && up.UserEmail == "");
            if (tempProfile != null)
            {
                tempProfile.UserEmail = userEmail;
                tempProfile.LastUpdateUtc = New<INow>().Utc;
                New<IUserProfilesStore>().UpdateUser(tempProfile);
                return;
            }

            AddTempUserProfile(basePath, userEmail);
        }
    }
}