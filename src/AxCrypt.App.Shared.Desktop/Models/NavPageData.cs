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
                    Subtitle = "Your encrypted dashboard — recent files, quick actions, and security status at a glance.",
                    Match = NavLinkMatch.All,
                    StyleClass = "nav-home-ico",
                    SideMenu = true
                },
                new()
                {
                    Href = "/securedfolders",
                    Label = Texts.WatchedFoldersTabPageText,
                    Title = Texts.WatchedFoldersTabPageText,
                    Subtitle = "Folders AxCrypt watches — anything you drop in is automatically encrypted.",
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
                    Subtitle = "A private, encrypted space that automatically protects your files and securely shares them with trusted people.",
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
                    Subtitle = "Store passwords, generate strong ones, and autofill — all protected by AES-256.",
                    Match = NavLinkMatch.Prefix,
                    StyleClass = "nav-pwdmggr-ico",
                    SideMenu = true
                },
                new()
                {
                    Href = "/securedmessenger",
                    Label = Texts.SecuredMessengerLinkLabel,
                    Title = Texts.SecuredMessengerLinkLabel,
                    Subtitle = "Send end-to-end encrypted messages — only the recipient can read them.",
                    Match = NavLinkMatch.Prefix,
                    StyleClass = "nav-secdmsgr-ico",
                    SideMenu = true
                },
                new()
                {
                    Href = "/textencryption",
                    Label = Texts.TextEncryptionLinkLabel,
                    Title = Texts.TextEncryptionLinkLabel,
                    Subtitle = "Paste any text, encrypt it, and share the ciphertext safely anywhere.",
                    IsPaid = true,
                    Match = NavLinkMatch.Prefix,
                    StyleClass = "nav-txtencryp-ico",
                    SideMenu = true
                },
                new()
                {
                    Href = "/tools",
                    Label = "Tools",
                    Title = "Tools",
                    Subtitle = "Secure-delete, anonymous rename, file verification, and other AxCrypt utilities.",
                    Match = NavLinkMatch.Prefix,
                    StyleClass = "nav-tools-ico",
                    SideMenu = true
                },
                new()
                {
                    Href = "/helpcenter",
                    Label = "Help & Support",
                    Title = "Help Center",
                    Subtitle = "Browse FAQs, search the knowledge base, send feedback, or reach our support team.",
                    Match = NavLinkMatch.Prefix,
                    StyleClass = "nav-suprt-ico",
                    SideMenu = true
                },
                new()
                {
                    Href = "/notification",
                    Label = Texts.PromptNotificationsText,
                    Title = Texts.PromptNotificationsText,
                    Subtitle = "Activity alerts, share-key invitations, and system messages in one place.",
                    Match = NavLinkMatch.Prefix,
                    StyleClass = "nav-suprt-ico",
                    SideMenu = false
                },
            };
        }
    }
}
