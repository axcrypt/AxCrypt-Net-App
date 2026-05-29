namespace AxCrypt.App.Shared.Desktop.Models;

public class NewActionItem
{
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconSvg { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public bool IsEnable { get; set; } = true;
}