using AxCrypt.App.Entitlement.Contracts;
using AxCrypt.App.Shared.Desktop.Models;
using AxCrypt.App.Shared.Desktop.Services;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Content;
using AxCrypt.Core.UI.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AxCrypt.App.Shared.Desktop.ViewModels.Home;

/// <summary>
/// ViewModel for the "New Secure Action" CTA panel on the home page.
/// Owns the new-action menu data and all business-logic dispatch for each action.
/// Navigation calls (NavigateTo) remain in the razor since they are Blazor-specific.
/// </summary>
public class QuickSecureActionViewModel : ViewModelBase
{
    private readonly ActionsViewModel _actionsVm;
    private readonly SubActionViewModel _subActionVm;
    private readonly RecentFoldersViewModel _foldersVm;
    private readonly VaultViewModel _vaultVm;
    private readonly NavPageService _navPageService;
    private readonly PaidFeaturegateService _paidGateService;
    private readonly Main.AppSettingsViewModel _appSettingsVm;

    public QuickSecureActionViewModel(
        ActionsViewModel actionsVm,
        SubActionViewModel subActionVm,
        RecentFoldersViewModel foldersVm,
        VaultViewModel vaultVm,
        NavPageService navPageService,
        PaidFeaturegateService paidGateService,
        Main.AppSettingsViewModel appSettingsVm)
    {
        _actionsVm      = actionsVm;
        _subActionVm    = subActionVm;
        _foldersVm      = foldersVm;
        _vaultVm        = vaultVm;
        _navPageService = navPageService;
        _paidGateService = paidGateService;
        _appSettingsVm  = appSettingsVm;
    }

    // ── Menu data ──────────────────────────────────────────────
    /// <summary>Returns the groups of new-action items shown in the dropdown.</summary>
    public List<NewActionGroup> GetNewActionGroups() => new()
    {
        new NewActionGroup
        {
            GroupLabel = "ENCRYPTION",
            Items = new()
            {
                new() { Label = Texts.QuickSecureActionEncryptAndShare,      Description = Texts.QuickSecureActionEncryptAndShareDescription,  Route = "/actions/share",
                        IconSvg = "<svg width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='#6c757d' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><rect x='3' y='11' width='14' height='11' rx='2'/><path d='M7 11V7a5 5 0 0110 0'/><circle cx='10' cy='16' r='1' fill='#2563eb'/><path d='M18 13h4M20 11l2 2-2 2'/></svg>" },
                new() { Label = Texts.QuickSecureActionSecureFolder,         Description = Texts.QuickSecureActionSecureFolderDescription,      Route = "/actions/encrypt-folder",
                        IconSvg = "<svg width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='#6c757d' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M3 7a2 2 0 012-2h4l2 2h8a2 2 0 012 2v9a2 2 0 01-2 2H5a2 2 0 01-2-2V7z'/><rect x='9' y='11' width='6' height='5' rx='1'/><path d='M11 11v-1.5a1.5 1.5 0 013 0V11'/><circle cx='12' cy='14' r='0.8' fill='#2563eb'/></svg>" },
                new() { Label = Texts.QuickSecureAddFilesVault,              Description = Texts.QuickSecureAddFilesVaultDescription,           Route = "/actions/vault-add",
                        IconSvg = "<svg width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='#6c757d' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><rect x='2' y='3' width='18' height='18' rx='2'/><circle cx='10' cy='12' r='4'/><circle cx='10' cy='12' r='1.5' fill='#2563eb'/><line x1='10' y1='8' x2='10' y2='6'/><line x1='14' y1='12' x2='16' y2='12'/><line x1='10' y1='16' x2='10' y2='18'/><line x1='6' y1='12' x2='4' y2='12'/><rect x='20' y='10' width='2' height='4' rx='1'/><line x1='16' y1='3' x2='16' y2='7'/><line x1='14' y1='5' x2='18' y2='5'/></svg>" },
                new() { Label = Texts.SecureDeleteToolStripMenuItemText,     Description = Texts.QuickSecureSecureDeleteDescription,            Route = "/actions/secure-delete",
                        IconSvg = "<svg width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='#6c757d' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><polyline points='3 6 5 6 21 6'/><path d='M8 6V4a1 1 0 011-1h6a1 1 0 011 1v2'/><rect x='5' y='6' width='14' height='15' rx='2'/><line x1='9' y1='10' x2='9.01' y2='17' stroke-dasharray='1.5 2'/><line x1='12' y1='10' x2='12.01' y2='17' stroke-dasharray='1.5 2'/><line x1='15' y1='10' x2='15.01' y2='17' stroke-dasharray='1.5 2'/></svg>" },
                new() { Label = Texts.QuickSecureActionRecentSecureFiles,    Description = Texts.QuickSecureActionRecentSecureFilesDescription,  Route = "/actions/findfiles",
                        IsEnable = _appSettingsVm.EnableFindFiles,
                        IconSvg = "<svg width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='#6c757d' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h5'/><path d='M14 2v6h6'/><circle cx='17' cy='17' r='5'/><polyline points='17 14 17 17 19 18'/></svg>" },
            }
        }
    };

