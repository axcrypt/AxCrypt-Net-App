using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.UI.FileActivity;

public class UserActivityLogger
{
    private readonly string _Actor = "";

    public UserActivityLogger(string actor)
    {
        _Actor = actor;
    }

    public void AppendActivity(string source, UserActivityLog fileActivityLogItem)
    {
        string formattedLog = FileActivityLogText(source, fileActivityLogItem);
        New<FileActivityStore>().Save(formattedLog);
    }

    private string FileActivityLogText(string source, UserActivityLog fileActivityLogItem)
    {
        DateTime dateTime = DateTime.Now;
        string logText = string.Empty;

        switch (fileActivityLogItem)
        {
            case UserActivityLog.SignIn:
                logText = $"{dateTime} - {_Actor} signed in";
                break;
            case UserActivityLog.SignOut:
                logText = $"{dateTime} - {_Actor} signed out";
                break;
            case UserActivityLog.Open:
                logText = $"{dateTime} - {_Actor} opened the file {source} ";
                break;
            case UserActivityLog.Encrypt:
                logText = $"{dateTime} - {_Actor} encrypted the file {source} ";
                break;
            case UserActivityLog.Decrypt:
                logText = $"{dateTime} - {_Actor} decrypted the file {source}";
                break;
            case UserActivityLog.ShareKey:
                logText = $"{dateTime} - {_Actor} key shared the file {source}";
                break;
            case UserActivityLog.SecureDelete:
                logText = $"{dateTime} - {_Actor} securely deleted the file {source}";
                break;
            case UserActivityLog.DecryptBrokenFile:
                logText = $"{dateTime} - {_Actor} decrypted the broken file {source} ";
                break;

            default:
            case UserActivityLog.None:
                logText = string.Empty;
                break;
        }

        return logText;
    }

    public static void ListActivities()
    {
        New<FileActivityStore>().GetFileActivityLogs();
    }
}
