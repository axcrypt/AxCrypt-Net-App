using Newtonsoft.Json;

namespace AxCrypt.Api.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class EncryptedTextApiModel
    {
        public static EncryptedTextApiModel Empty = new EncryptedTextApiModel();

        public EncryptedTextApiModel()
        {
        }

        [JsonProperty("sender")]
        public string Sender { get; set; } = string.Empty;

        [JsonProperty("cipher")]
        public byte[] Cipher { get; set; }
    }
}