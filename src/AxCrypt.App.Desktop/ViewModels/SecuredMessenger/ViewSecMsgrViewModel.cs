using AxCrypt.Abstractions;
using AxCrypt.Api.SecuredMessenger;
using AxCrypt.App.Desktop.Components.SecuredMessenger;
using AxCrypt.App.Desktop.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.SecuredMessenger;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Desktop.ViewModels.SecuredMessenger
{
    public class ViewSecMsgrViewModel : ViewModelBase
    {
        ISecureMessagingService _msgService { get; set; }
        IStatusAlertService _statusAlertService { get; set; }

        private NewSecMsgrViewModel _newSecMsgrViewModel;

        public ViewSecMsgrViewModel(ISecureMessagingService msgService, IStatusAlertService statusAlertService, NewSecMsgrViewModel newSecMsgrViewModel)
        {
            _msgService = msgService;
            _statusAlertService = statusAlertService;
            _newSecMsgrViewModel = newSecMsgrViewModel;
        }

        public SecuredMessengerModel Messenger { get; set; } = new SecuredMessengerModel();

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

        public async Task ViewMessageReplies(Guid messageId, SecureMsgrFilterTab securedMessengerFilterTab)
        {
            if (messageId == Guid.Empty)
            {
                return;
            }

            Messenger = new SecuredMessengerModel();

            ShowLoadingWheel = true;
            Messenger = await _msgService.GetModelForViewMessage(messageId, securedMessengerFilterTab);
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
            bool deleted = await _msgService.DeleteMessagesByIds(selectedMessengerList, secMessengerFilterTab);
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

            _newSecMsgrViewModel.Initialize();
            _newSecMsgrViewModel.Id = messengerId;
            _newSecMsgrViewModel.ParentId = parentId;
            _newSecMsgrViewModel.ReceiverEmails = recipients;
            _newSecMsgrViewModel.ReceiverList = recipients.Split(",").Select(ru => new MessengerReceiverViewModel { EmailAddress = ru, Read = DateTime.MinValue }).ToList();

            _newSecMsgrViewModel.IsVisible = true;
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
            SecuredMessage message = await _msgService.GetMessageByIdAsync(messageId);
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
    }
}