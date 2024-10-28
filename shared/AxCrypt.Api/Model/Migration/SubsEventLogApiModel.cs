using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AxCrypt.Api.Model.Migration
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SubsEventLogApiModel
    {
        public SubsEventLogApiModel()
        {
        }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("eventid")]
        public string EventId { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("level")]
        public string Level { get; set; }

        [JsonProperty("eventType")]
        public string EventType { get; set; }

        [JsonProperty("paymentreference")]
        public string PaymentReference { get; set; }

        [JsonProperty("logname")]
        public string LogName { get; set; }

        [JsonProperty("created")]
        public DateTime Created { get; set; } = DateTime.MinValue;
    }
}