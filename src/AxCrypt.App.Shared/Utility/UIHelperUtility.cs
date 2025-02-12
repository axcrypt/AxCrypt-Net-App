namespace AxCrypt.App.Shared.Utility;

public static class UIHelperUtility
{
    public static string GetIcon(this string fileName)
    {
        string fileExtension = GetExtention(fileName);

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

        return "default-type-ico";
    }

    public static string GetExtention(string fileName)
    {
        string fileExtension = Path.GetExtension(fileName);

        return fileExtension;
    }

    private static readonly string[] VideoFileTypes = {
        ".mp4",
        ".MP4",
        ".avi",
        ".AVI",
        ".mov",
        ".MOV",
        ".mkv",
        ".MKV",
        ".wmv",
        ".WMV",
        ".flv",
        ".FLV",
        ".3gp",
        ".3GP"
    };

    private static readonly string[] DocFileTypes = {
        ".txt",
        ".TXT",
        ".xml",
        ".XML",
        ".doc",
        ".DOC",
        ".docx",
        ".DOCX",
        ".pdf",
        ".PDF",
        ".xls",
        ".XLS",
        ".xlsx",
        ".XLSX",
        ".csv",
        ".CSV",
        ".js",
        ".JS",
        ".cshtml",
        ".CSHTML",
        ".cshtm",
        ".CSHTM",
        ".scss",
        ".htm",
        ".HTM",
        ".razor",
        ".RAZOR",
        ".zip",
        ".ZIP"
    };
    private static readonly string[] ImgFileTypes = {
        ".png",
        ".PNG",
        ".svg",
        ".SVG",
        ".jpg",
        ".JPG",
        ".jpeg",
        ".JPEG",
        ".gif",
        ".GIF",
        ".psd",
        ".PSD"
    };

    private static readonly string[] AudioFileTypes = {
        ".mp3",
        ".MP3",
        ".wav",
        ".WAV",
        ".aac",
        ".AAC",
        ".alac",
        ".ALAC",
        ".flac",
        ".FLAC"
    };

}
