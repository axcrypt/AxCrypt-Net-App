using AxCrypt.App.Shared.Utility;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Service;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.ViewModels;

public class AccountSetupViewModel : ViewModelBase
{
    public AccountSetupViewModel() { }

    public async Task ShowAccountIncompleteWarningDialog(LogOnIdentity logOnIdentity)
    {
        _logOnIdentity = logOnIdentity;
        PopupResult = DialogResult.None;
        IsVisible = true;

        while (PopupResult == DialogResult.None)
        {
            await Task.Delay(1000);
        }

        IsVisible = false;
    }

    private static bool _isVisible;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            OnAccountSetupDialogVisibilityChanged?.Invoke(_isVisible);
        }
    }

    public DialogResult PopupResult
    {
        get { return GetProperty<DialogResult>(nameof(PopupResult)); }
        set { SetProperty(nameof(PopupResult), value); }
    }

    public event Action<bool>? OnAccountSetupDialogVisibilityChanged;

    private LogOnIdentity? _logOnIdentity { get; set; }

    public async Task SetViewerPlanAsync()
    {
        IAccountService accountService = New<LogOnIdentity, IAccountService>(_logOnIdentity);
        await accountService.SetMyAccountViewerPlanAsync();
    }
}
