using System;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.Code;

public class NotifyIconGlobalNotification : IGlobalNotification
{
    public NotifyIconGlobalNotification()
    {

    }

    public void ShowTransient(string title, string text)
    {
        New<INotificationService>()?.ShowNotification(title, text);
    }
}