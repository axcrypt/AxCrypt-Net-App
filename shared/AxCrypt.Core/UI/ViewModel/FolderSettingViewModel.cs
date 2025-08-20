using AxCrypt.Common;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.Session;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.UI.ViewModel
{
    public class FolderSettingViewModel : ViewModelBase
    {
        private IEnumerable<string> _folderPaths;
        private FolderSettingViewModel(IEnumerable<string> folderPaths, IEnumerable<string> ignoredFolders, LogOnIdentity identity)
        {
            _folderPaths = folderPaths;
            AddIgnoreFolder = new DelegateAction<string>((folder) => AddIgnoreFolderAction(folder));
            SaveFolderSetings = new AsyncDelegateAction<object>((o) => SaveFolderSettingsAsync());
            RemoveIgnoreFolder = new AsyncDelegateAction<string>((folder) => RemoveIgnoreFolderAsync(folder));
            DecryptFilesInExcludedFolderTask = new AsyncDelegateAction<object>((o) => AsyncDecryptFilesInExcludedFolders(IgnoredFolders));
            InitializePropertyValues(ignoredFolders);
        }

        public IAction AddIgnoreFolder { get; private set; }
        public IAsyncAction RemoveIgnoreFolder { get; private set; }
        public IAsyncAction SaveFolderSetings { get; private set; }

        public IAsyncAction DecryptFilesInExcludedFolderTask { get; private set; }

        public IEnumerable<string> IgnoredFolders
        {
            get { return GetProperty<IEnumerable<string>>(nameof(IgnoredFolders)); }
            private set { SetProperty(nameof(IgnoredFolders), value.ToList()); }
        }

        private void InitializePropertyValues(IEnumerable<string> ignoredFolders)
        {
            IgnoredFolders = ignoredFolders;
        }

        public static FolderSettingViewModel CreateForSetting(IEnumerable<string> folders, LogOnIdentity identity)
        {
            if (folders == null) throw new ArgumentNullException(nameof(folders));
            if (identity == null) throw new ArgumentNullException(nameof(identity));

            IEnumerable<string> ignoredFolders = GetAllIgnoredFoldersFromWatchedFolders(folders);

            return new FolderSettingViewModel(folders, ignoredFolders, identity);
        }

        private static IEnumerable<string> GetAllIgnoredFoldersFromWatchedFolders(IEnumerable<string> folderPaths)
        {
            return folderPaths.ToWatchedFolders().IgnoredFolders();
        }

        private void AddIgnoreFolderAction(string folder)
        {
            IList<string> foldersList = IgnoredFolders.ToList();
            foldersList.Add(folder);

            IgnoredFolders = foldersList;
        }

        private async Task RemoveIgnoreFolderAsync(string folder)
        {
            List<string> foldersList = new List<string>();

            foldersList = new List<string>(IgnoredFolders);

            foldersList.Remove(folder);
            IgnoredFolders = foldersList;
        }

        private async Task SaveFolderSettingsAsync()
        {
            foreach (WatchedFolder watchedFolder in _folderPaths.ToWatchedFolders())
            {
                WatchedFolder wf = new WatchedFolder(watchedFolder, IgnoredFolders);
                await New<FileSystemState>().AddWatchedFolderAsync(wf).Free();
            }

            await New<FileSystemState>().Save();
        }

        private async Task AsyncDecryptFilesInExcludedFolders(IEnumerable<string> ignoredFolder)
        {
            foreach (string path in ignoredFolder)
            {
                await Resolve.SessionNotify.NotifyAsync(new SessionNotification(SessionNotificationType.WatchedFolderExcludedFolder, Resolve.KnownIdentities.DefaultEncryptionIdentity, path));
            }
        }
    }
}
