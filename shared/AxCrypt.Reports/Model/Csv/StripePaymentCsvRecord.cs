using AxCrypt.International;
using AxCrypt.Reports.Abstractions;
using AxCrypt.Reports.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Reports.Model.Csv
{
    public class StripePaymentCsvRecord : CsvRecord<StripePaymentCsvRecord>
    {
        [CsvColumn("id")]
        public string Id { get; private set; }

        [CsvColumn("Created (UTC)")]
        public string CreatedUtc { get; private set; }

        [CsvColumn("Description")]
        public string Description { get; private set; }

        [CsvColumn("Status")]
        public string Status { get; private set; }

        [CsvColumn("Currency")]
        public string Currency { get; private set; }

        [CsvColumn("Amount")]
        public string Amount { get; private set; }

        [CsvColumn("Amount Refunded")]
        public string AmountRefunded { get; private set; }

        [CsvColumn("Converted Amount")]
        public string ConvertedAmount { get; private set; }

        [CsvColumn("Converted Amount Refunded")]
        public string ConvertedAmountRefunded { get; private set; }

        [CsvColumn("Fee")]
        public string Fee { get; private set; }

        [CsvColumn("Amount Vat (metadata)")]
        public string MetadataAmountVat { get; private set; }

        [CsvColumn("Card Issue Country")]
        public string CardIssueCountry { get; private set; }

        protected override StripePaymentCsvRecord Invariant()
        {
            switch (Status)
            {
                case "Paid":
                case "Failed":
                case "Refunded":
                    return this;

                default:
                    throw new InvalidOperationException($"Unrecognized Stripe status {Status}.");
            }
        }

        public override DateTime TimeStampUtc
        {
            get
            {
                DateTime utc = DateTime.ParseExact(CreatedUtc, @"yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                return utc;
            }
        }
    }
}