using AxCrypt.Core;
using System.Drawing;

namespace AxCrypt.App.Desktop.Code
{
    internal static class AppPreferences
    {
        public const int MinimumWindowWidth = 1156;
        public const int MinimumWindowHeight = 770;

        public static double MainWindowWidth
        { get { return Resolve.UserSettings.Load<double>(nameof(MainWindowWidth)); } set { Resolve.UserSettings.Store(nameof(MainWindowWidth), value); } }

        public static double MainWindowHeight
        { get { return Resolve.UserSettings.Load<double>(nameof(MainWindowHeight)); } set { Resolve.UserSettings.Store(nameof(MainWindowHeight), value); } }

        public static Point MainWindowLocation
        { get { return new Point(Resolve.UserSettings.Load<int>("MainWindowLocationX"), Resolve.UserSettings.Load<int>("MainWindowLocationY")); } set { Resolve.UserSettings.Store("MainWindowLocationX", value.X); Resolve.UserSettings.Store("MainWindowLocationY", value.Y); } }

        public static int RecentFilesMaxNumber
        { get { return Resolve.UserSettings.Load<int>(nameof(RecentFilesMaxNumber), 250); } set { Resolve.UserSettings.Store(nameof(RecentFilesMaxNumber), value); } }

        public static int RecentFilesSizeWidth
        { get { return Resolve.UserSettings.Load<int>(nameof(RecentFilesSizeWidth)); } set { Resolve.UserSettings.Store(nameof(RecentFilesSizeWidth), value); } }

        public static int RecentFilesDocumentWidth
        { get { return Resolve.UserSettings.Load<int>(nameof(RecentFilesDocumentWidth)); } set { Resolve.UserSettings.Store(nameof(RecentFilesDocumentWidth), value); } }

        public static int RecentFilesAccessedDateWidth
        { get { return Resolve.UserSettings.Load<int>(nameof(RecentFilesAccessedDateWidth)); } set { Resolve.UserSettings.Store(nameof(RecentFilesAccessedDateWidth), value); } }

        public static int RecentFilesEncryptedPathWidth
        { get { return Resolve.UserSettings.Load<int>(nameof(RecentFilesEncryptedPathWidth)); } set { Resolve.UserSettings.Store(nameof(RecentFilesEncryptedPathWidth), value); } }

        public static int RecentFilesCryptoNameWidth
        { get { return Resolve.UserSettings.Load<int>(nameof(RecentFilesCryptoNameWidth)); } set { Resolve.UserSettings.Store(nameof(RecentFilesCryptoNameWidth), value); } }

        public static int RecentFilesModifiedDateWidth
        { get { return Resolve.UserSettings.Load<int>(nameof(RecentFilesModifiedDateWidth)); } set { Resolve.UserSettings.Store(nameof(RecentFilesModifiedDateWidth), value); } }

        public static bool RecentFilesAscending
        { get { return Resolve.UserSettings.Load<bool>(nameof(RecentFilesAscending), true); } set { Resolve.UserSettings.Store(nameof(RecentFilesAscending), value); } }

        public static int RecentFilesSortColumn
        { get { return Resolve.UserSettings.Load<int>(nameof(RecentFilesSortColumn), 0); } set { Resolve.UserSettings.Store(nameof(RecentFilesSortColumn), value); } }
    }
}