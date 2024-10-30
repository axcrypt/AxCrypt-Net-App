using AxCrypt.Content;
using AxCrypt.Core.IO;
using AxCrypt.Core.UI;
using AxCrypt.Core;
using Microsoft.Win32;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.Desktop;

public class KnownFoldersDiscovery : IKnownFoldersDiscovery
{
    private static readonly string MyAxCryptFolderName = "My AxCrypt";

    public IEnumerable<KnownFolder> Discover()
    {
        List<KnownFolder> knownFolders = new List<KnownFolder>();
        if (OS.Current.Platform != Core.Runtime.Platform.WindowsDesktop)
        {
            return knownFolders;
        }

        CheckDocumentsLibrary(knownFolders);
        CheckDropBox(knownFolders);
        CheckOneDrive(knownFolders);
        CheckGoogleDrive(knownFolders);

        return knownFolders;
    }

    private static void CheckDocumentsLibrary(IList<KnownFolder> knownFolders)
    {
        IDataContainer myDocumentsInfo = New<IDataContainer>(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        KnownFolder windowsDesktopFolder = new KnownFolder(myDocumentsInfo, MyAxCryptFolderName, KnownFolderKind.WindowsMyDocuments, null, Texts.KnownFolderNameWindowsMyDocuments);
        knownFolders.Add(windowsDesktopFolder);
    }

    private static void CheckDropBox(IList<KnownFolder> knownFolders)
    {
        string dropBoxFolder = Path.Combine(Environment.GetEnvironmentVariable("HOMEDRIVE") + Environment.GetEnvironmentVariable("HOMEPATH"), "DropBox");
        if (!Directory.Exists(dropBoxFolder))
        {
            return;
        }

        IDataContainer dropBoxFolderInfo = New<IDataContainer>(dropBoxFolder);
        KnownFolder knownFolder = new KnownFolder(dropBoxFolderInfo, MyAxCryptFolderName, KnownFolderKind.Dropbox, null, Texts.KnownFolderNameDropbox);

        knownFolders.Add(knownFolder);
    }

    private static void CheckOneDrive(IList<KnownFolder> knownFolders)
    {
        string oneDriveFolder = FindOneDriveFolder();
        if (!Directory.Exists(oneDriveFolder))
        {
            return;
        }

        Uri url = new Uri("https://onedrive.live.com/");
        IDataContainer oneDriveFolderInfo = New<IDataContainer>(oneDriveFolder);
        KnownFolder knownFolder = new KnownFolder(oneDriveFolderInfo, MyAxCryptFolderName, KnownFolderKind.OneDrive, url, Texts.KnownFolderNameOneDrive);

        knownFolders.Add(knownFolder);
    }

    private static string FindOneDriveFolder()
    {
        string oneDriveFolder = null;

        oneDriveFolder = TryRegistryLocationForOneDriveFolder(@"Software\Microsoft\OneDrive");
        if (oneDriveFolder != null)
        {
            return oneDriveFolder;
        }

        oneDriveFolder = TryRegistryLocationForOneDriveFolder(@"Software\Microsoft\Windows\CurrentVersion\SkyDrive");
        if (oneDriveFolder != null)
        {
            return oneDriveFolder;
        }

        oneDriveFolder = Path.Combine(Environment.GetEnvironmentVariable("HOMEDRIVE") + Environment.GetEnvironmentVariable("HOMEPATH"), "OneDrive");
        return oneDriveFolder;
    }

    private static string TryRegistryLocationForOneDriveFolder(string name)
    {
        RegistryKey oneDriveKey = Registry.CurrentUser.OpenSubKey(name);
        if (oneDriveKey == null)
        {
            return null;
        }

        string oneDriveFolder = oneDriveKey.GetValue("UserFolder") as string;
        if (String.IsNullOrEmpty(oneDriveFolder))
        {
            return null;
        }

        return oneDriveFolder;
    }

    private static void CheckGoogleDrive(IList<KnownFolder> knownFolders)
    {
        string googleDriveFolder = Path.Combine(Environment.GetEnvironmentVariable("HOMEDRIVE") + Environment.GetEnvironmentVariable("HOMEPATH"), "Google Drive");
        if (String.IsNullOrEmpty(googleDriveFolder) || !Directory.Exists(googleDriveFolder))
        {
            return;
        }

        Uri url = new Uri("https://drive.google.com/");

        IDataContainer googleDriveFolderInfo = New<IDataContainer>(googleDriveFolder);
        KnownFolder knownFolder = new KnownFolder(googleDriveFolderInfo, MyAxCryptFolderName, KnownFolderKind.GoogleDrive, url, Texts.KnownFolderNameGoogleDrive);
        knownFolders.Add(knownFolder);
    }
}
