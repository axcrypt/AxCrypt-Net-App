using AxCrypt.Api.Model.SecuredMessenger;
using AxCrypt.Core.UI;

namespace AxCrypt.Core.SecuredMessenger
{
    public class UserSecuredMessage
    {
        public UserSecuredMessage()
        {
        }

        public SecureMsgrVisibility Visibility { get; set; }

        public DateTime VisibleUntil { get; set; }

        public IEnumerable<MessengerReceiverApiModel> ReceiverList
        {
            get; set;
        }

        private string _username;

        public string Username
        {
            get { return _username ?? String.Empty; }
            set { _username = value; }
        }

        private string _theMessage;

        /// <summary>
        /// The (short) actual secret - it may be any text
        /// </summary>
        public string TheMessage
        {
            get { return _theMessage ?? String.Empty; }
            set { _theMessage = value ?? string.Empty; }
        }
    }
}