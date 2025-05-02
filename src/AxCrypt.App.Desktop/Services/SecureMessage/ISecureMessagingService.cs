using AxCrypt.Api.SecuredMessenger;
using AxCrypt.App.Desktop.ViewModels.SecuredMessenger;
using AxCrypt.Core.SecuredMessenger;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AxCrypt.App.Desktop.Services
{
    public interface ISecureMessagingService
    {
        Task<SecuredMessengerModel> GetListForInboxAsync(string keyword = "");

        Task<SecuredMessengerModel> GetListForUnreadAsync(string keyword = "");

        Task<SecuredMessengerModel> GetListForSentAsync(string keyword = "");

        Task<SecuredMessengerModel> GetLoadMoreSEMAsync(int pageNo, SecureMsgrFilterTab securedMessengerFilterTab);

        Task<IList<SecuredMessage>> ViewMessageWithRepliesAsync(Guid id, SecureMsgrFilterTab SecMessengerFilterTab);

        Task<SecuredMessage> GetMessageByIdAsync(Guid id);

        Task<bool> SetReadMessageStatusAsync(IEnumerable<Guid> selectedMessengerList);

        Task<bool> SetUnreadMessageStatusAsync(IEnumerable<Guid> selectedMessengerList);

        Task<bool> SentMessageAsync(NewSecMsgrViewModel model);

        Task<bool> DeleteMessagesByIds(IEnumerable<Guid> ids, SecureMsgrFilterTab securedMessengerFilter);

        Task<SecuredMessengerModel> GetSecMsgSearchFilterAsync(string keyword, SecuredMessengerModel messengerListViewModel);
    }
}