using AxCrypt.App.Shared.Utility;
using AxCrypt.Core.UI.ViewModel;

namespace AxCrypt.App.Shared.ViewModels;

public class AccountSetupViewModel : ViewModelBase
{
    public AccountSetupViewModel() { }

    public async Task ShowAccountIncompleteWarningDialog()
    {
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
}
