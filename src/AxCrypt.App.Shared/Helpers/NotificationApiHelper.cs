using AxCrypt.Abstractions;
using AxCrypt.Api.Model.Notification;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Service.UserNotification;
using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Helpers;

public class NotificationApiHelper
{
    public static async Task<IEnumerable<UserNotificationApiModel>> GetNotificationAsync(string useremail, string subslevel)
    {
        LogOnIdentity logOnIdentity = New<AxCrypt.Core.UI.KnownIdentities>().DefaultEncryptionIdentity;
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

    public static async Task<bool> InsertNotificationAsync(IEnumerable<NotificationApiModel> notificationModel)
    {
        LogOnIdentity logOnIdentity = New<KnownIdentities>().DefaultEncryptionIdentity;

        try
        {
            return await New<LogOnIdentity, INotificationService>(logOnIdentity).InsertUserNotificationAsync(notificationModel);
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