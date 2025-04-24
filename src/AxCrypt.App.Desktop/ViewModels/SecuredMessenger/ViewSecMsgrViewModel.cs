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
    public class ViewSecMsgrViewModel : ViewModelBase
    {
        private ISecureMessagingService _messageService { get; set; }
        private IStatusAlertService _statusAlertService { get; set; }

        public DateTime IsRead { get; set; } = DateTime.MinValue;

        public ViewSecMsgrViewModel(ISecureMessagingService msgService, IStatusAlertService statusAlertService, NewSecMsgrViewModel newSecMsgrViewModel)
        {
            _messageService = msgService;
            _statusAlertService = statusAlertService;
            NewSecMsgrViewModel = newSecMsgrViewModel;
        }

        public SecuredMessengerModel Messenger { get; set; } = new SecuredMessengerModel();

        public IList<SecuredMessage> Messages { get; set; } = new List<SecuredMessage>();

        public Guid? SelectedMessageId { get; set; } = Guid.Empty;

        public bool AnySelected => Messages.Any(m => m.IsSelected);


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

        public async Task ViewMessageReplies(Guid messageId, SecureMsgrFilterTab securedMessengerFilterTab, MouseEventArgs e)
        {
            if (messageId == Guid.Empty)
            {
                return;
            }

            SelectedMessageId = messageId;

            Messenger = new SecuredMessengerModel();
            NewSecMsgrViewModel.IsVisible = false;

            ShowLoadingWheel = true;
            IsRead = DateTime.MaxValue;
            Messenger = await _messageService.GetModelForViewMessage(messageId, securedMessengerFilterTab);
            Messenger.SecMessengerFilterTab = securedMessengerFilterTab;
            ShowLoadingWheel = false;
        }

        public async Task DeleteMessageById(Guid messengerId, Guid parentId, SecureMsgrFilterTab secMessengerFilterTab)
        {
            if (messengerId == Guid.Empty)
            {
                _statusAlertService.Error(Texts.DeletionFailed);
                return;
            }

            SecuredMessage messengerMsg = Messenger.Messages.FirstOrDefault(mg => mg.Id == messengerId);
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
                _statusAlertService.Success(string.Format(Texts.DeletionSuccess, "Message"));
            }
            else
            {
                _statusAlertService.Error(string.Format(Texts.DeletionFailed, "Message"));
            }
            messengerMsg.ShowLoadingWheel = false;
            UpdateViewState();
        }

        public async Task ReplyMessage(Guid messengerId, Guid parentId, string recipients)
        {
            if (!New<AxCryptOnlineState>().IsOnline)
            {
                _statusAlertService.Error(Texts.NoInternetErrorMessage);
                return;
            }

            NewSecMsgrViewModel.Initialize();
            NewSecMsgrViewModel.Id = messengerId;
            NewSecMsgrViewModel.ParentId = parentId;
            NewSecMsgrViewModel.ReceiverEmails = recipients;
            NewSecMsgrViewModel.ReceiverList = recipients.Split(",").Select(ru => new MessengerReceiverViewModel { EmailAddress = ru, Read = DateTime.MinValue }).ToList();

            NewSecMsgrViewModel.IsVisible = true;
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

            NewSecMsgrViewModel.Initialize();
            NewSecMsgrViewModel.IsVisible = true;
        }

        internal void SelectMessagesForActions(SecuredMessage message)
        {
            message.IsSelected = !message.IsSelected;
            UpdateViewState();
        }

        private IEnumerable<Guid> GetSelectedMessagesList()
        {
            return Messages.Where(msg => msg.IsSelected)?.Select(m => m.Id) ?? new List<Guid>();
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

            Messages = Messages.Where(msg => !selectedMessengerList.Contains(msg.Id)).ToList();
            _statusAlertService.Success(string.Format(Texts.DeletionSuccess, "Message"));
        }

        private void SetReadOrUnreadMessage(IEnumerable<Guid> selectedMessengerList, DateTime visibility)
        {
            List<SecuredMessage> mess = Messages.Where(msg => selectedMessengerList.Contains(msg.Id)).ToList();
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
            foreach (SecuredMessage msg in Messages)
            {
                msg.IsSelected = isChecked;
            }
        }
    }
}