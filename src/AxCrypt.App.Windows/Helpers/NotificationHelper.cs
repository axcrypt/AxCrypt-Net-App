using AxCrypt.Abstractions;
using AxCrypt.Api.Model.Notification;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.UI;
using AxCrypt.Core.Service.UserNotification;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.Helpers;

public class NotificationApiHelper
{
    public static async Task<IEnumerable<UserNotificationApiModel>> GetNotificationAsync(string useremail, string subslevel)
    {
        LogOnIdentity logOnIdentity = New<KnownIdentities>().DefaultEncryptionIdentity;
        try
        {
            return await New<LogOnIdentity, INotificationService>(logOnIdentity).GetAllUserNotificationAsync(useremail, subslevel);
        }
        catch (Exception ex)
        {
            New<IReport>().Exception(ex);
            if (New<AxCryptOnlineState>().IsOffline)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, Texts.NoInternetErrorMessage);
            }
            return new List<UserNotificationApiModel>();
        }
    }

    public static async Task<bool> DeleteNotificationAsync(long id)
    {
        LogOnIdentity logOnIdentity = New<AxCrypt.Core.UI.KnownIdentities>().DefaultEncryptionIdentity;
        try
        {
            return await New<LogOnIdentity, INotificationService>(logOnIdentity).DeleteAsync(id);
        }
        catch (Exception ex)
        {
            New<IReport>().Exception(ex);
            if (New<AxCryptOnlineState>().IsOffline)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, Texts.NoInternetErrorMessage);
            }
            return false;
        }
    }
}
