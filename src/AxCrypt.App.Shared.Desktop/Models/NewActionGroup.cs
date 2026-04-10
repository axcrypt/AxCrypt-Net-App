using System.Collections.Generic;

namespace AxCrypt.App.Shared.Desktop.Models;

public class NewActionGroup
{
    public string GroupLabel { get; set; } = string.Empty;
    public List<NewActionItem> Items { get; set; } = new();
}
