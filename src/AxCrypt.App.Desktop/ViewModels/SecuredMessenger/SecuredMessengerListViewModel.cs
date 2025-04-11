using AxCrypt.Abstractions;
using AxCrypt.Api.SecuredMessenger;
using AxCrypt.App.Desktop.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Desktop.ViewModels.SecuredMessenger
{
    public class SecuredMessengerListViewModel : ViewModelBase
    {
        private ISecureMessagingService _messageService;
        private IStatusAlertService _statusAlertService;

        public SecuredMessengerListViewModel(ISecureMessagingService messageService, NewSecMsgrViewModel newSecMsgrViewModel, IStatusAlertService statusAlertService)
        {
            _messageService = messageService;
            NewSecMsgrViewModel = newSecMsgrViewModel;
            _statusAlertService = statusAlertService;
        }

        public SecuredMessengerModel Messenger { get; set; } = new SecuredMessengerModel();

        public NewSecMsgrViewModel NewSecMsgrViewModel { get; set; }

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

                UpdateViewState();
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

        public void ComposeNewMessage()
        {
            if (!New<AxCryptOnlineState>().IsOnline)
            {
                _statusAlertService.Error(Texts.NoInternetErrorMessage);
                return;
            }

            NewSecMsgrViewModel.Initialize();
            NewSecMsgrViewModel.IsVisible = true;
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
            UpdateViewState();
        }

        public async Task Read(string messengerId)
        {
            if (string.IsNullOrEmpty(messengerId))
            {
                Console.WriteLine("Messenger ID is empty.");
                return;
            }

            await _messageService.SetReadMessageStatusAsync(messengerId);
            UpdateViewState();
        }

        public async Task Unread(string messengerId)
        {
            if (string.IsNullOrEmpty(messengerId))
            {
                Console.WriteLine("Messenger ID is empty.");
                return;
            }

            await _messageService.SetUnreadMessageStatusAsync(messengerId);
            UpdateViewState();
        }
    }
}