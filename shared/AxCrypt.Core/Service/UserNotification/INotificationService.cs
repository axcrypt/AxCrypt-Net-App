using AxCrypt.Api.Model.Notification;
using AxCrypt.Core.Crypto;

namespace AxCrypt.Core.Service.UserNotification
{
    public interface INotificationService
    {
        INotificationService Refresh();

        LogOnIdentity Identity { get; }

        Task<IEnumerable<UserNotificationApiModel>> GetAllUserNotificationAsync(string useremail, string subslevel);
        
        Task<bool> InsertUserNotificationAsync(IEnumerable<NotificationApiModel> notificationModel);

        Task<bool> DeleteAsync(long id);
    }
}