namespace AxCrypt.App.Shared.Utility;

public static class UIHelperUtility
{
    public static string GetIcon(this string fileName)
    {
        string fileExtension = GetExtention(fileName);
        fileExtension = fileExtension?.ToLowerInvariant() ?? "";
        
        if (DocFileTypes.Contains(fileExtension))
        {
            return "doc-type-ico";
        }

        if (ImgFileTypes!.Contains(fileExtension))
        {
            return "img-type-ico";
        }

        if (VideoFileTypes!.Contains(fileExtension))
        {
            return "vid-type-ico";
        }

        if (AudioFileTypes!.Contains(fileExtension))
        {
            return "aud-type-ico";
        }

        if (CompressedFileTypes!.Contains(fileExtension))
        {
            return "comprz-type-ico";
        }

        return "default-type-ico";
    }

    public static string GetExtention(string fileName)
    {
        string fileExtension = Path.GetExtension(fileName);

        return fileExtension;
    }

    private static readonly string[] VideoFileTypes =
    {
        ".mp4",
        ".avi",
        ".mov",
        ".mkv",
        ".wmv",
        ".flv",
        ".3gp",
    };

    private static readonly string[] DocFileTypes =
    {
        ".txt",
        ".xml",
        ".doc",
        ".docx",
        ".pdf",
        ".pptx",
        ".ppt",
        ".xls",
        ".xlsx",
        ".csv",
        ".js",
        ".cshtml",
        ".cshtm",
        ".scss",
        ".htm",
        ".razor",
    };
    private static readonly string[] ImgFileTypes =
    {
        ".png",
        ".svg",
        ".jpg",
        ".jpeg",
        ".gif",
        ".psd",
    };

    private static readonly string[] AudioFileTypes = { ".mp3", ".wav", ".aac", ".alac", ".flac", };
    private static readonly string[] CompressedFileTypes = { ".zip", ".7z", ".rar", };
}
