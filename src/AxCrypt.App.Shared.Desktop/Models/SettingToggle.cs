using System;
using System.Threading.Tasks;

namespace AxCrypt.App.Shared.Desktop.Models;

/// <summary>Toggle switch item in Settings popup.</summary>
public class SettingToggle
{
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsOn { get; set; }

    /// <summary>
    /// True when the user's plan grants this feature. When false the
    /// row renders a lock badge and clicking the row (or the badge)
    /// opens the paid-gate popup using <see cref="PaidPerks"/> + the
    /// label/description as the pitch copy.
    /// </summary>
    public bool IsEnable { get; set; } = true;

    public string ImageClass { get; set; } = string.Empty;

    /// <summary>
    /// Per-feature perks shown in the paid-gate popup when a free
    /// user clicks a locked row. Falls back to a generic upgrade
    /// pitch when empty.
    /// </summary>
    public string[] PaidPerks { get; set; } = Array.Empty<string>();

    public required Func<Task> OnClickFunc { get; set; }
}