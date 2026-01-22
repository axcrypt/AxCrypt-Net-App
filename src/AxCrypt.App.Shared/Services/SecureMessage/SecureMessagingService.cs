using AxCrypt.Abstractions;
using AxCrypt.Api;
using AxCrypt.Api.Model.SecuredMessenger;
using AxCrypt.Api.SecuredMessenger;
using AxCrypt.Api.Shared.Helper;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.App.Shared.ViewModels.SecuredMessenger;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.SecuredMessenger;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.Core.Notification;

namespace AxCrypt.App.Shared.Services
{
    public class SecureMessagingService : ISecureMessagingService
    {
        private int _pageNumber = 0;

        private LogOnViewModel _logOnViewModel;
        private IStatusAlertService? _statusAlertService;

        public SecureMessagingService(LogOnViewModel logOnViewModel, IStatusAlertService? statusAlertService)
        {
            _logOnViewModel = logOnViewModel;
            _statusAlertService = statusAlertService;
        }

        public async Task<SecuredMessengerModel> GetListForInboxAsync(string keyword = "")
        {
            SecuredMessengerModel model = await InboxSecuredMesssengerFrom(_pageNumber);
            model.Keyword = keyword;

            return model;
        }

        public async Task<SecuredMessengerModel> GetListForUnreadAsync(string keyword = "")
        {
            SecuredMessengerModel model = await UnreadSecuredMessengerFrom(_pageNumber);
            model.Keyword = keyword;

            return model;
        }

        public async Task<SecuredMessengerModel> GetListForSentAsync(string keyword = "")
        {
            SecuredMessengerModel model = await SentSecuredMessengerFrom(_pageNumber);
            model.Keyword = keyword;

            return model;
        }

        public async Task<IList<SecuredMessage>> ViewMessageWithRepliesAsync(Guid messageId, SecureMsgrFilterTab secMessengerFilterTab)
        {
            IEnumerable<SecuredMessage> replies = await SecuredMessengerFacade.GetMessageRepliesAsync(messageId, secMessengerFilterTab);
            if (replies == null)
            {
                return null;
            }

            if (secMessengerFilterTab == SecureMsgrFilterTab.Sent)
            {
                return replies.ToList();
            }

            Task updateStatustask = new Task(async () => await UpdateVisibilityStatusAsync(replies.Where(r => r.Id == messageId)));
            updateStatustask.Start();

            return replies.ToList();
        }

        public async Task<SecuredMessage> GetMessageByIdAsync(Guid messageId)
        {
            if (messageId == Guid.Empty)
            {
                return null;
            }

            SecuredMessage reply = await SecuredMessengerFacade.GetMessageAsync(messageId);

            Task updateStatustask = new Task(async () => await UpdateVisibilityStatusAsync(new List<SecuredMessage> { reply }));
            updateStatustask.Start();

            return reply;
        }

        public async Task<bool> SentMessageAsync(NewSecMsgrViewModel viewModel)
        {
            if (viewModel == null)
            {
                return false;
            }

            bool allowToAdd = SecMessengerUtility.AllowAddNewMessage(_logOnViewModel.SubscriptionLevel);
            if (!allowToAdd)
            {
                _statusAlertService!.Error("Maximum count reached to send a secured message. Please upgrade your subscription!");
                return false;
            }

            viewModel.ReceiverList = ReceiverListFrom(viewModel).ToList();

            int maxSendUserCount = SecMessengerUtility.MaxSendUserCount(_logOnViewModel.SubscriptionLevel);
            if (viewModel.ReceiverList.Count() > maxSendUserCount)
            {
                //StatusAlertService.Error(Texts.SendSecuredMessageFailure);
                _statusAlertService.Error($"Maximum recipients count {maxSendUserCount} reached to send a secured message. Please upgrade your subscription!");
                return false;
            }

            SecuredMessengerApiModel messengerApiModel = MessengerApiModelFrom(viewModel);
            bool saved = await SecuredMessengerFacade.PostMessage(messengerApiModel);
            if (!saved)
            {
                return false;
            }

            if (SecMessengerUtility.CanUpdateFreeUserCount())
            {
                New<LogOnIdentity, AdditionalUserSettings>(New<KnownIdentities>().DefaultEncryptionIdentity).UpdateFreeUserSecuredMessengerLimit();
            }

            return true;
        }

