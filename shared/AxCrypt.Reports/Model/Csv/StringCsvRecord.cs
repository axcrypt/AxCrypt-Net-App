using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace AxCrypt.Reports.Model.Csv
{
    public class StringCsvRecord : CsvRecord<StringCsvRecord>
    {
        public static readonly StringCsvRecord Empty = new StringCsvRecord() { Id = string.Empty, Utc = DateTime.MinValue.ToString("u"), Record = string.Empty };

        [CsvColumn("id")]
        public string Id { get; set; }

        [CsvColumn("utc")]
        public string Utc { get; set; }

        [CsvColumn("record")]
        public string Record { get; set; }

        public override DateTime TimeStampUtc
        {
            get
            {
                return DateTime.ParseExact(Utc, "u", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            }
        }
    }
}