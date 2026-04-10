namespace AxCrypt.App.Shared.Desktop.Components.Shared;

/// <summary>
/// Layout for <see cref="AxLoader"/>.
/// <list type="bullet">
/// <item><see cref="FullPage"/> — scrim + centered ring, blocks the
///   whole app shell (sign-in, resume-session, big initial sync).</item>
/// <item><see cref="Body"/> — inline ring with no scrim, for use
///   inside a page body (Password Manager, Secured Messenger,
///   Notifications, etc.) so the topbar + sidebar stay live.</item>
/// </list>
/// </summary>
public enum LoaderMode
{
    Body,
    FullPage,
}
