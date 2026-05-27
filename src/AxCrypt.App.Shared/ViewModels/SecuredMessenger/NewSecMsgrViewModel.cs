using AxCrypt.Api.SecuredMessenger;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.Content;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.ViewModels.SecuredMessenger
{
    public class NewSecMsgrViewModel : ViewModelBase
    {
        private ISecureMessagingService _msgService { get; set; }

        private IStatusAlertService _StatusAlertService { get; set; }

        public NewSecMsgrViewModel(ISecureMessagingService msgService, IStatusAlertService statusAlertService)
        {
            _msgService = msgService;
            _StatusAlertService = statusAlertService;
        }

        public void Initialize(SecureMsgrFilterTab selectedTab)
        {
            SelectedTab = selectedTab;
            Id = Guid.Empty;
            ParentId = Guid.Empty;
            ReceiverEmails = "";
            ReceiverList = new List<MessengerReceiver>();
            Visibility = SecureMsgrVisibility.Forever;
            EncryptedMessage = "";
        }

        public string UserEmail { get; set; }

        public Guid Id { get; set; } = Guid.Empty;

        public Guid ParentId { get; set; } = Guid.Empty;

        [Required]
        public string ReceiverEmails { get; set; }

        public IList<MessengerReceiver> ReceiverList { get; set; } = new List<MessengerReceiver>();

        public IEnumerable<string> MessageVisibilityList
        {
            get
            {
                return Enum.GetNames(typeof(SecureMsgrVisibility));
            }
        }

        public SecureMsgrVisibility Visibility { get; set; }

        public SecureMsgrFilterTab SelectedTab { get; set; }

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
            }
        }

        public event Action<SecureMsgrFilterTab>? OnTabUpdateViewState;

        /// <summary>
        /// Toggled around <see cref="SentMessageAsync"/> so the New composer
        /// can paint a popup AxLoader card while the message is in flight.
        /// Distinct from <see cref="ProcessIndicator"/>'s global scrim — this
        /// one lives inside the compose panel and keeps the rest of the
        /// messenger interactive.
        /// </summary>
        public bool IsSending { get; private set; }

        public void TapUpdateViewState()
        {
            OnTabUpdateViewState?.Invoke(SelectedTab);
        }

        public async Task SentMessageAsync(NewSecMsgrViewModel newSecMsgrViewModel)
        {
            if (newSecMsgrViewModel == null || !newSecMsgrViewModel.ReceiverEmails.Any() || string.IsNullOrWhiteSpace(newSecMsgrViewModel.EncryptedMessage))
            {
                _StatusAlertService.Error(Texts.SendSecuredMessageFailure);
                return;
            }

            if (!New<Common.AxCryptOnlineState>().IsOnline)
            {
                _StatusAlertService.Error(@Texts.NoInternetErrorMessage);
                return;
            }

            if (IsSending)
            {
                // Re-entrancy guard — repeated Send clicks would queue
                // duplicate sends and look like a hang.
                return;
            }

            IsSending = true;
            UpdateViewState();
            bool result = false;
            try
            {
                using (ProcessIndicator indicator = new ProcessIndicator())
                {
                    result = await _msgService.SentMessageAsync(newSecMsgrViewModel);
                }
            }
            catch (Exception ex)
            {
                AxCrypt.App.Shared.Helpers.AxCServiceProviderExtension.ErrorReportService?.Report(
                    ex, "Sending message");
            }
            finally
            {
                IsSending = false;
                UpdateViewState();
            }

            if (result)
            {
                _StatusAlertService.Success(Texts.SendSecuredMessageSuccess);
                newSecMsgrViewModel.IsVisible = false;
                if (ParentId == Guid.Empty)
                {
                    SelectedTab = SecureMsgrFilterTab.Sent;
                }
                TapUpdateViewState();
                return;
            }

            _StatusAlertService.Error(Texts.SendSecuredMessageFailure);
        }
    }

    public class MessengerReceiver
    {
        public string EmailAddress { get; set; } = "";

        public DateTime Read { get; set; }
    }
}