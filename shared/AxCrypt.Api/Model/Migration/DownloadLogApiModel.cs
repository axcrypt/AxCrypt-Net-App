using Newtonsoft.Json;
using System;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DownloadLogApiModel : BaseApiModel
    {
        public DownloadLogApiModel()
        { }

        [JsonProperty("datetime_time_utc")]
        public DateTime DateTimeUTC { get; set; }

        [JsonProperty("length")]
        public long Length { get; set; }

        [JsonProperty("log_group")]
        public string? LogGroup { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("ok")]
        public bool Ok { get; set; }

        [JsonProperty("url_referrer")]
        public string? UrlRefferer { get; set; }

        [JsonProperty("user_agent")]
        public string? UserAgent { get; set; }

        [JsonProperty("user_host_addr")]
        public string? UserHostAddress { get; set; }
    }
}