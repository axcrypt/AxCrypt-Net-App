using AxCrypt.Api.SecuredMessenger;
using AxCrypt.App.Desktop.Services;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AxCrypt.App.Desktop.ViewModels.SecuredMessenger
{
    public class SecuredMessengerListViewModel : ViewModelBase
    {
        private ISecureMessagingService _messageService;

        public SecuredMessengerListViewModel(ISecureMessagingService messageService)
        {
            _messageService = messageService;
        }

        public SecuredMessengerModel Messenger { get; set; } = new SecuredMessengerModel();

        /// <summary>
        /// Loads messages based on the current SecMessengerFilterTab.
        /// </summary>
        public async Task GetMessagesList(SecureMsgrFilterTab secMessengerFilterTab)
        {
            using (ProcessIndicator processIndicator = new ProcessIndicator())
            {
                switch (secMessengerFilterTab)
                {
                    case SecureMsgrFilterTab.Inbox:
                        Messenger = await _messageService.GetModelForList();
                        break;

                    case SecureMsgrFilterTab.Sent:
                        Messenger = await _messageService.GetModelSentList();
                        break;

                    case SecureMsgrFilterTab.Unread:
                        Messenger = await _messageService.GetModelForUnreadList();
                        break;

                    default:
                        break;
                }
            }
        }

        public async Task SearchList(SecuredMessengerModel messengerModel)
        {
            if (messengerModel == null || string.IsNullOrWhiteSpace(messengerModel.Keyword))
            {
                await GetMessagesList(messengerModel.SecMessengerFilterTab);
                return;
            }

            Messenger = await _messageService.GetSecMsgSearchFilterAsync(messengerModel.Keyword, messengerModel);
        }

        public async Task<SecuredMessengerModel> LoadMore(int pageNumber, SecureMsgrFilterTab secMessengerFilterTab)
        {
            return await _messageService.GetLoadMoreSEMAsync(pageNumber, secMessengerFilterTab);
        }

        public async Task DeleteSelection(string messengerId, SecureMsgrFilterTab secMessengerFilterTab)
        {
            if (string.IsNullOrEmpty(messengerId))
            {
                Console.WriteLine("Messenger ID is empty.");
                return;
            }
            IEnumerable<Guid> selectedMessengerList = messengerId.Split(',').Select(mid => new Guid(mid));

            if (!selectedMessengerList.Any())
            {
                Console.WriteLine("No valid GUIDs found.");
                return;
            }

            await _messageService.DeleteMessagesByIds(selectedMessengerList, secMessengerFilterTab);
        }

        public async Task Read(string messengerId)
        {
            if (string.IsNullOrEmpty(messengerId))
            {
                Console.WriteLine("Messenger ID is empty.");
                return;
            }

            await _messageService.SetReadMessageStatusAsync(messengerId);
        }

        public async Task Unread(string messengerId)
        {
            if (string.IsNullOrEmpty(messengerId))
            {
                Console.WriteLine("Messenger ID is empty.");
                return;
            }

            await _messageService.SetUnreadMessageStatusAsync(messengerId);
        }
    }
}