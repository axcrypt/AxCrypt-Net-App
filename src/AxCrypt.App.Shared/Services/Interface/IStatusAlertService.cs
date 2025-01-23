using AxCrypt.App.Shared.Utility;

namespace AxCrypt.App.Shared.Services.Interface;

public interface IStatusAlertService
{
    event Action<bool> OnPopupVisibilityChanged;

    string Alert { get; }

    NotificationType Type { get; }

    void Success(string alert);

    void Error(string alert);

    void Hide();
}