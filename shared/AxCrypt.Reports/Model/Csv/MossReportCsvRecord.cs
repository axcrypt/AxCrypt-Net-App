using AxCrypt.Reports.Abstractions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Reports.Model.Csv
{
    public class MossReportCsvRecord : CsvRecord<MossReportCsvRecord>
    {
        [CsvColumn("Country")]
        public string Country { get; set; }

        [CsvColumn("Period")]
        public string Period { get; set; }

        [CsvColumn("Revenue (SEK)")]
        public string SekRevenue { get; set; }

        [CsvColumn("VAT (SEK)")]
        public string SekVat { get; set; }

        [CsvColumn("Revenue (EUR)")]
        public string EurRevenue { get; set; }

        [CsvColumn("VAT (EUR)")]
        public string EurVat { get; set; }

        public override DateTime TimeStampUtc
        {
            get
            {
                return DateTime.MinValue;
            }
        }
    }
}