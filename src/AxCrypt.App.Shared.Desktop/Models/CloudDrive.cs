namespace AxCrypt.App.Shared.Desktop.Models;

/// <summary>Cloud drive entry shown in the right panel.</summary>
public class CloudDrive
{
    public string Name { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    /// <summary>Raw SVG logo string rendered via MarkupString.</summary>
    public string LogoSvg { get; set; } = string.Empty;
}
