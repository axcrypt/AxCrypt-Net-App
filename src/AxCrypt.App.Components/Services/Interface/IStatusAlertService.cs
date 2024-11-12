using AxCrypt.App.Components.Utility;

namespace AxCrypt.App.Components.Services.Interface;

public interface IStatusAlertService
{
    event Action<bool> OnPopupVisibilityChanged;

    string Alert { get; }

    NotificationType Type { get; }

    void Success(string alert);

    void Error(string alert);

    void Hide();
}
