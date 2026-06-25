using AxCrypt.App.Shared.Desktop.Models;
using AxCrypt.App.Shared.Desktop.Services;
using AxCrypt.App.Shared.Services;
using AxCrypt.Content;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AxCrypt.App.Shared.Desktop.ViewModels.Main;

/// <summary>
/// ViewModel for the top application bar.
/// Owns settings toggle configuration (which toggles exist, their enabled/locked state),
/// paid-gate dispatch for locked settings, and the upgrade-availability click handler.
/// JS interop (theme apply, html lang) stays in the razor since it is Blazor-specific.
/// </summary>
public class TopBarViewModel : ViewModelBase
{
    private readonly AppSettingsViewModel _settingsVm;
    private readonly PaidFeaturegateService _paidGateService;

    public TopBarViewModel(AppSettingsViewModel settingsVm, PaidFeaturegateService paidGateService)
    {
        _settingsVm      = settingsVm;
        _paidGateService = paidGateService;
    }

    // ── Settings toggles ───────────────────────────────────────
    /// <summary>
    /// Builds the ordered list of setting-toggle rows shown in the top-bar
    /// settings menu. Each toggle carries its label, enabled flag, current
    /// on/off state, and the action to run when the user flips it.
    /// JS-side effects (setTheme, setHtmlLang) are handled by callbacks
    /// supplied by the razor via <see cref="BuildSettingToggles"/>.
    /// </summary>
    public List<SettingToggle> BuildSettingToggles(Func<Task> onThemeToggle) => new()
    {
        new() {
            Label       = Texts.OptionsHideRecentFilesToolStripMenuItemText,
            Description = Texts.OptionsHideRecentFilesDescription,
            ImageClass  = "stngs-menu-icon IcoEye",
            IsEnable    = true,
            IsOn        = _settingsVm.HideRecentFiles,
            OnClickFunc = () => { _settingsVm.ToggleHideRecentFiles(); return Task.CompletedTask; },
        },
        new() {
            Label       = Texts.AlwaysOffline,
            Description = Texts.AlwaysOfflineDescription,
            ImageClass  = "stngs-menu-icon IcoOffln",
            IsEnable    = true,
            IsOn        = _settingsVm.AlwaysOffline,
            OnClickFunc = () => { _settingsVm.ToggleAlwaysOffline(null!); return Task.CompletedTask; },
        },
        new() {
            Label       = Texts.OptionsConvertMenuItemText,
            Description = Texts.OptionsConvertMenuItemDescription,
            ImageClass  = "stngs-menu-icon Ico256",
            IsEnable    = _settingsVm.EnableAutoUpgrade,
            IsOn        = _settingsVm.AutoUpgradeToAES256,
            PaidPerks   = new[] { Texts.AutoUpgradeFilesAES256, Texts.BackgroundEncryptionMigrationPopup, Texts.OneLatestStandardPopup },
            OnClickFunc = async () => await _settingsVm.ToggleEncryptionUpgradeMode(),
        },
        new() {
            Label       = Texts.OptionsIncludeSubfoldersToolStripMenuItemText,
            Description = Texts.OptionsIncludeSubfoldersDescription,
            ImageClass  = "stngs-menu-icon IcoIncldFldrs24px",
            IsEnable    = _settingsVm.EnableIncludeSubfolders,
            IsOn        = _settingsVm.IncludeSubfolders,
            PaidPerks   = new[] { Texts.EncryptFilesAllSubFoldersPopup, Texts.BatchEncryptMultiplePopup, Texts.KeepFolderStructurePopup, Texts.UnlockAdvancedFolderPopup },
            OnClickFunc = async () => await _settingsVm.ToggleIncludeSubfolders(null!),
        },
        new() {
            Label       = _settingsVm.DarkTheme ? Texts.SwitchToLightActionText : Texts.SwitchToDarkActionText,
            Description = Texts.SwitchToDarkActionDescription,
            ImageClass  = "stngs-menu-icon IcoDkMd",
            IsEnable    = true,
            IsOn        = _settingsVm.DarkTheme,
            // Theme toggle needs JS — delegate is provided by the razor.
            OnClickFunc = onThemeToggle,
        },
    };

    // ── Locked-row click ───────────────────────────────────────
    /// <summary>
    /// Called when the user clicks a settings row (not the inner toggle button).
    /// For locked rows, shows the paid-gate popup and returns false.
    /// For enabled rows, returns true so the razor can no-op or delegate to the toggle.
    /// </summary>
    public bool HandleSettingToggleRowClick(SettingToggle tog)
    {
        if (tog.IsEnable)
        {
            return true; // enabled — toggle button owns the click
        }

        // Locked: close any open popup then surface the upgrade prompt.
        _paidGateService.ShowPaidGate(
            feature: tog.Label,
            desc: string.IsNullOrWhiteSpace(tog.Description)
                ? Texts.AvailablePremiumBusinessPlans
                : tog.Description,
            perks: tog.PaidPerks.Length > 0
                ? tog.PaidPerks
                : new[] { Texts.QuickActionUnlimitedFileEncryptions, Texts.SecureKeySharingPopup, Texts.PrioritySupportTitle });

        return false; // locked — caller should close the settings popup
    }

    // ── Update pill ────────────────────────────────────────────
    /// <summary>
    /// Handles the "update available" pill click — redirects to the stored
    /// download URL rather than re-running the version check.
    /// </summary>
    public void HandleUpdateClick()
    {
        Core.BrowseUtility.RedirectTo(Core.Resolve.UserSettings.UpdateUrl.ToString());
    }

    // ── Inactivity sign-out upgrade prompt ─────────────────────
    /// <summary>Shows the paid-gate popup for the locked Inactivity Sign-Out option.</summary>
    public void ShowInactivitySignOutUpgradePopup()
    {
        _paidGateService.ShowPaidGate(
            Texts.IdleSignOutToolStripMenuItemText,
            Texts.ShowUpgradeAutoSignoutPendingPopup,
            new[]
            {
                Texts.ShowUpgradeAutoSignoutPopup,
                Texts.ShowUpgradeAutoEncryptOpenedPopup,
                Texts.ShowUpgradeProtectSensitivePopup,
                Texts.ShowUpgradeEnhancedSecurityPopup,
            });
    }
}
