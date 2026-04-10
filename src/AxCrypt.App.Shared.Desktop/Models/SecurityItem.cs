using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.App.Shared.Desktop.Models;

/// <summary>One row in the Security Status panel.</summary>
public class SecurityItem
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Visual state of the row. Drives icon color + tooltip.
    ///  • Active   — feature is enabled and working (green check)
    ///  • Warning  — feature exists but the user should take action (amber)
    ///  • Locked   — feature is in a higher tier; rendered as an upsell row (grey + lock badge)
    /// </summary>
    public SecurityItemState State { get; set; } = SecurityItemState.Active;

    /// <summary>Optional route the row navigates to when clicked.</summary>
    public string? ActionRoute { get; set; }

    /// <summary>Optional short label on the right (e.g. "Set up", "Upgrade").</summary>
    public string? ActionLabel { get; set; }
}

public enum SecurityItemState
{
    Active,
    Warning,
    Locked,
}
