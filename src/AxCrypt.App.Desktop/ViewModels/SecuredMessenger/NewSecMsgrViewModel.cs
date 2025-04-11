using AxCrypt.App.Desktop.Services;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace AxCrypt.App.Desktop.ViewModels.SecuredMessenger
{
    public class NewSecMsgrViewModel : ViewModelBase
    {
        ISecureMessagingService _msgService { get; set; }

        public NewSecMsgrViewModel(ISecureMessagingService msgService)
        {
            _msgService = msgService;
        }

        public void Initialize()
        {
            Id = Guid.Empty;
            ParentId = Guid.Empty;
            ReceiverEmails = "";
            ReceiverList = new List<MessengerReceiverViewModel>();
            Visibility = SecureMsgrVisibility.Forever;
            EncryptedMessage = "";
        }

        public string UserEmail { get; set; }

        public Guid Id { get; set; } = Guid.Empty;

        public Guid ParentId { get; set; } = Guid.Empty;

        [Required]
        public string ReceiverEmails { get; set; }

        public IList<MessengerReceiverViewModel> ReceiverList { get; set; } = new List<MessengerReceiverViewModel>();

        public IEnumerable<string> MessageVisibilityList
        {
            get
            {
                return Enum.GetNames(typeof(SecureMsgrVisibility));
            }
        }

        public SecureMsgrVisibility Visibility { get; set; }

        [Required(ErrorMessage = "The message field is required.")]

        public string EncryptedMessage { get; set; }

        private bool _isVisible;
        public bool IsVisible
        {
            get
            {
                return _isVisible;
            }
            set
            {
                _isVisible = value;
                UpdateViewState();
            }
        }

        public async Task<bool> SentMessageAsync(NewSecMsgrViewModel newSecMsgrViewModel)
        {
            if (newSecMsgrViewModel == null)
            {
                return false;
            }

            return await _msgService.SentMessageAsync(newSecMsgrViewModel);
        }
    }

    public class MessengerReceiverViewModel : ViewModelBase
    {
        public string EmailAddress { get; set; } = "";

        public DateTime Read { get; set; }
    }
}