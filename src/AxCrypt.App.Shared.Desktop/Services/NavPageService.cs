using System;
using System.Collections.Generic;
using System.Linq;
using AxCrypt.App.Shared.Desktop.Models;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Content;

namespace AxCrypt.App.Shared.Desktop.Services;

public class NavPageService
{
    // ═══════════════════════════════════════════
    //  PAGE TITLES (dynamic per nav item)
    // ═══════════════════════════════════════════

    // Intentionally a property (not a static field) so Texts.* re-reads
    // the current culture on every access. A static field would bake in
    // the startup language and never update after a language change + reload.
    private static PageMeta DefaultCurrentPageMeta =>
        new()
        {
            Title    = Texts.HomeLinkLabel,
            Subtitle = Texts.NavPageSubtitle,
        };

    // Initialised lazily via the property so the first read also picks up
    // the correct culture even if the user changed language before login.
    public PageMeta CurrentPage { get; set; } = DefaultCurrentPageMeta;

    public void SetActivePage(NavPage page)
    {
        CurrentPage = page;
        UpdateViewState();
    }

    public void SetActivePage(string navUrl)
    {
        CurrentPage = NavPageData.NavPages.FirstOrDefault(nv => nv.Href == navUrl) ?? DefaultCurrentPageMeta;
        UpdateViewState();
    }

    /// <summary>Re-reads the title and subtitle for the current page using the
    /// active culture. Call after a language change to update the TopBar title
    /// without requiring a full navigation.</summary>
    public void RefreshCurrentPage()
    {
        string href = (CurrentPage as NavPage)?.Href ?? "/";
        SetActivePage(href);
    }

    public IEnumerable<NavPage>? SideMenuNavPages
    {
        get
        {
            return NavPageData.NavPages?.Where(np => np.SideMenu) ?? new List<NavPage>();
        }
    }

    public event Action? OnUpdateViewState;

    public void UpdateViewState()
    {
        OnUpdateViewState?.Invoke();
    }
}
