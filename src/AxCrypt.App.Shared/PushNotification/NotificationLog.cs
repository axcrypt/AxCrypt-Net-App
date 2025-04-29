using AxCrypt.Api.Model.Notification;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Common;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.PushNotification;

public class NotificationLog
{
    private Func<Task<bool>> _asyncLogger;

    private static string _message = string.Empty;

    private static IEnumerable<string> _receivers = new List<string>();

    private static string _actor = string.Empty;

    private static string _actionData = string.Empty;

    private static bool _pushNotify = false;

    private static NotificationType _eventType = NotificationType.None;

    public NotificationLog(Func<Task<bool>> asyncLogger)
    {
        _asyncLogger = asyncLogger;
    }

    public static NotificationLog Events(NotificationLogMode notifyLogMode)
    {
        if (notifyLogMode == NotificationLogMode.Enabled)
        {
            return new NotificationLog(async () => await NotificationToDBAsync());
        }

        return new NotificationLog(() => Task.FromResult(default(bool)));
    }

    private static async Task<bool> NotificationToDBAsync()
    {
        IList<NotificationApiModel> notificationApiModels = new List<NotificationApiModel>();
        for (int i = 0; i < _receivers.Count(); i++)
        {
            string receiver = _receivers.ElementAt(i);
            NotificationApiModel notification = new NotificationApiModel(0, receiver, _actor, _message, _eventType.ToString(), New<Abstractions.INow>().Utc, New<Abstractions.INow>().Utc, DateTime.MinValue);
            notification.ActionData = _actionData;
            notification.PushNotify = _pushNotify;

            notificationApiModels.Add(notification);
        }

        bool inserted = await NotificationApiHelper.InsertNotificationAsync(notificationApiModels);
        return inserted;
    }

    public NotificationLog Receiver(string[] receiver)
    {
        _receivers = receiver;
        return this;
    }

    public NotificationLog Actor(string actor)
    {
        _actor = actor;
        return this;
    }

    public NotificationLog Message(string message)
    {
        _message = message;
        return this;
    }

    public NotificationLog EventType(NotificationType eventType)
    {
        _eventType = eventType;
        return this;
    }

    public NotificationLog ActionData(string actionData)
    {
        _actionData = actionData;
        return this;
    }

    public NotificationLog PushNotify(bool pushNotify)
    {
        _pushNotify = pushNotify;
        return this;
    }

    public void Post()
    {
        try
        {
            if (New<AxCryptOnlineState>().IsOffline)
            {
                return;
            }

            Task.Run(async () => await _asyncLogger());
        }
        catch (Exception hex)
        {
            throw new Exception(hex.Message);
        }
    }
}
