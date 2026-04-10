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

    private static PageMeta DefaultCurrentPageMeta =
        new()
        {
            Title = Texts.WelcomeBackTitle,
            Subtitle = "Your encrypted dashboard — recent files, quick actions, and security status at a glance."
        };

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
