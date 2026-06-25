using AxCrypt.App.Shared.Desktop.Models;
using AxCrypt.App.Shared.Desktop.Services;
using AxCrypt.App.Shared.Services;
using AxCrypt.Content;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Linq;

namespace AxCrypt.App.Shared.Desktop.ViewModels.Main;

/// <summary>
/// ViewModel for the application sidebar.
/// Owns navigation access control (paid-gate dispatch), active-tab tracking,
/// and the set of routes that belong to the Password Manager section.
/// </summary>
public class SideBarViewModel : ViewModelBase
{
    private readonly PaidFeaturegateService _paidGateService;
    private readonly UserService _userService;
    private readonly NavPageService _navPageService;

    // Routes whose active link is tracked via a separate TabActive flag
    // rather than Blazor's NavLink automatic matching.
    private static readonly string[] PasswordManagerRoutes = new[]
    {
        "/editpassword", "/editnote", "/editcard",
        "/viewcard",     "/viewnote", "/viewpassword",
        "/AddCard",      "/AddNote",  "/AddPassword",
    };

    /// <summary>CSS active-tab override for routes that share a single nav item.</summary>
    public string TabActive { get; private set; } = string.Empty;

    public SideBarViewModel(PaidFeaturegateService paidGateService, UserService userService, NavPageService navPageService)
    {
        _paidGateService = paidGateService;
        _userService     = userService;
        _navPageService  = navPageService;
    }

    // ── Active tab tracking ────────────────────────────────────
    /// <summary>
    /// Updates TabActive based on the current URL path so that
    /// sub-routes of a nav item still highlight the correct menu entry.
    /// </summary>
    public void UpdateActiveTabForPath(string currentPath)
    {
        TabActive = string.Empty;
        if (PasswordManagerRoutes.Contains(currentPath, StringComparer.OrdinalIgnoreCase))
        {
            TabActive = "passwordManager";
        }
    }

    /// <summary>Returns the extra CSS class when <paramref name="activeTab"/> matches TabActive.</summary>
    public string GetActiveTabClass(string activeTab) =>
        TabActive == activeTab.TrimStart('/') ? "nav-item--active" : string.Empty;

    // ── Navigation access control ──────────────────────────────
    /// <summary>
    /// Handles a nav-menu click. For paid routes accessed by free users,
    /// shows the tier-targeted paid-gate popup and returns false (do not navigate).
    /// For permitted routes, updates the active page and returns true.
    /// </summary>
    public bool HandleNavMenuClick(NavPage navPage)
    {
        if (navPage.IsPaid && _userService.IsFreeTier)
        {
            _paidGateService.ShowPaidGateForPage(navPage.Href);
            return false; // caller should not navigate
        }

        _navPageService.SetActivePage(navPage);
        return true; // caller may navigate normally
    }
}
