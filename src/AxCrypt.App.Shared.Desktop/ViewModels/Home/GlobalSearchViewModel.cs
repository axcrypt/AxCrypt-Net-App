using AxCrypt.App.Shared.Desktop.Models;
using AxCrypt.App.Shared.Desktop.Services;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Content;
using AxCrypt.Core.UI.ViewModel;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AxCrypt.App.Shared.Desktop.ViewModels.Home;

/// <summary>
/// ViewModel for the global search component in the top bar.
/// Owns search result construction, paid-gate dispatch for locked results,
/// and result-action execution. Navigation and JS interop stay in the razor.
/// </summary>
public class GlobalSearchViewModel : ViewModelBase
{
    private readonly RecentFilesViewModel _recentFilesVm;
    private readonly RecentFoldersViewModel _recentFoldersVm;
    private readonly Main.AppSettingsViewModel _appSettingsVm;
    private readonly PopupService _popupService;
    private readonly NavPageService _navPageService;
    private readonly PaidFeaturegateService _paidGateService;
    private readonly UserService _userService;

    // ── Inline SVG constants ───────────────────────────────────
    private const string FileIcon     = "<svg width='14' height='14' viewBox='0 0 24 24' fill='none' stroke='#3b82f6' stroke-width='2'><path d='M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z'/><polyline points='14 2 14 8 20 8'/></svg>";
    private const string FolderIcon   = "<svg width='14' height='14' viewBox='0 0 24 24' fill='none' stroke='#0ea5e9' stroke-width='2'><path d='M22 19a2 2 0 01-2 2H4a2 2 0 01-2-2V5a2 2 0 012-2h5l2 3h9a2 2 0 012 2z'/></svg>";
    private const string SettingsIcon = "<svg width='14' height='14' viewBox='0 0 24 24' fill='none' stroke='#f59e0b' stroke-width='2'><circle cx='12' cy='12' r='3'/><path d='M19.07 4.93l-1.41 1.41M5.34 18.66l-1.41 1.41M20 12h2M2 12h2M19.07 19.07l-1.41-1.41M5.34 5.34L3.93 3.93M12 20v2M12 2v2'/></svg>";
    private const string ToolsIcon    = "<svg width='14' height='14' viewBox='0 0 24 24' fill='none' stroke='#8b5cf6' stroke-width='2'><path d='M14.7 6.3a1 1 0 000 1.4l1.6 1.6a1 1 0 001.4 0l3.77-3.77a6 6 0 01-7.94 7.94l-6.91 6.91a2.12 2.12 0 01-3-3l6.91-6.91a6 6 0 017.94-7.94l-3.76 3.76z'/></svg>";
    private const string PageIcon     = "<svg width='14' height='14' viewBox='0 0 24 24' fill='none' stroke='#10b981' stroke-width='2'><rect x='3' y='3' width='18' height='18' rx='2'/></svg>";

    // Tool IDs gated behind a paid plan — mirrors the Tools page logic.
    private static readonly HashSet<string> PaidToolIds = new(StringComparer.OrdinalIgnoreCase)
        { "find-files", "anon-rename", "restore-rename", "upgrade-tool", "secure-delete" };

    // In-app page routes gated for free users.
    private static readonly HashSet<string> PaidRoutes = new(StringComparer.OrdinalIgnoreCase)
        { "/vault", "/securedfolders", "/textencryption" };

    public GlobalSearchViewModel(
        RecentFilesViewModel recentFilesVm,
        RecentFoldersViewModel recentFoldersVm,
        Main.AppSettingsViewModel appSettingsVm,
        PopupService popupService,
        NavPageService navPageService,
        PaidFeaturegateService paidGateService,
        UserService userService)
    {
        _recentFilesVm   = recentFilesVm;
        _recentFoldersVm = recentFoldersVm;
        _appSettingsVm   = appSettingsVm;
        _popupService    = popupService;
        _navPageService  = navPageService;
        _paidGateService = paidGateService;
        _userService     = userService;
    }

