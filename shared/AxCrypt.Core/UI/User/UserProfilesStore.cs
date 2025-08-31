using AxCrypt.Abstractions;
using AxCrypt.Core.IO;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.UI.User
{
    public class UserProfilesStore : IUserProfilesStore
    {
        private IDataStore? _persistanceFileInfo;

        private IList<UserProfile> _userProfiles;

        private string _AppRootFolder = "";

        public UserProfilesStore(IDataStore dataStore)
        {
            _persistanceFileInfo = dataStore;
            _AppRootFolder = dataStore.Container.FullName;
            _userProfiles = new List<UserProfile>();

            if (_persistanceFileInfo == null || !_persistanceFileInfo.IsAvailable)
            {
                return;
            }

            using (New<FileLocker>().Acquire(_persistanceFileInfo))
            {
                Initialize(_persistanceFileInfo.OpenRead());
            }
        }

        public string AppRootFolder
        {
            get
            {
                return _AppRootFolder;
            }
        }

        public IEnumerable<UserProfile> Profiles
        {
            get
            {
                return _userProfiles;
            }
        }

        public UserProfile ActiveProfile
        {
            get
            {
                return _userProfiles.LastOrDefault(ap => ap.Active) ?? UserProfile.Empty;
            }
        }

        public bool AddUser(UserProfile userProfile)
        {
            if (userProfile == null)
            {
                return false;
            }

            _userProfiles.Add(userProfile);
            Save();
            return true;
        }

        public bool UpdateUser(UserProfile userProfile)
        {
            if (userProfile == null)
            {
                return false;
            }

            UserProfile? currentUserProfile = _userProfiles.FirstOrDefault(up => up.UserEmail == userProfile.UserEmail);
            if (currentUserProfile == null)
            {
                return false;
            }

            currentUserProfile.UserEmail = userProfile.UserEmail;
            currentUserProfile.Active = userProfile.Active;
            currentUserProfile.SubsType = userProfile.SubsType;
            currentUserProfile.LastLogOnUtc = userProfile.LastLogOnUtc;
            currentUserProfile.LastUpdateUtc = userProfile.LastLogOnUtc;
            Save();

            return true;
        }

        public bool RemoveUser(UserProfile userProfile)
        {
            if (userProfile == null)
            {
                return false;
            }

            _userProfiles.Remove(userProfile);
            Save();
            return true;
        }

        protected void Save()
        {
            if (_persistanceFileInfo == null)
            {
                return;
            }

            using (New<FileLocker>().Acquire(_persistanceFileInfo))
            {
                Save(_persistanceFileInfo.OpenWrite());
            }
        }

        protected void Initialize(Stream readStream)
        {
            _userProfiles = New<IStringSerializer>().Deserialize<List<UserProfile>>(readStream);
        }

        protected void Save(Stream saveStream)
        {
            New<IStringSerializer>().Serialize(_userProfiles, saveStream);
        }
    }
}