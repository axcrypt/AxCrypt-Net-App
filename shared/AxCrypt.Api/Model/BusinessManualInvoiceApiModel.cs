using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Api.Model
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class BusinessManualInvoiceApiModel : BaseApiModel
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("creator")]
        public string Creator { get; set; }

        [JsonProperty("businessname")]
        public string BusinessName { get; set; }

        [JsonProperty("metadataobject")]
        public string MetaDataObject { get; set; }

        [JsonProperty("invoiceurl")]
        public string InvoiceUrl { get; set; }
    }
}