using System;

namespace AxCrypt.App.Shared.Desktop.Models;

// Models/HomeModels.cs
// Place this file in your Models folder inside the MAUI Blazor project.

/// <summary>Quick-action card shown in the home dashboard.</summary>
public class QuickActionItem
{
    public string Id { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public QuickActionType Type { get; set; } = QuickActionType.None;

    public string Description { get; set; } = string.Empty;

    public string HelpText { get; set; } = string.Empty;

    /// <summary>Raw SVG string rendered via MarkupString.</summary>
    public string SvgIcon { get; set; } = string.Empty;

    public bool IsPaid { get; set; }

    public string[] PaidPerks { get; set; } = Array.Empty<string>();
}

public enum QuickActionType
{
    None,

    OpenSecured,

    Encrypt,

    Decrypt,

    ShareKey,
}
