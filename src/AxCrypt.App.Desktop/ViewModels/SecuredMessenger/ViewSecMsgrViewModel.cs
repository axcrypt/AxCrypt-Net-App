using AxCrypt.Api.SecuredMessenger;
using AxCrypt.App.Desktop.Services;
using AxCrypt.Core.SecuredMessenger;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AxCrypt.App.Desktop.ViewModels.SecuredMessenger
{
    public class ViewSecMsgrViewModel : ViewModelBase
    {
        ISecureMessagingService _msgService { get; set; }

        public ViewSecMsgrViewModel(ISecureMessagingService msgService)
        {
            _msgService = msgService;
        }

        public SecuredMessengerModel Messenger { get; set; } = new SecuredMessengerModel();

        public async Task ViewMessage(Guid id, SecureMsgrFilterTab securedMessengerFilterTab)
        {
            if (id == Guid.Empty)
            {
                return;
            }

            Messenger = await _msgService.GetModelForViewMessage(id, securedMessengerFilterTab);
            Messenger.SecMessengerFilterTab = securedMessengerFilterTab;
        }

        public async Task<bool> Delete(Guid messengerId, Guid parentId, SecureMsgrFilterTab secMessengerFilterTab)
        {
            if (messengerId == Guid.Empty)
            {
                Console.WriteLine("Messenger ID is empty.");
                return false;
            }
            IEnumerable<Guid> selectedMessengerList = new List<Guid> { messengerId };

            if (!selectedMessengerList.Any())
            {
                Console.WriteLine("No valid GUIDs found.");
                return false;
            }

            await _msgService.DeleteMessagesByIds(selectedMessengerList, secMessengerFilterTab);

            if (parentId != Guid.Empty)
            {
            }

            return true;
        }

        public async Task<SecuredMessage> ViewMessageById(Guid messageId)
        {
            if (messageId == Guid.Empty)
            {
                Console.WriteLine("Message ID is empty.");
                return new SecuredMessage();
            }

            SecuredMessage message = await _msgService.GetMessageByIdAsync(messageId);

            if (message == null)
            {
                Console.WriteLine($"No message found for ID: {messageId}");
                return new SecuredMessage();
            }

            Console.WriteLine($"Message found: {message}");
            return message;
        }
    }
}