using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AxCrypt.App.Shared.Models.Secret;

[JsonObject(MemberSerialization.OptIn)]
public class SecretsClientModel
{
    [JsonProperty("secrets")]
    public IList<SecretClientModel> Secrets { get; set; } = new List<SecretClientModel>();
}