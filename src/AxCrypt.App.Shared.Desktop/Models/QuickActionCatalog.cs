using System.Collections.Generic;

namespace AxCrypt.App.Shared.Desktop.Models;

/// <summary>
/// Single source of truth for the Quick Action tiles shown on the home
/// dashboard. Decoupled from <c>QuickAction.razor</c> so that:
///   • adding a new tile means adding one entry here (no markup edits)
///   • the same catalog can power other surfaces later (e.g. the
///     legacy <c>ActionsComponent.razor</c>, or a command palette)
///   • copy / perks can be localized + unit-tested without spinning
///     up a Razor host.
///
/// Ordering of the list = visual order on the home screen.
/// </summary>
public static class QuickActionCatalog
{
    public static IReadOnlyList<QuickActionItem> Items { get; } = new List<QuickActionItem>
    {
        new()
        {
            Id          = "open",
            Label       = "Open Encrypted",
            Description = "Preview encrypted files without decrypting permanently",
            HelpText    = "Open and view encrypted files securely while keeping the original file encrypted on disk.",
            Type        = QuickActionType.OpenSecured,
            SvgIcon     = "images/default/IcoOpnScrd.svg",
            //Description = "Preview without permanent decrypt",
        },
        new()
        {
            Id          = "encrypt",
            Label       = "Encrypt File",
            Description = "Secure files with AES-256 encryption",
            HelpText    = "You've reached the free encryption limit. Upgrade to continue protecting files without restrictions.",
            Type        = QuickActionType.Encrypt,
            SvgIcon     = "images/default/IcoScr.svg",
            PaidPerks   = new[]{ "Unlimited file encryptions", "Encrypt files in seconds", "Secure files with strong encryption", "Unlock advanced encryption features" },
        },
        new()
        {
            Id          = "decrypt",
            Label       = "Decrypt File",
            Description = "Unlock and access protected files",
            HelpText    = "Decrypt encrypted files securely using the correct password.",
            Type        = QuickActionType.Decrypt,
            SvgIcon     = "images/default/IcoStpScr.svg",
        },
        new()
        {
            Id          = "share",
            Label       = "Share Securely",
            Description = "Share secure access to encrypted files",
            HelpText    = "Securely share encrypted files with others without sharing passwords.",
            Type        = QuickActionType.ShareKey,
            SvgIcon     = "images/default/IcoShrKs.svg",
            PaidPerks   = new[] { "Share file access securely", "No need to share passwords", "Control who can access files", "Remove access anytime" },
        },
    };
}