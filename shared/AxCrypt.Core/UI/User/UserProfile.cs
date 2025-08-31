using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Core.UI.User;

[JsonObject(MemberSerialization.OptIn)]
public class UserProfile
{
    public static UserProfile Empty { get; set; } = new UserProfile();

    [JsonProperty("active")]
    public bool Active { get; set; }

    [JsonProperty("userEmail")]
    public string UserEmail { get; set; } = "";

    [JsonProperty("basePath")]
    public string BasePath { get; set; } = "";

    [JsonProperty("subsType")]
    public string SubsType { get; set; } = "";

    [JsonProperty("lastLogOnUtc")]
    public DateTime LastLogOnUtc { get; set; }

    [JsonProperty("lastUpdateUtc")]
    public DateTime LastUpdateUtc { get; set; }
}