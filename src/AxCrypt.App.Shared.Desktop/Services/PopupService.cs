// Services/PopupService.cs
// Inject as scoped service — shared across all pages for global popup state

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AxCrypt.App.Shared.Desktop.Services;

/// <summary>
/// Global popup/modal state service. Inject into any page or component
/// to open paid-gate, help overlay, context menu, or any app-level overlay.
/// </summary>
public class PopupService
{
    // ── Events ─────────────────────────────────────────────────
    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    // ── Paid Gate ──────────────────────────────────────────────
    public bool PaidGateOpen { get; set; }

    public void ShowPaidGate()
    {
        CloseAll();
        PaidGateOpen = true;
        Notify();
    }

    // ── Help Overlay ───────────────────────────────────────────
    public bool HelpOpen { get; private set; }
    public string HelpInitTab { get; private set; } = "start";

    public void ShowHelp(string tab = "start")
    {
        CloseAll();
        HelpInitTab = tab;
        HelpOpen = true;
        Notify();
    }

    // ── Advanced Settings ──────────────────────────────────────
    public bool AdvancedSettingsOpen { get; private set; }
    public string AdvancedSettingsInitTab { get; private set; } = "Vault";

    public bool EncryptionFilePropertiesOpen { get; private set; }

    public void ShowAdvancedSettings(string tab = "Vault")
    {
        CloseAll();
        AdvancedSettingsInitTab = tab;
        AdvancedSettingsOpen = true;
        Notify();
    }

    public void ShowEncryptionFileProperties()
    {
        CloseAll();
        EncryptionFilePropertiesOpen = true;
        Notify();
    }

    // ── Toast Notifications ────────────────────────────────────
    private readonly List<ToastMessage> _toasts = new();
    public IReadOnlyList<ToastMessage> Toasts => _toasts.AsReadOnly();

    public void ShowToast(string message, ToastType type = ToastType.Info, int durationMs = 3000)
    {
        var toast = new ToastMessage(Guid.NewGuid(), message, type, durationMs);
        _toasts.Add(toast);
        Notify();
        Task.Delay(durationMs).ContinueWith(_ => { _toasts.Remove(toast); Notify(); });
    }

    // ── Global Close ──────────────────────────────────────────
    public bool AnyOpen => PaidGateOpen || HelpOpen || AdvancedSettingsOpen;

    public void CloseAll()
    {
        PaidGateOpen = HelpOpen = AdvancedSettingsOpen = EncryptionFilePropertiesOpen = false;
        Notify();
    }

    // ═══════════════════════════════════════════
    //  POPUP / MODAL STATE  — single source of truth
    // ═══════════════════════════════════════════
    public bool ProfileMenuOpen { get; set; }
    public bool SettingsMenuOpen { get; set; }
    public bool NewActionMenuOpen { get; set; }
    public bool ContextMenuOpen { get; set; }
    public bool SwitchUserPopupOpen { get; set; }

    public double ContextMenuX { get; set; }
    public double ContextMenuY { get; set; }

    public bool HelpOverlayOpen { get; set; }
    public bool SidebarCollapsed { get; set; }

    public bool AnyPopupOpen => ProfileMenuOpen || SettingsMenuOpen || NewActionMenuOpen
                               || PaidGateOpen || HelpOverlayOpen || AdvancedSettingsOpen || SwitchUserPopupOpen;

    public void CloseAllPopups()
    {
        ProfileMenuOpen = SettingsMenuOpen = NewActionMenuOpen =
        ContextMenuOpen = PaidGateOpen = HelpOverlayOpen = AdvancedSettingsOpen = SwitchUserPopupOpen = false;
        Notify();
    }

    public void ToggleSidebar() => SidebarCollapsed = !SidebarCollapsed;
    public void ToggleProfileMenu() { bool was = ProfileMenuOpen; CloseAllPopups(); ProfileMenuOpen = !was; }
    public void ToggleNewActionMenu() { bool was = NewActionMenuOpen; CloseAllPopups(); NewActionMenuOpen = !was; }
    public void ToggleSettingsMenu() { bool was = SettingsMenuOpen; CloseAllPopups(); SettingsMenuOpen = !was; }

    /// <summary>
    /// Opens the Settings menu unconditionally and notifies subscribers.
    /// Used by callers outside the TopBar (e.g. global search) that need
    /// the TopBar to re-render and show the popup — ToggleSettingsMenu
    /// alone wouldn't fire OnChange with the final state.
    /// </summary>
    public void OpenSettingsMenu() { CloseAllPopups(); SettingsMenuOpen = true; Notify(); }
    public void ToggleRecentFilesContextMenu() { bool was = ContextMenuOpen; CloseAllPopups(); ContextMenuOpen = !was; Notify(); }

    public void ToggleSwitchUserPopupOpen() { bool was = SwitchUserPopupOpen; CloseAllPopups(); SwitchUserPopupOpen = !was; Notify(); }
    
}

public record ToastMessage(Guid Id, string Message, ToastType Type, int DurationMs);

public enum ToastType { Success, Error, Warning, Info }
