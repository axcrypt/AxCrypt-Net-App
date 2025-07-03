namespace AxCrypt.Core.Notification;

public class NotificationLogger
{
    public static Task LogAsync(NotificationType eventType, string message, string user)
    {
        return LogAsync(user, eventType, message, new[] { user });
    }

    public static Task PushAsync(string actor, NotificationType eventType, string message, string receiver, string actionData)
    {
        return PushAsync(actor, eventType, message, new[] { receiver }, actionData);
    }

    public static Task PushAsync(string actor, NotificationType eventType, string message, string[] receiver, string actionData)
    {
        return LogAsync(actor, eventType, message, receiver, actionData, true);
    }

    public static Task LogAsync(string actor, NotificationType eventType, string message, string[] user, string actionData = "", bool pushNotify = false)
    {
        NotificationLog notificationLog = NotificationLog.Events(NotificationLogMode.Enabled)
                .Actor(actor)
                .Receiver(user)
                .EventType(eventType)
                .Message(message)
                .ActionData(actionData)
                .PushNotify(pushNotify);

        notificationLog.Post();
        return Task.FromResult(default(object));
    }
}
