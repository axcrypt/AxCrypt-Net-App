using System;
using System.Collections.Generic;
using AxCrypt.Content;
using Microsoft.AspNetCore.Components.Routing;

namespace AxCrypt.App.Shared.Desktop.Models;

public static class NavPageData
{
    public static IEnumerable<NavPage> NavPages
    {
        get
        {
            return new List<NavPage>() {
                new()
                {
                    Href = "/",
                    Label = Texts.HomeLinkLabel,
                    Title = Texts.WelcomeBackTitle,
                    Subtitle = Texts.NavPageSubtitle,
                    Match = NavLinkMatch.All,
                    StyleClass = "nav-home-ico",
                    SideMenu = true
                },
                new()
                {
                    Href = "/securedfolders",
                    Label = Texts.WatchedFoldersTabPageText,
                    Title = Texts.WatchedFoldersTabPageText,
                    Subtitle = Texts.NavPageSecuredFoldersSubtitle,
                    IsPaid = true,
                    Match = NavLinkMatch.Prefix,
                    StyleClass = "nav-folders-ico",
                    SideMenu = true
                },
                new()
                {
                    Href = "/vault",
                    Label = Texts.VaultText,
                    Title = Texts.VaultText,
                    Subtitle = Texts.NavPageVaultSubtitle,
                    Match = NavLinkMatch.Prefix,
                    IsPaid = true,
                    StyleClass = "nav-vault-ico",
                    SideMenu = true
                },
                new()
                {
                    Href = "/passwordManager",
                    Label = Texts.PasswordManagerLinkLabel,
                    Title = Texts.PasswordManagerLinkLabel,
                    Subtitle = Texts.NavPagePasswordManagerSubtitle,
                    Match = NavLinkMatch.Prefix,
                    StyleClass = "nav-pwdmggr-ico",
                    SideMenu = true
                },
                new()
                {
                    Href = "/securedmessenger",
                    Label = Texts.SecuredMessengerLinkLabel,
                    Title = Texts.SecuredMessengerLinkLabel,
                    Subtitle = Texts.NavPageSecuredMessengerSubtitle,
                    Match = NavLinkMatch.Prefix,
                    StyleClass = "nav-secdmsgr-ico",
                    SideMenu = true
                },
                new()
                {
                    Href = "/textencryption",
                    Label = Texts.TextEncryptionLinkLabel,
                    Title = Texts.TextEncryptionLinkLabel,
                    Subtitle = Texts.NavPageTextEncryptionSubtitle,
                    IsPaid = true,
                    Match = NavLinkMatch.Prefix,
                    StyleClass = "nav-txtencryp-ico",
                    SideMenu = true
                },
                new()
                {
                    Href = "/tools",
                    Label = Texts.NavPageToolsLinkLabel,
                    Title = Texts.NavPageToolsLinkLabel,
                    Subtitle = Texts.NavPageToolsSubtitle,
                    Match = NavLinkMatch.Prefix,
                    StyleClass = "nav-tools-ico",
                    SideMenu = true
                },
                new()
                {
                    Href = "/helpcenter",
                    Label = Texts.NavPageHelpCenterLinkLabel,
                    Title = Texts.NavPageHelpCenterTitle,
                    Subtitle = Texts.NavPageHelpCenterSubtitle,
                    Match = NavLinkMatch.Prefix,
                    StyleClass = "nav-suprt-ico",
                    SideMenu = true
                },
                new()
                {
                    Href = "/notification",
                    Label = Texts.PromptNotificationsText,
                    Title = Texts.PromptNotificationsText,
                    Subtitle = Texts.NavPageToolsNotificationsSubtitle,
                    Match = NavLinkMatch.Prefix,
                    StyleClass = "nav-suprt-ico",
                    SideMenu = false
                },
                new()
                {
                    Href = "/findfiles",
                    Label = Texts.FindFileFeatureTitle,
                    Title = Texts.FindFileFeatureTitle,
                    Subtitle = Texts.FindFilesFeatureDescription,
                    Match = NavLinkMatch.Prefix,
                    StyleClass = "nav-tools-ico",
                    SideMenu = false   // accessed via Tools page, not sidebar
                },
            };
        }
    }
}
