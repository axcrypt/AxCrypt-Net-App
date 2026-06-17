using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AxCrypt.Api.Model.Migration
{
    public class LoggerApiModel : BaseApiModel
    {
        public LoggerApiModel()
        { }

        public LoggerApiModel(string logLevel, string? msg, DateTime created)
        {
            LogLevel = logLevel;
            this.Message = msg;
            CreatedUtc = created;
        }

        [JsonProperty("id")]
        public long id { get; set; }

        [JsonProperty("LogLevel")]
        public string LogLevel { get; set; }

        [JsonProperty("msg")]
        public string? Message { get; set; }

        [JsonProperty("ipaddress")]
        public string IpAddress { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("requesturl")]
        public string RequestUrl { get; set; }

        [JsonProperty("port")]
        public string Port { get; set; }

        [JsonProperty("useripaddress")]
        public string UserIpAddress { get; set; }

        [JsonProperty("useragent")]
        public string UserAgent { get; set; }

        [JsonProperty("urlreferrer")]
        public string UrlReferrer { get; set; }

        [JsonProperty("statuscode")]
        public string StatusCode { get; set; }

        [JsonProperty("timetaken")]
        public string TimeTaken { get; set; }
    }
}