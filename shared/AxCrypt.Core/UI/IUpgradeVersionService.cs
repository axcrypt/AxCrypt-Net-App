namespace AxCrypt.Core.UI;

public interface IUpgradeVersionService
{
    Task<PopupButtons> ShowDialogAsync(PopupButtons buttons, string title, string message);
}
