using AxCrypt.Abstractions;
using AxCrypt.Core.UI.FileActivity;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.UI.FindFilesActivity;

public class FindFilesLogger
{
    public static void Log(string filePath, UserActivityLog securedFilesLogItem)
    {
        switch (securedFilesLogItem)
        {
            case UserActivityLog.Encrypt:
            case UserActivityLog.AnonymousRename:
            case UserActivityLog.RestoreRenameToOriginal:
                InternalLog(filePath);
                break;

            case UserActivityLog.Decrypt:
                New<FindFilesStore>().PurgeIfExists(filePath);
                break;

            default:
                break;
        }
    }

    private static void InternalLog(string filePath)
    {
        FindFilesLog fileEntry = new FindFilesLog
        {
            DateTime = New<INow>().Utc,
            FilePath = filePath
        };

        New<FindFilesStore>().Save(fileEntry);
    }
}