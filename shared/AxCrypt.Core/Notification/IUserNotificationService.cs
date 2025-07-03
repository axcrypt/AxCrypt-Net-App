using System;
using AxCrypt.Api.Model.Notification;

namespace AxCrypt.Core.Notification
{
	public interface IUserNotificationService
	{
		public Task<IEnumerable<UserNotificationApiModel>> GetNotificationAsync(string useremail, string subslevel);

		Task<bool> InsertNotificationAsync(IEnumerable<NotificationApiModel> notificationModel);

		Task<bool> DeleteNotificationAsync(long id);
    }
}

