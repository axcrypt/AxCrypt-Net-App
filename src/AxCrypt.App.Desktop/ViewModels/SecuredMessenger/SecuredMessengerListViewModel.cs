using AxCrypt.Api.SecuredMessenger;
using AxCrypt.App.Desktop.Services;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.Core.UI.ViewModel;
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

                UpdateViewState();
            }
        }

        public async Task SearchList(SecuredMessengerModel messengerModel)
        {
            if (messengerModel != null || !string.IsNullOrWhiteSpace(messengerModel.Keyword))
            {
                Messenger = await _messageService.GetSecMsgSearchFilterAsync(messengerModel.Keyword, messengerModel);
            }

            if (messengerModel == null || string.IsNullOrWhiteSpace(messengerModel.Keyword))
            {
                await GetMessagesList(messengerModel.SecMessengerFilterTab);
                return;
            }            
        }

        public async Task<SecuredMessengerModel> LoadMore(int pageNumber, SecureMsgrFilterTab secMessengerFilterTab)
        {
            return await _messageService.GetLoadMoreSEMAsync(pageNumber, secMessengerFilterTab);
        }
    }
}