using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Reports.Model.Csv
{
    public class TransactionReportCsvRecord : CsvRecord<TransactionReportCsvRecord>
    {
        [CsvColumn("Payment Processor")]
        public string Source { get; set; }

        [CsvColumn("Month")]
        public string Month { get; set; }

        [CsvColumn("Id")]
        public string Id { get; set; }

        [CsvColumn("Utc")]
        public string Utc { get; set; }

        [CsvColumn("Id Reference")]
        public string IdReference { get; set; }

        [CsvColumn("Description")]
        public string Description { get; set; }

        [CsvColumn("Type")]
        public string Type { get; set; }

        [CsvColumn("Affects Revenue")]
        public string AffectsRevenue { get; set; }

        [CsvColumn("Affects Balance")]
        public string AffectsBalance { get; set; }

        [CsvColumn("Payment Currency")]
        public string PaymentCurrency { get; set; }

        [CsvColumn("Payment Total")]
        public string PaymentTotal { get; set; }

        [CsvColumn("Payment Fee")]
        public string PaymentFee { get; set; }

        [CsvColumn("Payment Vat")]
        public string PaymentVat { get; set; }

        [CsvColumn("Account Currency")]
        public string AccountCurrency { get; set; }

        [CsvColumn("Account Total")]
        public string AccountTotal { get; set; }

        [CsvColumn("Account Fee")]
        public string AccountFee { get; set; }

        [CsvColumn("Account Vat")]
        public string AccountVat { get; set; }

        [CsvColumn("Account Balance")]
        public string AccountBalance { get; set; }

        [CsvColumn("Country")]
        public string Country { get; set; }

        public override DateTime TimeStampUtc
        {
            get
            {
                return DateTime.ParseExact(Utc, "u", CultureInfo.InvariantCulture);
            }
        }
    }
}