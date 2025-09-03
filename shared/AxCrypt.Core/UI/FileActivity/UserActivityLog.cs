namespace AxCrypt.Core.UI.FileActivity;

public enum UserActivityLog
{
    None = 0,

    SignIn,

    SignOut,

    Open,

    Encrypt,

    Decrypt,

    ShareKey,

    SecureDelete,

    DecryptBrokenFile,

    AnonymousRename,

    RestoreRenameToOriginal,

}