        public async Task<bool> SetReadMessageStatusAsync(IEnumerable<Guid> selectedMessengerList)
        {
            if (selectedMessengerList == null)
            {
                return false;
            }

            return await New<LogOnIdentity, AxCrypt.Core.Service.SecuredMessenger.ISecuredMessengerService>(Identity()).UpdateAsync(selectedMessengerList, New<KnownIdentities>().DefaultEncryptionIdentity.UserEmail.Address);
        }

        public async Task<bool> SetUnreadMessageStatusAsync(IEnumerable<Guid> selectedMessengerList)
        {
            if (selectedMessengerList == null)
            {
                return false;
            }

            return await New<LogOnIdentity, AxCrypt.Core.Service.SecuredMessenger.ISecuredMessengerService>(Identity()).UpdateAsync(selectedMessengerList, New<KnownIdentities>().DefaultEncryptionIdentity.UserEmail.Address, true);
        }

        public async Task<bool> DeleteMessagesByIds(IEnumerable<Guid> ids, SecureMsgrFilterTab securedMessengerFilter)
        {
            string userName = New<KnownIdentities>().DefaultEncryptionIdentity.UserEmail.Address;
            return await New<LogOnIdentity, AxCrypt.Core.Service.SecuredMessenger.ISecuredMessengerService>(Identity()).DeleteAsync(ids, userName, securedMessengerFilter);
        }

        public async Task<SecuredMessengerModel> GetLoadMoreSEMAsync(int pageNo, SecureMsgrFilterTab securedMessengerFilter)
        {
            SecuredMessengerModel model = new SecuredMessengerModel(securedMessengerFilter);
            model.PageNumber = pageNo;
            if (securedMessengerFilter == SecureMsgrFilterTab.Inbox)
            {
                model = await InboxSecuredMesssengerFrom(pageNo);
            }
            else if (securedMessengerFilter == SecureMsgrFilterTab.Sent)
            {
                model = await SentSecuredMessengerFrom(pageNo);
            }
            else
            {
                model = await UnreadSecuredMessengerFrom(pageNo);
            }

            model.PageNumber++;
            return model;
        }

