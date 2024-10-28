using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace AxCrypt.Reports.Model.Csv
{
    public class StripeBalanceCsvRecord : CsvRecord<StripeBalanceCsvRecord>
    {
        [CsvColumn("id")]
        public string Id { get; set; }

        [CsvColumn("Type")]
        public string Type { get; set; }

        [CsvColumn("Source")]
        public string Source { get; set; }

        [CsvColumn("Amount")]
        public string Amount { get; set; }

        [CsvColumn("Fee")]
        public string Fee { get; set; }

        [CsvColumn("Destination Platform Fee")]
        public string DestinationPlatformFee { get; set; }

        [CsvColumn("Net")]
        public string Net { get; set; }

        [CsvColumn("Currency")]
        public string Currency { get; set; }

        [CsvColumn("Created (UTC)")]
        public string CreatedUtc { get; set; }

        [CsvColumn("Available On (UTC)")]
        public string AvailableOnUtc { get; set; }

        [CsvColumn("Description")]
        public string Description { get; set; }

        [CsvColumn("Customer Facing Amount")]
        public string CustomerFacingAmount { get; set; }

        [CsvColumn("Customer Facing Currency")]
        public string CustomerFacingCurrency { get; set; }

        [CsvColumn("Transfer")]
        public string Transfer { get; set; }

        [CsvColumn("Transfer Date (UTC)")]
        public string TransferDateUtc { get; set; }

        [CsvColumn("Transfer Group")]
        public string TransferGroup { get; set; }

        [CsvColumn("Amount Vat (metadata)")]
        public string CustomerFacingAmountVat { get; set; }

        [CsvColumn("Paid For (metadata)")]
        public string PaidFor { get; set; }

        [CsvColumn("Vat Country (metadata)")]
        public string VatCountry { get; set; }

        [CsvColumn("Vat Rate (metadata)")]
        public string VatRate { get; set; }

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