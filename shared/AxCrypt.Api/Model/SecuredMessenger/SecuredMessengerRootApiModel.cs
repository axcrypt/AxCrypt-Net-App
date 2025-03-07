using Newtonsoft.Json;

namespace AxCrypt.Api.Model.SecuredMessenger
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SecuredMessengerRootApiModel
    {
        [JsonProperty("message")]
        public SecuredMessengerApiModel Message { get; set; }

        [JsonProperty("replies")]
        public IEnumerable<SecuredMessengerApiModel> Replies { get; set; }
    }
}