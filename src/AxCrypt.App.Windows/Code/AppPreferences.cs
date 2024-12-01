using AxCrypt.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics;

namespace AxCrypt.App.Windows.Code
{
    internal static class AppPreferences
    {
        public static double MainWindowWidth
        { get { return Resolve.UserSettings.Load<double>(nameof(MainWindowWidth)); } set { Resolve.UserSettings.Store(nameof(MainWindowWidth), value); } }

        public static double MainWindowHeight
        { get { return Resolve.UserSettings.Load<double>(nameof(MainWindowHeight)); } set { Resolve.UserSettings.Store(nameof(MainWindowHeight), value); } }

        public static PointInt32 MainWindowLocation
        { get { return new PointInt32(Resolve.UserSettings.Load<int>(nameof(MainWindowLocation)), Resolve.UserSettings.Load<int>(nameof(MainWindowLocation))); } set { Resolve.UserSettings.Store("MainWindowLocationX", value.X); Resolve.UserSettings.Store("MainWindowLocationY", value.Y); } }

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
