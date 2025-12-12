using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AxCrypt.Api.Model.TextEncryption
{
    [JsonObject(MemberSerialization.OptIn)]
    public class TextEncryptionApiModel : BaseApiModel
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("textid")]
        public Guid TextId { get; set; }

        [JsonProperty("owner")]
        public string Owner { get; set; }

        [JsonProperty("encryptedText")]
        public string EncryptedText { get; set; }

        [JsonProperty("recipients")]
        public IEnumerable<string> Recipients { get; set; }

        [JsonProperty("visibleuntil")]
        public DateTime VisibleUntil { get; set; }
    }
}