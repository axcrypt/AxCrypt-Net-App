// Services/PopupService.cs — global popup/modal state, single source of truth.
// Inject as a scoped service to open any app-level overlay from anywhere.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AxCrypt.App.Shared.Desktop.Services;

/// <summary>
/// Global popup/modal state service. Every open/close call goes through here
/// so only one overlay is ever visible at a time.
/// </summary>
public class PopupService
{
    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    // ── Feature-gate modal ─────────────────────────────────────────────
    public bool PaidGateOpen { get; private set; }

    public void ShowPaidGate()
    {
        CloseAllPopups();
        PaidGateOpen = true;
        Notify();
    }

    public void ClosePaidGate()
    {
        PaidGateOpen = false;
        Notify();
    }

    // ── Help overlay ───────────────────────────────────────────────────
    public bool HelpOpen { get; private set; }
    public string HelpInitTab { get; private set; } = "start";

    public void ShowHelp(string tab = "start")
    {
        CloseAllPopups();
        HelpInitTab = tab;
        HelpOpen = true;
        Notify();
    }

    // ── Advanced Settings / Encryption Properties ──────────────────────
    public bool AdvancedSettingsOpen { get; private set; }
    public string AdvancedSettingsInitTab { get; private set; } = "Vault";
    public bool EncryptionFilePropertiesOpen { get; private set; }

    public void ShowAdvancedSettings(string tab = "Vault")
    {
        CloseAllPopups();
        AdvancedSettingsInitTab = tab;
        AdvancedSettingsOpen = true;
        Notify();
    }

    public void ShowEncryptionFileProperties()
    {
        CloseAllPopups();
        EncryptionFilePropertiesOpen = true;
        Notify();
    }

    // ── Toast notifications ────────────────────────────────────────────
    private readonly List<ToastMessage> _toasts = new();
    public IReadOnlyList<ToastMessage> Toasts => _toasts.AsReadOnly();

    public void ShowToast(string message, ToastType type = ToastType.Info, int durationMs = 3000)
    {
        ToastMessage toast = new ToastMessage(Guid.NewGuid(), message, type, durationMs);
        _toasts.Add(toast);
        Notify();
        Task.Delay(durationMs).ContinueWith(_ => { _toasts.Remove(toast); Notify(); });
    }

    // ── Transient menus (profile, settings, action, context) ──────────
    public bool ProfileMenuOpen    { get; set; }
    public bool SettingsMenuOpen   { get; set; }
    public bool NewActionMenuOpen  { get; set; }
    public bool ContextMenuOpen    { get; set; }
    public bool SwitchUserPopupOpen { get; set; }
    public bool HelpOverlayOpen    { get; set; }
    public bool SidebarCollapsed   { get; set; }

    public double ContextMenuX { get; set; }
    public double ContextMenuY { get; set; }

    // ── Global state query ─────────────────────────────────────────────
    /// <summary>True when any overlay or menu is open — used to show the global click-outside scrim.</summary>
    public bool AnyPopupOpen =>
        ProfileMenuOpen || SettingsMenuOpen || NewActionMenuOpen || ContextMenuOpen ||
        PaidGateOpen || HelpOpen || HelpOverlayOpen ||
        AdvancedSettingsOpen || EncryptionFilePropertiesOpen || SwitchUserPopupOpen;

    // ── Global close ───────────────────────────────────────────────────
    /// <summary>Close every overlay and menu, then notify subscribers.</summary>
    public void CloseAllPopups()
    {
        ProfileMenuOpen = SettingsMenuOpen = NewActionMenuOpen = ContextMenuOpen =
        PaidGateOpen = HelpOpen = HelpOverlayOpen =
        AdvancedSettingsOpen = EncryptionFilePropertiesOpen = SwitchUserPopupOpen = false;
        Notify();
    }

    // ── Toggle helpers ─────────────────────────────────────────────────
    public void ToggleSidebar()    => SidebarCollapsed = !SidebarCollapsed;

    public void ToggleProfileMenu()
    { bool was = ProfileMenuOpen; CloseAllPopups(); ProfileMenuOpen = !was; Notify(); }

    public void ToggleNewActionMenu()
    { bool was = NewActionMenuOpen; CloseAllPopups(); NewActionMenuOpen = !was; Notify(); }

    public void ToggleSettingsMenu()
    { bool was = SettingsMenuOpen; CloseAllPopups(); SettingsMenuOpen = !was; Notify(); }

    /// <summary>
    /// Opens the Settings menu unconditionally and notifies.
    /// Use this instead of ToggleSettingsMenu when you need to guarantee
    /// the menu is open (e.g. triggered from outside the TopBar).
    /// </summary>
    public void OpenSettingsMenu()
    { CloseAllPopups(); SettingsMenuOpen = true; Notify(); }

    public void ToggleRecentFilesContextMenu()
    { bool was = ContextMenuOpen; CloseAllPopups(); ContextMenuOpen = !was; Notify(); }

    public void ToggleSwitchUserPopupOpen()
    { bool was = SwitchUserPopupOpen; CloseAllPopups(); SwitchUserPopupOpen = !was; Notify(); }
}

public record ToastMessage(Guid Id, string Message, ToastType Type, int DurationMs);

public enum ToastType { Success, Error, Warning, Info }
