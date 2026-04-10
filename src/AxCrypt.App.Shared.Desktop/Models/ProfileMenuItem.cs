namespace AxCrypt.App.Shared.Desktop.Models;

/// <summary>Item in the profile popup menu.</summary>
public class ProfileMenuItem
{
    public string Label { get; set; } = string.Empty;
    public string SubLabel { get; set; } = string.Empty;
    public string IconSvg { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    /// <summary>Optional badge text (e.g. "3 active").</summary>
    public string Badge { get; set; } = string.Empty;
    public bool IsDanger { get; set; }
    public bool IsDivider { get; set; }
    public string StyleClass { get; set; } = string.Empty;

}