    // ── Search result construction ─────────────────────────────
    /// <summary>
    /// Builds up to 12 search results spanning Files, Folders, Settings, Tools, and Pages.
    /// Free-tier users see locked entries with a paid-gate badge.
    /// Settings entries that toggle state directly carry an OnClick delegate.
    /// Settings entries that require UI carry an OpenSettingsMenu flag.
    /// </summary>
    public List<SearchResult> BuildSearchResults(string q, Func<Task> openSettingsAsync)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return new();
        }

        bool freeGate = _userService.IsFreeTier;
        var results = new List<SearchResult>();

        // Files — recent encrypted files.
        results.AddRange(_recentFilesVm.RecentFilesList
            .Where(f => f.FileName.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Select(f => new SearchResult
            {
                Category = "Files",
                Name     = f.FileName,
                SubText  = f.Algorithm + " · " + f.FileSize,
                Route    = f.FilePath,
                IconSvg  = FileIcon,
                IsLocked = freeGate,
            }));

        // Folders — watched/secured folders.
        results.AddRange(_recentFoldersVm.RecentFoldersList
            .Where(folder =>
                Path.GetFileName(folder.TrimEnd('/', '\\'))
                    .Contains(q, StringComparison.OrdinalIgnoreCase)
                || folder.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Select(folder => new SearchResult
            {
                Category = "Folders",
                Name     = Path.GetFileName(folder.TrimEnd('/', '\\')),
                SubText  = folder,
                IconSvg  = FolderIcon,
                IsLocked = freeGate,
                OnClick  = () =>
                {
                    _recentFoldersVm.HandleDoubleClick(new MouseEventArgs { Type = "dblclick" }, folder);
                    return Task.CompletedTask;
                },
            }));

        // Settings — direct toggles or "Open Settings" redirects.
        (string Name, string Keywords, string SubText, Func<Task> Action)[] settings =
        [
            (Texts.SwitchToDarkActionText, "appearance theme dark light mode display colour color night", "Toggle light / dark theme",   openSettingsAsync),
            (Texts.OptionsHideRecentFilesToolStripMenuItemText, "hide recent files privacy home screen list", "Toggle on / off", () => { _appSettingsVm.ToggleHideRecentFiles(); return Task.CompletedTask; }),
            (Texts.AlwaysOffline, "offline always offline network sync server connection", "Toggle on / off", () => { _appSettingsVm.ToggleAlwaysOffline(EventArgs.Empty); return Task.CompletedTask; }),
            (Texts.OptionsLanguageToolStripMenuItemText, "language locale region translation culture", "Open Settings", openSettingsAsync),
            (Texts.IdleSignOutToolStripMenuItemText, "auto sign out inactivity timeout idle lock session", "Open Settings", openSettingsAsync),
            (Texts.AdvancedOptionsTitle, "advanced settings preferences vault options properties", "Open Settings", openSettingsAsync),
            (Texts.FilePropertiesToolStripMenuItemText, "privacy security preferences settings options", "Open Settings", openSettingsAsync),
        ];
        results.AddRange(settings
            .Where(s => s.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                     || s.Keywords.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Select(s => new SearchResult
            {
                Category = "Settings",
                Name     = s.Name,
                SubText  = s.SubText,
                IconSvg  = SettingsIcon,
                OnClick  = s.Action,
            }));

        // Tools — every enabled tool card on the Tools page.
        (string Id, string Name, string Keywords, bool IsEnabled)[] toolNames =
        [
            ("secure-delete", Texts.McInfMenuWipe, "secure delete permanently wipe shred", true),
            ("anon-rename", Texts.AnonymousRenameMenuText, "hide filename anonymize anonymous rename", true),
            ("restore-rename", Texts.RestoreAnonymousNamesMenuText, "restore original names undo rename", true),
            ("find-files", Texts.FindFileFeatureTitle, "scan find encrypted locate files", _appSettingsVm.EnableFindFiles),
            ("activity-logs", Texts.UserActivityTitle, "activity log history file actions audit", _appSettingsVm.EnableUserActivity),
            ("upgrade-tool", Texts.UpgradeLegacyFilesMenuItemText, "upgrade aes 256 128 re-encrypt legacy", true),
            ("invite", Texts.InviteFriendText, "invite share referral friend colleague", true),
            ("import-public-key",Texts.ImportSomeonesPublicSharingKeyText, "import public sharing key", true),
            ("export-public-key", Texts.DialogExportSharingKeyTitle, "export public sharing key", true),
            ("export-axcryptid-public-key",Texts.DialogExportAxCryptIdTitle, "export axcrypt id private key backup", true),
        ];

        results.AddRange(toolNames
            .Where(t => t.IsEnabled
                     && (t.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                      || t.Keywords.Contains(q, StringComparison.OrdinalIgnoreCase)))
            .Select(t => new SearchResult
            {
                Category = "Tools",
                Name     = t.Name,
                SubText  = "Open in Tools",
                Route    = $"/tools?tool={t.Id}",
                IconSvg  = ToolsIcon,
                IsLocked = freeGate && PaidToolIds.Contains(t.Id),
            }));

        // Pages — in-app nav items.
        var pages = new (string Name, string SubText, string Route)[]
        {
            ("Home",                "Dashboard & quick actions",      "/"),
            ("Vault",               "Your secure vault",              "/vault"),
            ("Secured Folders",     "Auto-encrypt watched folders",   "/securedfolders"),
            ("Password Manager",    "Passwords, cards & notes",       "/passwordManager"),
            ("Secured Messenger",   "Encrypted messages",             "/securedmessenger"),
            ("Text Encryption",     "Encrypt & share text",           "/textencryption"),
            ("Notifications",       "Alerts & updates",               "/notification"),
            ("Help & Support",      "Guides and contact",             "/helpcenter"),
        };
        results.AddRange(pages
            .Where(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Select(p => new SearchResult
            {
                Category = "Pages",
                Name     = p.Name,
                SubText  = p.SubText,
                Route    = p.Route,
                IconSvg  = PageIcon,
                IsLocked = freeGate && PaidRoutes.Contains(p.Route),
            }));

        return results.Take(12).ToList();
    }

    // ── Result dispatch ────────────────────────────────────────
    /// <summary>
    /// Executes the action for a selected search result.
    /// Returns a navigation route when the caller (razor) should also call NavigateTo,
    /// or null when no navigation is needed (action handled here).
    /// </summary>
    public async Task<string?> HandleSearchResultAsync(SearchResult r)
    {
        if (r.IsLocked)
        {
            // Per-category paid-gate copy.
            if (r.Category == "Pages")
            {
                _paidGateService.ShowPaidGateForPage(r.Route);
            }
            else
            {
                string headline = r.Category switch
                {
                    "Files"   => "File search",
                    "Folders" => "Secured folder search",
                    _         => r.Name,
                };
                _paidGateService.ShowPaidGate(
                    headline,
                    Texts.PagesHeadline,
                    new[] { Texts.PagesSearchAcrossPopup, Texts.PagesSearchOpenSecuredPopup, Texts.PagesUnlockFindFilesPopup, Texts.PagesPrioritySupportPopup });
            }
            return null;
        }

        // Delegate results — settings toggles and folder opens.
        if (r.OnClick != null)
        {
            await r.OnClick.Invoke();
            return null;
        }

        // File open — mirrors Recent Files double-click.
        if (r.Category == "Files")
        {
            await _recentFilesVm.OpenSecuredMouseDoubleClick(
                new MouseEventArgs { Type = "dblclick" }, r.Route);
            return null;
        }

        // Tools & Pages — close all popups, set active page, return route for razor.
        _popupService.CloseAllPopups();
        _navPageService.SetActivePage(r.Route.Split('?')[0]);
        return r.Route;
    }
}
