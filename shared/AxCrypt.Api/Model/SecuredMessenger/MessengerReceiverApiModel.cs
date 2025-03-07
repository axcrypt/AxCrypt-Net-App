using Newtonsoft.Json;

namespace AxCrypt.Api.Model.SecuredMessenger
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MessengerReceiverApiModel
    {
        [JsonProperty("user")]
        public string User { get; set; } = "";

        [JsonProperty("read")]
        public DateTime Read { get; set; } = DateTime.MinValue;
    }
}