    // ── Lock checks ────────────────────────────────────────────
    /// <summary>Returns true when the item is gated behind a paid plan for this user.</summary>
    public bool IsLockedFor(NewActionItem item, UserService userService, IFeatureUsageProvider usage) =>
        item.Route switch
        {
            "/actions/share"          => userService.IsFreeTier && !usage.CanUse(FeatureKey.KeyShare),
            "/actions/encrypt-folder" => userService.IsFreeTier,
            "/actions/vault-add"      => userService.IsFreeTier,
            "/actions/secure-delete"  => userService.IsFreeTier,
            "/actions/findfiles"      => userService.IsFreeTier,
            _                         => false
        };

    // ── Action dispatch ────────────────────────────────────────
    /// <summary>
    /// Executes the business logic for a new-action item.
    /// Returns a route string when the caller (razor) should also navigate,
    /// or null when no navigation is needed.
    /// </summary>
    public async Task<string?> HandleNewActionAsync(System.EventArgs e, NewActionItem item, UserService userService, IFeatureUsageProvider usage)
    {
        switch (item.Route)
        {
            case "/actions/share":
                if (userService.IsFreeTier && !usage.CanUse(FeatureKey.KeyShare))
                {
                    _paidGateService.ShowPaidGate(
                        Texts.QuickSecureActionEncryptAndShare,
                        Texts.QuickSecureActionSecureFilesShare,
                        new[] { Texts.QuickSecureActionShareFileAccessSecurely, Texts.QuickSecureActionNoNeedSharePasswords, Texts.QuickSecureActionControlFiles, Texts.QuickSecureActionRemoveAccessAnytime });
                    return null;
                }
                await _actionsVm.ShareKeysAsync(e);
                return null;

            case "/actions/encrypt-folder":
                if (userService.IsFreeTier)
                {
                    _paidGateService.ShowPaidGate(
                        Texts.WatchedFoldersTabPageText,
                        Texts.QuickSecureActionAutomaticallyMonitor,
                        new[] { Texts.QuickSecureActionAutomaticallyEncrypt, Texts.QuickSecureActionMonitorMultiple, Texts.QuickSecureActionProtectSensitive, Texts.QuickSecureActionUnlockAdvanced });
                    return null;
                }
                await _foldersVm.AddSecuredFolder(e);
                return null;

            case "/actions/vault-add":
                if (userService.IsFreeTier)
                {
                    _paidGateService.ShowPaidGate(
                        Texts.VaultText,
                        Texts.QuickSecureActionEncryptedVaults,
                        new[] { Texts.QuickSecureActionSecureEncryptedVaults, Texts.QuickSecureActionProtectImportantFiles, Texts.QuickSecureActionAdvancedVaultSecurity, Texts.QuickSecureActionUnlockPremium });
                    return null;
                }
                // ValidateVaultPath may open a setup dialog; only proceed when valid.
                bool isValid = await _subActionVm.ValidateVaultPath();
                if (isValid)
                {
                    await _vaultVm.AddVaultFiles();
                }
                return null;

            case "/actions/secure-delete":
                if (userService.IsFreeTier)
                {
                    _paidGateService.ShowPaidGate(
                        Texts.SecureDeleteToolStripMenuItemText,
                        Texts.QuickSecureActionPermanentlyDelete,
                        new[] { Texts.QuickSecureActionPermanentFileDeletion, Texts.QuickSecureActionPreventFileRecovery, Texts.QuickSecureActionSensitiveInformation, Texts.QuickSecureActionUnlockSecurityTools });
                    return null;
                }
                _subActionVm.SecureWipeFiles(e);
                return null;

            case "/actions/findfiles":
                if (userService.IsFreeTier)
                {
                    _paidGateService.ShowPaidGate(
                        Texts.FindFileFeatureTitle,
                        Texts.QuickSecureActionUnlockMoreFeatures,
                        new[] { Texts.QuickSecureActionUnlimitedFiles, Texts.PrioritySupportTitle, Texts.QuickSecureActionAdvancedAlgorithms });
                    return null;
                }
                // Tell the razor to navigate to /findfiles and update the nav-page state.
                _navPageService.SetActivePage("/findfiles");
                return "/findfiles";

            default:
                return null;
        }
    }
}
