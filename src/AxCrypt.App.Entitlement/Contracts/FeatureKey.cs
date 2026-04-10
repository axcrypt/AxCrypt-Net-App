namespace AxCrypt.App.Entitlement.Contracts;

/// <summary>
/// Identifies a feature whose usage is metered for free-tier users.
/// Premium and Business have no caps and the enum is ignored for them.
///
/// Defaults shown next to each value are baseline caps the local
/// FeatureUsageAdapter falls back to when the server hasn't yet
/// returned a fresh snapshot — the canonical limits live on the API.
///
/// Lives in <c>AxCrypt.App.Entitlement</c> (NOT in AxCrypt.App.Shared)
/// to avoid a circular project reference — Shared → Entitlement is the
/// one allowed direction.
/// </summary>
public enum FeatureKey
{
    /// <summary>Files the user encrypts (Home screen, drag-drop, batch).
    /// Default free cap: 3 / month.</summary>
    FileEncryption = 1,

    /// <summary>Key-share invitations sent for an encrypted file.
    /// Default free cap: 1 file, 1 recipient.</summary>
    KeyShare = 2,

    /// <summary>Password entries created in the Password Manager.
    /// Default free cap: 10 total.</summary>
    PasswordEntry = 3,

    /// <summary>Messages sent through the Secured Messenger.
    /// Default free cap: 10 / month.</summary>
    SecuredMessage = 4,
}
