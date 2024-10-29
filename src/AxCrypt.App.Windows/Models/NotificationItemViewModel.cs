using AxCrypt.Api.Model.Notification;
using AxCrypt.Core.UI.ViewModel;

namespace AxCrypt.App.Windows.Models;

public class NotificationItemViewModel : ViewModelBase
{
    public NotificationItemViewModel(UserNotificationApiModel model)
    {
        Id = model.Id;
        Title = "";
        Content = model.Content;
        CreatedDate = GetDateString(model.CreatedUtc);
        EventType = model.EventType;
        PushNotif_Title = model.PushNotif_Title;
        PushNotify = model.PushNotify;
    }

    public string Title
    {
        get { return GetProperty<string>(nameof(Title)); }
        private set { SetProperty(nameof(Title), value); }
    }

    public string PushNotif_Title
    {
        get { return GetProperty<string>(nameof(PushNotif_Title)); }
        private set { SetProperty(nameof(PushNotif_Title), value); }
    }

    public bool PushNotify { get; private set; }

    public string Content
    {
        get { return GetProperty<string>(nameof(Content)); }
        private set { SetProperty(nameof(Content), value); }
    }

    public string CreatedDate
    {
        get { return GetProperty<string>(nameof(CreatedDate)); }
        private set { SetProperty(nameof(CreatedDate), value); }
    }

    public string Image
    {
        get { return GetProperty<string>(nameof(Image)); }
        private set { SetProperty(nameof(Image), value); }
    }

    public string PushNotifTitle
    {
        get { return GetProperty<string>(nameof(PushNotifTitle)); }
        private set { SetProperty(nameof(PushNotifTitle), value); }
    }

    public long Id { get; set; }

    public string EventType
    {
        get { return GetProperty<string>(nameof(EventType)); }
        private set { SetProperty(nameof(EventType), value); }
    }

    private string GetDateString(DateTime createdUtc)
    {
        if (createdUtc.Date.Equals(AxCrypt.Abstractions.TypeResolve.New<AxCrypt.Abstractions.INow>().Utc.Date))
        {
            return "Today";
        }
        if (createdUtc.Date.Equals(AxCrypt.Abstractions.TypeResolve.New<AxCrypt.Abstractions.INow>().Utc.AddDays(-1).Date))
        {
            return "Yesterday";
        }
        return createdUtc.ToString("MMMM dd, yyyy");
    }
}
