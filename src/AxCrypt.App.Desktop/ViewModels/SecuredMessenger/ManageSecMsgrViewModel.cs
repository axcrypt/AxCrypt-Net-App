using AxCrypt.Abstractions;
using AxCrypt.Api.SecuredMessenger;
using AxCrypt.App.Desktop.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.SecuredMessenger;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Desktop.ViewModels.SecuredMessenger
{
    public class ManageSecMsgrViewModel : ViewModelBase
    {
        private ISecureMessagingService _messageService { get; set; }
        private IStatusAlertService _statusAlertService { get; set; }

        public DateTime IsRead { get; set; } = DateTime.MinValue;

        public ManageSecMsgrViewModel(ISecureMessagingService msgService, IStatusAlertService statusAlertService, NewSecMsgrViewModel newSecMsgrViewModel)
        {
            _messageService = msgService;
            _statusAlertService = statusAlertService;
            NewSecMsgrViewModel = newSecMsgrViewModel;
        }

        public SecuredMessengerModel Messenger { get; set; } = new SecuredMessengerModel(SecureMsgrFilterTab.Inbox);

        public Guid? SelectedMessageId { get; set; } = Guid.Empty;

        public bool AnySelected => Messenger.Messages.Any(m => m.IsSelected);

        private bool _showLoadingWheel;

        public bool ShowLoadingWheel
        {
            get
            {
                return _showLoadingWheel;
            }
            set
            {
                _showLoadingWheel = value;
                UpdateViewState();
            }
        }

        public bool SelectAllMessages { get; set; }

        public NewSecMsgrViewModel NewSecMsgrViewModel { get; set; }

        public async Task GetMessagesList(SecureMsgrFilterTab secMessengerFilterTab)
        {
            using (ProcessIndicator processIndicator = new ProcessIndicator())
            {
                if (secMessengerFilterTab == SecureMsgrFilterTab.None)
                {
                    secMessengerFilterTab = SecureMsgrFilterTab.Inbox;
                }

                switch (secMessengerFilterTab)
                {
                    case SecureMsgrFilterTab.Inbox:
                        Messenger = await _messageService.GetListForInboxAsync();
                        break;

                    case SecureMsgrFilterTab.Sent:
                        Messenger = await _messageService.GetListForSentAsync();
                        break;

                    case SecureMsgrFilterTab.Unread:
                        Messenger = await _messageService.GetListForUnreadAsync();
                        break;

                    default:
                        break;
                }

                UpdateViewState();
            }
        }

        public async Task SetSelectedTabActive(SecureMsgrFilterTab secMessengerFilterTab)
        {
            Messenger = new SecuredMessengerModel(secMessengerFilterTab);
            if (Messenger.ChildMessages != null && Messenger.ChildMessages.Any())
            {
                Messenger.ChildMessages = new List<AxCrypt.Core.SecuredMessenger.SecuredMessage>();
            }

            await GetMessagesList(secMessengerFilterTab);
        }

        public async Task SearchList(SecuredMessengerModel messengerModel, SecureMsgrFilterTab secMessengerFilterTab)
        {
            if (messengerModel == null || string.IsNullOrWhiteSpace(messengerModel.Keyword))
            {
                await GetMessagesList(secMessengerFilterTab);
                return;
            }

            Messenger = await _messageService.GetSecMsgSearchFilterAsync(messengerModel.Keyword, messengerModel);
            UpdateViewState();
        }

        public async Task<SecuredMessengerModel> LoadMore(int pageNumber, SecureMsgrFilterTab secMessengerFilterTab)
        {
            return await _messageService.GetLoadMoreSEMAsync(pageNumber, secMessengerFilterTab);
        }

        public async Task ViewMessageReplies(Guid messageId, SecureMsgrFilterTab securedMessengerFilterTab, MouseEventArgs e)
        {
            if (messageId == Guid.Empty)
            {
                return;
            }

            SelectedMessageId = messageId;

            ResetMessageView();

            ShowLoadingWheel = true;
            IsRead = DateTime.MaxValue;
            Messenger.ChildMessages = await _messageService.ViewMessageWithRepliesAsync(messageId, securedMessengerFilterTab);
            Messenger.SecMessengerFilterTab = securedMessengerFilterTab;

            if (Messenger.SecMessengerFilterTab == SecureMsgrFilterTab.Inbox)
            {
                ReadSelectedMessage(messageId);
            }

            ShowLoadingWheel = false;
        }

        private void ResetMessageView()
        {
            Messenger.ChildMessages = new List<SecuredMessage>();

            NewSecMsgrViewModel.IsVisible = false;
            NewSecMsgrViewModel.UpdateViewState();
        }

        private void ReadSelectedMessage(Guid messageId)
        {
            SecuredMessage selectedMsg = Messenger.Messages.First(msg => msg.Id == messageId || msg.ParentId == messageId);
            selectedMsg.Message.ReceiverList.First(mr => mr.User == New<KnownIdentities>().DefaultEncryptionIdentity.UserEmail.Address).Read = New<INow>().Utc;
        }

        public async Task DeleteMessageById(Guid messengerId, Guid parentId, SecureMsgrFilterTab secMessengerFilterTab)
        {
            if (messengerId == Guid.Empty)
            {
                _statusAlertService.Error(Texts.DeletionFailed);
                return;
            }

            SecuredMessage? messengerMsg = Messenger.ChildMessages.FirstOrDefault(mg => mg.Id == messengerId);
            if (messengerMsg == null)
            {
                return;
            }

            messengerMsg.ShowLoadingWheel = true;
            UpdateViewState();
            IEnumerable<Guid> selectedMessengerList = new List<Guid> { messengerId };
            bool deleted = await _messageService.DeleteMessagesByIds(selectedMessengerList, secMessengerFilterTab);
            if (deleted)
            {
                Messenger.ChildMessages = Messenger.ChildMessages.Where(cm => cm.Id != messengerId).ToList();
                if (!Messenger.ChildMessages.Any())
                {
                    Messenger.Messages = Messenger.Messages.Where(cm => cm.Id != messengerId).ToList();
                }
                _statusAlertService.Success(string.Format(Texts.DeletionSuccess, "Message"));
            }
            else
            {
                _statusAlertService.Error(string.Format(Texts.DeletionFailed, "Message"));
            }
            messengerMsg.ShowLoadingWheel = false;
            UpdateViewState();
        }

        public void ReplyMessage(Guid messengerId, Guid parentId, string recipients)
        {
            if (!New<AxCryptOnlineState>().IsOnline)
            {
                _statusAlertService.Error(Texts.NoInternetErrorMessage);
                return;
            }

            NewSecMsgrViewModel.Initialize(Messenger.SecMessengerFilterTab);
            NewSecMsgrViewModel.Id = messengerId;
            NewSecMsgrViewModel.ParentId = parentId;
            NewSecMsgrViewModel.ReceiverEmails = recipients;
            NewSecMsgrViewModel.ReceiverList = recipients.Split(",").Select(ru => new MessengerReceiver { EmailAddress = ru, Read = DateTime.MinValue }).ToList();

            NewSecMsgrViewModel.IsVisible = true;
            NewSecMsgrViewModel.UpdateViewState();
        }

        public async Task ViewMessageById(Guid messageId)
        {
            if (messageId == Guid.Empty)
            {
                return;
            }
            SecuredMessage messengerMsg = Messenger.Messages.FirstOrDefault(mg => mg.Id == messageId);
            if (messengerMsg == null)
            {
                return;
            }

            messengerMsg.ShowLoadingWheel = true;
            UpdateViewState();
            SecuredMessage message = await _messageService.GetMessageByIdAsync(messageId);
            if (message == null)
            {
                messengerMsg.ShowLoadingWheel = false;
                UpdateViewState();
                return;
            }

            messengerMsg.Message = message.Message;
            messengerMsg.ShowLoadingWheel = false;
            UpdateViewState();
        }

        public bool isMultiCheckboxVisible = false;

        public void ToggleMultiCheckbox()
        {
            isMultiCheckboxVisible = !isMultiCheckboxVisible;
        }

        internal void ComposeNewMessage()
        {
            if (!New<AxCryptOnlineState>().IsOnline)
            {
                _statusAlertService.Error(Texts.NoInternetErrorMessage);
                return;
            }

            Messenger.ChildMessages = new List<SecuredMessage>();
            NewSecMsgrViewModel.Initialize(Messenger.SecMessengerFilterTab);
            SelectedMessageId = new Guid();
            NewSecMsgrViewModel.IsVisible = true;
            NewSecMsgrViewModel.UpdateViewState();
        }

        internal void SelectMessagesForActions(SecuredMessage message)
        {
            message.IsSelected = !message.IsSelected;
            UpdateViewState();
        }

        private IEnumerable<Guid> GetSelectedMessagesList()
        {
            return Messenger.Messages.Where(msg => msg.IsSelected)?.Select(m => m.Id) ?? new List<Guid>();
        }

        internal async Task MultiActionAsync(string actionType)
        {
            IEnumerable<Guid> selectedMessengerList = GetSelectedMessagesList();
            if (!selectedMessengerList.Any())
            {
                return;
            }

            bool updated = false;
            using (ProcessIndicator progress = new ProcessIndicator())
            {
                switch (actionType)
                {
                    case "delete":
                        await DeleteSelectedMessagesAsync(selectedMessengerList);
                        break;

                    case "read":
                        updated = await _messageService.SetReadMessageStatusAsync(selectedMessengerList);
                        if (updated)
                        {
                            SetReadOrUnreadMessage(selectedMessengerList, New<INow>().Utc);
                        }
                        break;

                    case "unread":
                        updated = await _messageService.SetUnreadMessageStatusAsync(selectedMessengerList);
                        if (updated)
                        {
                            SetReadOrUnreadMessage(selectedMessengerList, DateTime.MinValue);
                        }
                        break;

                    default:
                        Console.WriteLine($"Invalid action type {actionType}");
                        break;
                }

                SelectAllMessages = false;
                UpdateViewState();
            }
        }

        private async Task DeleteSelectedMessagesAsync(IEnumerable<Guid> selectedMessengerList)
        {
            bool updated = await _messageService.DeleteMessagesByIds(selectedMessengerList, Messenger.SecMessengerFilterTab);
            if (!updated)
            {
                _statusAlertService.Error(string.Format(Texts.DeletionFailed, "Message"));
                return;
            }

            Messenger.Messages = Messenger.Messages.Where(msg => !selectedMessengerList.Contains(msg.Id)).ToList();
            Messenger.ChildMessages = new List<SecuredMessage>();
            UpdateViewState();
            _statusAlertService.Success(string.Format(Texts.DeletionSuccess, "Message"));
        }

        private void SetReadOrUnreadMessage(IEnumerable<Guid> selectedMessengerList, DateTime visibility)
        {
            List<SecuredMessage> mess = Messenger.Messages.Where(msg => selectedMessengerList.Contains(msg.Id)).ToList();
            mess.ForEach(msg =>
            {
                msg.IsSelected = false;
                msg.Message.ReceiverList.FirstOrDefault(ru => ru.User == New<KnownIdentities>().DefaultEncryptionIdentity.UserEmail.Address)!.Read = visibility;
            });
        }

        internal void ToggleAllMessages(ChangeEventArgs e)
        {
            bool isChecked = Convert.ToBoolean(e.Value);
            SelectAllMessages = isChecked;
            foreach (SecuredMessage msg in Messenger.Messages)
            {
                msg.IsSelected = isChecked;
            }

            UpdateViewState();
        }
    }
}