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

            return AddTempUserProfile(workPath);
        }

        public static IDataContainer CreateTemporaryFolder(string basePath)
        {
            string destinationFolder = Resolve.Portable.Path().Combine(basePath, Resolve.Portable.Path().GetFileNameWithoutExtension(Resolve.Portable.Path().GetRandomFileName()) + Resolve.Portable.Path().DirectorySeparatorChar);
            IDataContainer destinationFolderInfo = New<IDataContainer>(destinationFolder);
            destinationFolderInfo.CreateFolder();

            return destinationFolderInfo;
        }

        public static string AddTempUserProfile(string workPath)
        {
            string folderPath = CreateTemporaryFolder(workPath).FullName;
            UserProfile userProfile = new UserProfile()
            {
                Active = true,
                BasePath = folderPath,
                LastUpdateUtc = New<INow>().Utc,
            };

            bool created = New<IUserProfilesStore>().AddUser(userProfile);
            if (created)
            {
                return folderPath;
            }

            return null;
        }
    }
}