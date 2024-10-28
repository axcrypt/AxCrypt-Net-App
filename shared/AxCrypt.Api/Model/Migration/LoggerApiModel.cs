using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AxCrypt.Api.Model.Migration
{
    public class LoggerApiModel : BaseApiModel
    {
        public LoggerApiModel(string logLevel, string? msg, DateTime created)
        {
            LogLevel = logLevel;
            this.msg = msg;
            CreatedUtc = created;
        }

        [JsonProperty("id")]
        public long id { get; set; }

        [JsonProperty("LogLevel")]
        public string LogLevel { get; set; }

        [JsonProperty("msg")]
        public string? msg { get; set; }
    }
}