        public async Task<SecuredMessengerModel> GetSecMsgSearchFilterAsync(string keyword, SecuredMessengerModel messengerListViewModel)
        {
            RequestOptions options = new RequestOptions();
            options.UserName = Identity().UserEmail.ToString();
            options.StartDate = messengerListViewModel.StartDate;
            options.EndDate = messengerListViewModel.EndDate;

            IEnumerable<SecuredMessage> messages = await SecuredMessengerFacade.GetSecMsgSearchFilterAsync(messengerListViewModel.SecMessengerFilterTab, options);

            keyword = keyword.Trim().ToLower();
            IEnumerable<SecuredMessage> filteredMessages = messages.Where(m =>
                (string.IsNullOrWhiteSpace(keyword) ||
                    (m.Message.ReceiverList.Any(rl => rl.User.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                     m.Message.Username?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true ||
                     m.Message.TheMessage?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true)) &&

                (string.IsNullOrWhiteSpace(messengerListViewModel.ReceiverName) ||
                    m.Message.ReceiverList.Any(rl => rl.User.Contains(messengerListViewModel.ReceiverName, StringComparison.OrdinalIgnoreCase))) &&

                (string.IsNullOrWhiteSpace(messengerListViewModel.UserName) ||
                    m.Message.Username?.Contains(messengerListViewModel.UserName, StringComparison.OrdinalIgnoreCase) == true)
            );

            messengerListViewModel.Messages = filteredMessages.ToList();

            return messengerListViewModel;
        }

        #region PrivateHelpers

        private static LogOnIdentity Identity()
        {
            return New<KnownIdentities>().DefaultEncryptionIdentity;
        }

        private async Task<SecuredMessengerModel> InboxSecuredMesssengerFrom(int pageNumber)
        {
            IEnumerable<SecuredMessage> messages = await SecuredMessengerFacade.GetInboxMessagesAsync(pageNumber);

            return CreateMessengerListViewModel(messages, SecureMsgrFilterTab.Inbox);
        }

        private async Task<SecuredMessengerModel> SentSecuredMessengerFrom(int pageNumber)
        {
            IEnumerable<SecuredMessage> messages = await SecuredMessengerFacade.GetSentMessagesAsync(pageNumber);

            return CreateMessengerListViewModel(messages, SecureMsgrFilterTab.Sent);
        }

        private async Task<SecuredMessengerModel> UnreadSecuredMessengerFrom(int pageNumber)
        {
            IEnumerable<SecuredMessage> messages = await SecuredMessengerFacade.GetUnreadMessagesAsync(pageNumber);

            return CreateMessengerListViewModel(messages, SecureMsgrFilterTab.Unread);
        }

        private SecuredMessengerModel CreateMessengerListViewModel(IEnumerable<SecuredMessage> messages, SecureMsgrFilterTab selectedTab)
        {
            return new SecuredMessengerModel(selectedTab)
            {
                Messages = messages.ToList(),
            };
        }

        private static IEnumerable<MessengerReceiver> ReceiverListFrom(NewSecMsgrViewModel viewModel)
        {
            IList<MessengerReceiver> messengerReceiverViewModels = new List<MessengerReceiver>();
            IEnumerable<string> receiversList = viewModel.ReceiverEmails.Split(',').Distinct();
            foreach (string receiver in receiversList)
            {
                EmailAddress receiverEmail;
                if (!EmailAddress.TryParse(receiver, out receiverEmail))
                {
                    continue;
                }

                messengerReceiverViewModels.Add(new MessengerReceiver { EmailAddress = receiverEmail.Address });
            }

            return messengerReceiverViewModels;
        }

        private static SecuredMessengerApiModel MessengerApiModelFrom(NewSecMsgrViewModel model)
        {
            model.Id = Guid.NewGuid();
            DateTime visibleUntil = GetVisibleUntil(model.Visibility);
            SecuredMessengerApiModel apiModel = new SecuredMessengerApiModel()
            {
                MessageId = model.Id,
                Sender = New<KnownIdentities>().DefaultEncryptionIdentity.UserEmail.Address,
                Visibility = model.Visibility.ToString(),
                EncryptedMessage = model.EncryptedMessage,
                VisibleUntil = visibleUntil,
                ParentId = model.ParentId,
                Receiver = model.ReceiverList.Select(mr =>
                {
                    return new MessengerReceiverApiModel()
                    {
                        User = mr.EmailAddress,
                    };
                }),
                CreatedUtc = New<Abstractions.INow>().Utc,
                UpdatedUtc = New<Abstractions.INow>().Utc
            };

            return apiModel;
        }

        private static async Task UpdateVisibilityStatusAsync(IEnumerable<SecuredMessage> messages)
        {
            IList<Guid> messageIds = new List<Guid>();
            foreach (SecuredMessage msg in messages)
            {
                bool updateRead = msg.Message.ReceiverList.Any(mr => mr.User == New<KnownIdentities>().DefaultEncryptionIdentity.UserEmail.Address && mr.Read == DateTime.MinValue);
                if (!updateRead)
                {
                    continue;
                }

                messageIds.Add(msg.Id);
            }

            await New<LogOnIdentity, AxCrypt.Core.Service.SecuredMessenger.ISecuredMessengerService>(Identity()).UpdateAsync(messageIds, New<KnownIdentities>().DefaultEncryptionIdentity.UserEmail.Address);
        }

        private static DateTime GetVisibleUntil(SecureMsgrVisibility visibility)
        {
            DateTime currentDateTime = New<INow>().Utc;
            switch (visibility)
            {
                case SecureMsgrVisibility.Once:
                case SecureMsgrVisibility.Forever:
                    return DateTime.MaxValue;

                case SecureMsgrVisibility.OneHour:
                    return currentDateTime.AddHours(1);

                case SecureMsgrVisibility.OneDay:
                    return currentDateTime.AddDays(1);

                case SecureMsgrVisibility.OneWeek:
                    return currentDateTime.AddDays(7);

                case SecureMsgrVisibility.OneMonth:
                    return currentDateTime.AddMonths(1);

                case SecureMsgrVisibility.OneYear:
                    return currentDateTime.AddYears(1);

                default:
                    throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null);
            }
        }

        #endregion PrivateHelpers
    }
}