namespace AxCrypt.App.Shared.Desktop.Models;

/// <summary>Individual tool card on the Tools page.</summary>
public class ToolItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconSvg { get; set; } = string.Empty;
    public string IconBg { get; set; } = "#eff6ff";
    public bool IsDanger { get; set; }
    public bool IsNew { get; set; }
    public bool IsBeta { get; set; }
    public bool IsPaid { get; set; }
    public bool IsEnable { get; set; } = true;
}