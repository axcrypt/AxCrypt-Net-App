using Newtonsoft.Json;

namespace AxCrypt.Api.Model.SecuredMessenger
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SecuredMessengerApiModel : BaseApiModel
    {
        public SecuredMessengerApiModel()
        {
        }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("messageId")]
        public Guid MessageId { get; set; }

        [JsonProperty("sender")]
        public string Sender { get; set; } = string.Empty;

        [JsonProperty("receiver")]
        public IEnumerable<MessengerReceiverApiModel> Receiver { get; set; } = new List<MessengerReceiverApiModel>();

        [JsonProperty("visibility")]
        public string Visibility { get; set; } = string.Empty;

        [JsonProperty("visibleuntil")]
        public DateTime VisibleUntil { get; set; }

        [JsonProperty("encryptedMessage")]
        public string EncryptedMessage { get; set; } = string.Empty;

        [JsonProperty("parentid")]
        public Guid ParentId { get; set; }
    }
}