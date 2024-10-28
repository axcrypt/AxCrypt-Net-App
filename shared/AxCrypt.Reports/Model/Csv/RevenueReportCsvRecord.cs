using AxCrypt.Reports.Abstractions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Reports.Model.Csv
{
    public class RevenueReportCsvRecord : CsvRecord<RevenueReportCsvRecord>
    {
        [CsvColumn("Payment Processor")]
        public string Source { get; set; }

        [CsvColumn("Month")]
        public string Month { get; set; }

        [CsvColumn("Account Currency")]
        public string AccountCurrency { get; set; }

        [CsvColumn("Opening Balance (SEK)")]
        public string SekBalanceOpen { get; set; }

        [CsvColumn("Closing Balance (SEK)")]
        public string SekBalanceClose { get; set; }

        [CsvColumn("Fees (SEK)")]
        public string SekFees { get; set; }

        [CsvColumn("Turnover SE (SEK)")]
        public string SekSeTurnover { get; set; }

        [CsvColumn("VAT SE (SEK)")]
        public string SekSeVat { get; set; }

        [CsvColumn("Turnover EU (SEK)")]
        public string SekEuTurnover { get; set; }

        [CsvColumn("VAT EU (SEK)")]
        public string SekEuVat { get; set; }

        [CsvColumn("Turnover Export (SEK)")]
        public string SekExportTurnover { get; set; }

        [CsvColumn("Closing Balance")]
        public string BalanceClose { get; set; }

        [CsvColumn("Net Sales")]
        public string NetSales { get; set; }

        [CsvColumn("Net Sales (SEK)")]
        public string SekNetSales { get; set; }

        [CsvColumn("Exchange Difference (SEK)")]
        public string ExchangeDifference { get; set; }

        public override DateTime TimeStampUtc
        {
            get
            {
                return DateTime.MinValue;
            }
        }
    }
}