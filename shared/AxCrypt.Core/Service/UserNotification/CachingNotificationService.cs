using AxCrypt.Api.Model.Notification;
using AxCrypt.Common;
using AxCrypt.Core.Crypto;

namespace AxCrypt.Core.Service.UserNotification
{
    public class CachingNotificationService : INotificationService
    {
        private INotificationService _service;

        public CachingNotificationService(INotificationService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            _service = service;
        }

        public LogOnIdentity Identity => throw new NotImplementedException();

        public async Task<IEnumerable<UserNotificationApiModel>> GetAllUserNotificationAsync(string useremail, string subslevel)
        {
            return await _service.GetAllUserNotificationAsync(useremail, subslevel).Free();
        }

        public async Task<bool> InsertUserNotificationAsync(IEnumerable<NotificationApiModel> notificationModel)
        {
            return await _service.InsertUserNotificationAsync(notificationModel).Free();
        }

        public INotificationService Refresh()
        {
            return this;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            return await _service.DeleteAsync(id).Free();
        }
    }
}