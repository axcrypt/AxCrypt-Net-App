using AxCrypt.Abstractions;
using AxCrypt.Api.Model.SecuredMessenger;
using AxCrypt.Api.SecuredMessenger;
using AxCrypt.Api.Shared.Helper;
using AxCrypt.App.Desktop.ViewModels;
using AxCrypt.App.Desktop.ViewModels.SecuredMessenger;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.SecuredMessenger;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Desktop.Services
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

        public async Task<SecuredMessengerModel> GetModelForList(string keyword = "")
        {
            SecuredMessengerModel model = await InboxSecuredMesssengerFrom(_pageNumber);
            model.Keyword = keyword;

            return model;
        }

        public async Task<SecuredMessengerModel> GetModelForUnreadList(string keyword = "")
        {
            SecuredMessengerModel model = await UnreadSecuredMessengerFrom(_pageNumber);
            model.Keyword = keyword;

            return model;
        }

        public async Task<SecuredMessengerModel> GetModelSentList(string keyword = "")
        {
            SecuredMessengerModel model = await SentSecuredMessengerFrom(_pageNumber);
            model.Keyword = keyword;

            return model;
        }

        public async Task<SecuredMessengerModel> GetModelForViewMessage(Guid messageId, SecureMsgrFilterTab SecMessengerFilterTab)
        {
            SecuredMessengerModel model = new SecuredMessengerModel();
            IEnumerable<SecuredMessage> replies = await SecuredMessengerFacade.GetMessageRepliesAsync(messageId);
            if (replies == null)
            {
                model.ErrorMessage = "Failed to fetch the root message.";
                return model;
            }

            model.Messages = replies;

            if (SecMessengerFilterTab == SecureMsgrFilterTab.Sent)
            {
                return model;
            }

            await UpdateVisibilityStatusAsync(model);
            return model;
        }

        public async Task<SecuredMessage> GetMessageByIdAsync(Guid messageId)
        {
            if (messageId == null)
            {
                return null;
            }

            return await SecuredMessengerFacade.GetMessageAsync(messageId);
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
                //_statusAlertService.Error(Texts.SendSecuredMessageFailure);
                _statusAlertService.Error("Maximum count reached to send a secured message. Please upgrade your subscription!");
                return false;
            }

            viewModel.ReceiverList = ReceiverListFrom(viewModel).ToList();

            int maxSendUserCount = SecMessengerUtility.MaxSendUserCount(_logOnViewModel.SubscriptionLevel);
            if (viewModel.ReceiverList.Count() > maxSendUserCount)
            {
                //StatusAlertService.Error(Texts.SendSecuredMessageFailure);
                _statusAlertService.Error($"Maximum recipients count{maxSendUserCount} reached to send a secured message. Please upgrade your subscription!");
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

            // await NotificationLogger.PushAsync(New<KnownIdentities>().DefaultEncryptionIdentity.UserEmail.Address, NotificationType.SecuredMessageSent, Texts.SentSecuredMessageNotificationText, viewModel.ReceiverList.Select(sem => sem.EmailAddress).ToArray(), null);
            return true;
        }

        public async Task<bool> SetReadMessageStatusAsync(string messengerId)
        {
            IEnumerable<Guid> selectedMessengerList = messengerId.Split(',').Select(mid => new Guid(mid));
            if (selectedMessengerList == null)
            {
                return false;
            }

            return await New<LogOnIdentity, Core.Service.SecuredMessenger.ISecuredMessengerService>(Identity()).UpdateAsync(selectedMessengerList, New<KnownIdentities>().DefaultEncryptionIdentity.UserEmail.Address);
        }

        public async Task<bool> SetUnreadMessageStatusAsync(string messengerId)
        {
            IEnumerable<Guid> selectedMessengerList = messengerId.Split(',').Select(mid => new Guid(mid));
            if (selectedMessengerList == null)
            {
                return false;
            }

            return await New<LogOnIdentity, Core.Service.SecuredMessenger.ISecuredMessengerService>(Identity()).UpdateAsync(selectedMessengerList, New<KnownIdentities>().DefaultEncryptionIdentity.UserEmail.Address, true);
        }

        public async Task<bool> DeleteMessagesByIds(IEnumerable<Guid> ids, SecureMsgrFilterTab securedMessengerFilter)
        {
            string userName = New<KnownIdentities>().DefaultEncryptionIdentity.UserEmail.Address;
            return await New<LogOnIdentity, Core.Service.SecuredMessenger.ISecuredMessengerService>(Identity()).DeleteAsync(ids, userName, securedMessengerFilter);
        }

        public async Task<SecuredMessengerModel> GetLoadMoreSEMAsync(int pageNo, SecureMsgrFilterTab securedMessengerFilter)
        {
            SecuredMessengerModel model = new SecuredMessengerModel();
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
            if (messengerListViewModel.SecMsgSearchFilters == SecureMsgrSearchFilters.None)
            {
                return messengerListViewModel!;
            }

            IEnumerable<SecuredMessage> messages = await SecuredMessengerFacade.GetSecMsgSearchFilterAsync(messengerListViewModel.SecMessengerFilterTab, messengerListViewModel.SecMsgSearchFilters);

            keyword = keyword.Trim().ToLower();
            IEnumerable<SecuredMessage> filteredMessages = messages
                .Where(m => (m.Message.ReceiverList.Any(rl => rl.User.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)) ||
                            (m.Message.Username?.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (m.Message.TheMessage?.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0));

            messengerListViewModel.Messages = filteredMessages;

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
            return new SecuredMessengerModel
            {
                Messages = messages,
                SecMessengerFilterTab = selectedTab
            };
        }

        private static IEnumerable<MessengerReceiverViewModel> ReceiverListFrom(NewSecMsgrViewModel viewModel)
        {
            IList<MessengerReceiverViewModel> messengerReceiverViewModels = new List<MessengerReceiverViewModel>();
            IEnumerable<string> receiversList = viewModel.ReceiverEmails.Split(',').Distinct();
            foreach (string receiver in receiversList)
            {
                EmailAddress receiverEmail;
                if (!EmailAddress.TryParse(receiver, out receiverEmail))
                {
                    continue;
                }

                messengerReceiverViewModels.Add(new MessengerReceiverViewModel { EmailAddress = receiverEmail.Address });
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

        private static async Task UpdateVisibilityStatusAsync(SecuredMessengerModel model)
        {
            foreach (SecuredMessage msg in model.Messages)
            {
                bool updateRead = msg.Message.ReceiverList.Any(mr => mr.User == New<KnownIdentities>().DefaultEncryptionIdentity.UserEmail.Address && mr.Read == DateTime.MinValue);
                if (!updateRead)
                {
                    continue;
                }

                IEnumerable<Guid> messageIds = new List<Guid>() { msg.Id };
                await New<LogOnIdentity, Core.Service.SecuredMessenger.ISecuredMessengerService>(Identity()).UpdateAsync(messageIds, New<KnownIdentities>().DefaultEncryptionIdentity.UserEmail.Address);
            }
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
                    return currentDateTime.AddDays(1).Date;

                case SecureMsgrVisibility.OneWeek:
                    return currentDateTime.AddDays(7).Date;

                case SecureMsgrVisibility.OneMonth:
                    return currentDateTime.AddMonths(1).Date;

                case SecureMsgrVisibility.OneYear:
                    return currentDateTime.AddYears(1).Date;

                default:
                    throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null);
            }
        }

        #endregion PrivateHelpers
    }
}