namespace AxCrypt.App.Shared.Services.UI;

public class CommonDialogService
{
    public event Action<bool>? OnDialogVisibilityChanged;

    private bool _isDialogVisible;

    private bool IsDialogVisible
    {
        get => _isDialogVisible;
        set
        {
            _isDialogVisible = value;
            OnDialogVisibilityChanged?.Invoke(_isDialogVisible);
        }
    }

    public void Show()
    {
        IsDialogVisible = true;
    }

    public void Close()
    {
        IsDialogVisible = false;
    }
}