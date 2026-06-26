using AxCrypt.Abstractions;
using AxCrypt.App.Shared.CloudCore;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.IO;
using AxCrypt.Desktop;
using System.Text.RegularExpressions;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.Platforms.Windows.Implementation;

public class PlatformInitializer
{
    public static void RegisterTypeFactories()
    {
        TypeMap.Register.Singleton<IInternetState>(() => new InternetState());
        TypeMap.Register.Singleton<InstallationVerifier>(() => new InstallationVerifier());

        string myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string? rootApplicationDirectory = Path.GetDirectoryName(myDocuments);
        TypeMap.Register.Singleton<ImportedFileStorage>(() => new ImportedFileStorage(rootApplicationDirectory!));
    }

    public static void CheckLavasoftWebCompanionExistence()
    {
        if (New<InstallationVerifier>().IsLavasoftApplicationInstalled)
        {
            Texts.LavasoftWebCompanionExistenceWarning.ShowWarning(Texts.WarningTitle, DoNotShowAgainOptions.LavasoftWebCompanionExistenceWarning);
        }
    }

    public static void SetupPathFilters()
    {
        if (OS.Current.Platform != Core.Runtime.Platform.WindowsDesktop)
        {
            return;
        }

        New<FileFilter>().AddUnencryptable(new Regex(@"\\\.dropbox$"));
        New<FileFilter>().AddUnencryptable(new Regex(@"\\desktop\.ini$"));
        New<FileFilter>().AddUnencryptable(new Regex(@".*\.tmp$"));
        New<FileFilter>().AddUnencryptable(new Regex(@"^.*\\~\$[^\\]*$"));
        New<FileFilter>().AddUnencryptable(new Regex(@"\\\$Recycle.Bin\*$"));
        New<FileFilter>().AddUnencryptable(new Regex(@"\\System Volume Information\*$"));

        AddEnvironmentVariableBasedFilePathFilter(@"^{0}(?!Temp$)", "SystemRoot");
        AddEnvironmentVariableBasedFilePathFilter(@"^{0}(?!Temp$)", "windir");
        AddEnvironmentVariableBasedFilePathFilter(@"^{0}", "ProgramData");
        AddEnvironmentVariableBasedFilePathFilter(@"^{0}", "ProgramFiles");
        AddEnvironmentVariableBasedFilePathFilter(@"^{0}", "ProgramFiles(x86)");
        AddEnvironmentVariableBasedFilePathFilter(@"^{0}$", "SystemDrive");

        // Windows 11 specific system app locations (hardcoded since no env vars)
        AddFolderPathFilter(@"C:\Windows\SystemApps");
        AddFolderPathFilter(@"C:\Program Files\WindowsApps");
        AddFolderPathFilter(@"C:\WindowsApps"); // sometimes appears outside Program Files
        AddFolderPathFilter(@"C:\Recovery");
        AddFolderPathFilter(@"C:\$WinREAgent");

        // Default user profile template
        AddFolderPathFilter(@"C:\Users\Default");
        AddFolderPathFilter(@"C:\Users\All Users"); // legacy junction

        New<FileFilter>().AddPlatformIndependent();

        AddDefaultWindows11FolderFilters();
    }

    public static void AddDefaultWindows11FolderFilters()
    {
        // Core system and program folders
        AddEnvironmentVariableBasedFolderPathFilter("ProgramData");
        AddEnvironmentVariableBasedFolderPathFilter("ProgramFiles");
        AddEnvironmentVariableBasedFolderPathFilter("ProgramFiles(x86)");
        AddEnvironmentVariableBasedFolderPathFilter("ProgramW6432");
        AddEnvironmentVariableBasedFolderPathFilter("SystemRoot");
        AddEnvironmentVariableBasedFolderPathFilter("windir");

        // User profile and app data
        AddEnvironmentVariableBasedFolderPathFilter("APPDATA");
        AddEnvironmentVariableBasedFolderPathFilter("LOCALAPPDATA");
        AddEnvironmentVariableBasedFolderPathFilter("USERPROFILE");
        AddEnvironmentVariableBasedFolderPathFilter("PUBLIC");

        // Common program files
        AddEnvironmentVariableBasedFolderPathFilter("CommonProgramFiles");
        AddEnvironmentVariableBasedFolderPathFilter("CommonProgramFiles(x86)");
        AddEnvironmentVariableBasedFolderPathFilter("CommonProgramW6432");

        // Temporary folders
        AddEnvironmentVariableBasedFolderPathFilter("TEMP");
        AddEnvironmentVariableBasedFolderPathFilter("TMP");
    }

    private static void AddEnvironmentVariableBasedFilePathFilter(string formatRegularExpression, string name)
    {
        IDataContainer folder = name.FolderFromEnvironment();
        if (folder == null)
        {
            return;
        }
        string escapedPath = folder.FullName.Replace(@"\", @"\\");
        New<FileFilter>().AddUnencryptable(new Regex(formatRegularExpression.InvariantFormat(escapedPath)));
    }

    private static void AddEnvironmentVariableBasedFolderPathFilter(string name)
    {
        IDataContainer folder = name.FolderFromEnvironment();
        if (folder == null)
        {
            return;
        }
        New<FileFilter>().AddForbiddenFolderFilters(folder.FullName);
    }

    private static void AddFolderPathFilter(string name)
    {
        IDataContainer folder = New<IDataContainer>(name);
        if (folder == null)
        {
            return;
        }
        New<FileFilter>().AddForbiddenFolderFilters(folder.FullName);
    }
}