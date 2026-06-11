using System.Collections.Generic;
using AxCrypt.Content;

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
            Label       = Texts.QuickActionOpenEncrypted,
            Description = Texts.QuickActionOpenEncryptedDescription,
            HelpText    = Texts.QuickActionOpenEncryptedHelpText,
            Type        = QuickActionType.OpenSecured,
            SvgIcon     = "images/default/IcoOpnScrd.svg",
            //Description = "Preview without permanent decrypt",
        },
        new()
        {
            Id          = "encrypt",
            Label       = Texts.QuickActionEncryptFile,
            Description = Texts.QuickActionEncryptFileDescription,
            HelpText    = Texts.QuickActionEncryptFileHelpText,
            Type        = QuickActionType.Encrypt,
            SvgIcon     = "images/default/IcoScr.svg",
            PaidPerks   = new[]{ Texts.QuickActionUnlimitedFileEncryptions, Texts.QuickActionEncryptFilesSeconds, Texts.QuickActionSecureStrongEncryption, Texts.UnlockAdvancedEncryptionFeaturesPopup },
        },
        new()
        {
            Id          = "decrypt",
            Label       = Texts.QuickActionDecryptFile,
            Description = Texts.QuickActionDecryptFileDescription,
            HelpText    = Texts.QuickActionDecryptFileHelpText,
            Type        = QuickActionType.Decrypt,
            SvgIcon     = "images/default/IcoStpScr.svg",
        },
        new()
        {
            Id          = "share",
            Label       = Texts.QuickActionShareSecurely,
            Description = Texts.QuickActionShareSecurelyDescription,
            HelpText    = Texts.QuickActionShareSecurelyHelpText,
            Type        = QuickActionType.ShareKey,
            SvgIcon     = "images/default/IcoShrKs.svg",
            PaidPerks   = new[] { Texts.QuickSecureActionShareFileAccessSecurely, Texts.QuickSecureActionNoNeedSharePasswords, Texts.QuickSecureActionControlFiles, Texts.QuickSecureActionRemoveAccessAnytime },
        },
    };
}