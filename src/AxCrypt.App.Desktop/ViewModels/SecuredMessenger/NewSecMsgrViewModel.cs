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

        public Guid Id { get; set; } = Guid.Empty;

        public Guid ParentId { get; set; } = Guid.Empty;

        [Required]
        [Display(Name = "ReceiverList")]
        public string ReceiverEmails { get; set; }

        public IEnumerable<MessengerReceiverViewModel> ReceiverList { get; set; } = new List<MessengerReceiverViewModel>();

        public IEnumerable<string> MessageVisibilityList
        {
            get
            {
                return Enum.GetNames(typeof(SecureMsgrVisibility));
            }
        }

        [Display(Name = "Visibility")]
        public SecureMsgrVisibility Visibility { get; set; }

        [Required(ErrorMessage = "The message field is required.")]
        [Display(Name = "Encrypted Message")]
        public string EncryptedMessage { get; set; }

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