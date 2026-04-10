using System.Collections.Generic;

namespace AxCrypt.App.Shared.Desktop.Models;

public class ToolGroup
{
    public string Name { get; set; } = string.Empty;

    public List<ToolItem> Tools { get; set; } = new();

    public bool IsEnable { get; set; } = true;
}