// Models/SettingsModels.cs
// Add these to HomeModels.cs or keep as a standalone file.
using System;

using System;

using System.Threading.Tasks;

namespace SecureApp.Models;

/// <summary>A toggle row in the main Settings panel (Privacy & Security section).</summary>
public class SettingToggleItem
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconSvg { get; set; } = string.Empty;
    public bool IsOn { get; set; }

    /// <summary>When true and IsFreeTier is true, shows a lock icon instead of a toggle.</summary>
    public bool IsPaid { get; set; }
}

/// <summary>A single property in the Encryption File Properties popup.</summary>
public class FilePropertyItem
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconSvg { get; set; } = string.Empty;
    public Func<Task> OnClickFunc { get; set; }

    public string[] PaidPerks { get; set; } = Array.Empty<string>();

    /// <summary>"columns" or "security" — controls which section the item renders in.</summary>
    public string Section { get; set; } = string.Empty;

    public bool IsToggled { get; set; }
    public bool IsEnable { get; set; }
}

/// <summary>A nav row in the Advanced Options popup that navigates to a deeper page.</summary>
public class AdvancedNavItem
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconSvg { get; set; } = string.Empty;
    public string IconBg { get; set; } = "#f5f3ff";
    public string Route { get; set; } = string.Empty;

    /// <summary>
    /// When true + the user is on the free tier, the row renders a
    /// lock badge and a click opens the paid-gate popup using
    /// <see cref="PaidPerks"/> + the label/description as the pitch.
    /// </summary>
    public bool IsPaid { get; set; }

    /// <summary>Perks shown in the paid-gate popup for this row.</summary>
    public string[] PaidPerks { get; set; } = Array.Empty<string>();
}

/// <summary>A toggle row in the Advanced Options diagnostics section.</summary>
public class AdvancedToggleItem
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconSvg { get; set; } = string.Empty;
    public string IconBg { get; set; } = "#dbeafe";
    public bool IsOn { get; set; }

    /// <summary>When true, shows an amber privacy warning below the row while the toggle is ON.</summary>
    public bool ShowWarning { get; set; }

    /// <summary>
    /// When true + the user is on the free tier, the toggle is
    /// replaced by a lock badge and a click on the row opens the
    /// paid-gate popup using <see cref="PaidPerks"/> as the pitch.
    /// </summary>
    public bool IsPaid { get; set; }

    /// <summary>Perks shown in the paid-gate popup for this row.</summary>
    public string[] PaidPerks { get; set; } = Array.Empty<string>();
}

/// <summary>A notification toggle row in the Notifications popup.</summary>
public class NotificationItem
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconSvg { get; set; } = string.Empty;
    public string IconBg { get; set; } = "#dbeafe";

    /// <summary>"file" or "security" — controls section grouping.</summary>
    public string Section { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }
}