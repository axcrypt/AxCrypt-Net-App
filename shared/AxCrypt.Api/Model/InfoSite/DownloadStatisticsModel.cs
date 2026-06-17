using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Api.Model.InfoSite
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DownloadStatisticsModel
    {
        [JsonProperty("filename")]
        public string FileName { get; set; }

        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("userAgent")]
        public string UserAgent { get; set; }

        [JsonProperty("geolocation")]
        public string GeoLocation { get; set; }

        [JsonProperty("userlanguageculture")]
        public string UserLanguageCulture { get; set; }

        [JsonProperty("sourceurl")]
        public string SourceUrl { get; set; }

        [JsonProperty("referrerurl")]
        public string ReferrerUrl { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }
    }
}