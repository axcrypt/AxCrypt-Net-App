using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Reports.Model.Csv
{
    public class ExchangeRateCsvRecord : CsvRecord<ExchangeRateCsvRecord>
    {
        [CsvColumn("Source")]
        public string Source { get; set; }

        [CsvColumn("From Currency")]
        public string FromCurrency { get; set; }

        [CsvColumn("To Currency")]
        public string ToCurrency { get; set; }

        [CsvColumn("Rate")]
        public string Rate { get; set; }

        [CsvColumn("Sample Period")]
        public string SamplePeriod { get; set; }

        [CsvColumn("Validity Period")]
        public string ValidityPeriod { get; set; }

        [CsvColumn("Effective Date")]
        public string EffectiveDate { get; set; }

        public override DateTime TimeStampUtc
        {
            get
            {
                return DateTime.ParseExact(EffectiveDate, "u", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            }
        }
    }
}