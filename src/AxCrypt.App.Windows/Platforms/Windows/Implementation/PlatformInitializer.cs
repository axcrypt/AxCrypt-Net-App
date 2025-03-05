using AxCrypt.Abstractions;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
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

        AddEnvironmentVariableBasedFilePathFilter(@"^{0}(?!Temp$)", "SystemRoot");
        AddEnvironmentVariableBasedFilePathFilter(@"^{0}(?!Temp$)", "windir");
        AddEnvironmentVariableBasedFilePathFilter(@"^{0}", "ProgramFiles");
        AddEnvironmentVariableBasedFilePathFilter(@"^{0}", "ProgramFiles(x86)");
        AddEnvironmentVariableBasedFilePathFilter(@"^{0}$", "SystemDrive");

        New<FileFilter>().AddPlatformIndependent();

        AddEnvironmentVariableBasedFolderPathFilter("ProgramData");
        AddEnvironmentVariableBasedFolderPathFilter("ProgramFiles(x86)");
        AddEnvironmentVariableBasedFolderPathFilter("ProgramFiles");
        AddEnvironmentVariableBasedFolderPathFilter("SystemRoot");
        AddEnvironmentVariableBasedFolderPathFilter("APPDATA");
        AddEnvironmentVariableBasedFolderPathFilter("LOCALAPPDATA");
        AddEnvironmentVariableBasedFolderPathFilter("windir");
        AddEnvironmentVariableBasedFolderPathFilter("ProgramW6432");
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
}