using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.App.Shared.Desktop.Models;

public class HelpItem
{
    public string TabId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconSvg { get; set; } = string.Empty;
    public string IconBg { get; set; } = "#eff6ff";
    public string Shortcut { get; set; } = string.Empty;
}
