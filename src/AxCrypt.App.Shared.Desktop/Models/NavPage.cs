using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Routing;

namespace AxCrypt.App.Shared.Desktop.Models;

public class NavPage : PageMeta
{
    public string Href { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string StyleClass { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public NavLinkMatch Match { get; set; } = NavLinkMatch.Prefix;

    public bool SideMenu { get; set; }
}

public class PageMeta
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
}

public class SearchResult
{
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SubText { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string IconSvg { get; set; } = string.Empty;

    /// <summary>
    /// True when the result represents a feature the user's plan can't use.
    /// The GlobalSearchComponent renders these rows with a lock badge and
    /// routes the click to the paid-gate popup instead of activating them.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Optional action invoked when the result is clicked. When set, it takes
    /// precedence over <see cref="Route"/> based navigation — used so a Settings
    /// result can perform the setting directly and a Folder result can open it.
    /// </summary>
    public Func<Task>? OnClick { get; set; }
}
