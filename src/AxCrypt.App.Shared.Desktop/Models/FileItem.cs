namespace AxCrypt.App.Shared.Desktop.Models;

/// <summary>Row in the Recent Files table.</summary>
public class FileItem
{
    public string Name { get; set; } = string.Empty;
    /// <summary>Sub-text shown under the file name (e.g. "Encrypted").</summary>
    public string SubText { get; set; } = string.Empty;
    /// <summary>Status string: Encrypted | Opened | Key Shared</summary>
    public string Status { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    /// <summary>CSS background color for the file icon chip.</summary>
    public string IconColor { get; set; } = "#eff6ff";
    /// <summary>SVG &lt;text&gt; fragment that shows the extension label inside the chip.</summary>
    public string ExtLabel { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}
