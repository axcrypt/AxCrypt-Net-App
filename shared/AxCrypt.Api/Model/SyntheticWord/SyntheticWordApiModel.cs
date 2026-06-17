using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Api.Model.SyntheticWord
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SyntheticWordApiModel : BaseApiModel
    {
        public SyntheticWordApiModel()
        { }

        [JsonProperty("culturename")]
        public string CultureName { get; set; }

        [JsonProperty("streamname")]
        public string StreamName { get; set; }

        [JsonProperty("beginningtrigrams")]
        public string BeginningTrigrams { get; set; }

        [JsonProperty("middletrigrams")]
        public string MiddleTrigrams { get; set; }

        [JsonProperty("endingtrigrams")]
        public string EndingTrigrams { get; set; }
    }
}