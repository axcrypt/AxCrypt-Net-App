using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AxCrypt.Reports.Model.Csv
{
    public class PayPalTransactionCsvRecord : CsvRecord<PayPalTransactionCsvRecord>
    {
        [CsvColumn("Transaction ID")]
        public string TransactionId { get; private set; }

        [CsvColumn("Reference Txn ID")]
        public string ReferenceTransactionId { get; private set; }

        [CsvColumn("Date")]
        public string Date { get; private set; }

        [CsvColumn("Time")]
        public string Time { get; private set; }

        [CsvColumn("TimeZone")]
        public string TimeZone { get; private set; }

        [CsvColumn("Type")]
        public string Type { get; private set; }

        [CsvColumn("Status")]
        public string Status { get; private set; }

        [CsvColumn("Currency")]
        public string Currency { get; private set; }

        [CsvColumn("Gross")]
        public string Gross { get; private set; }

        [CsvColumn("Fee")]
        public string Fee { get; private set; }

        [CsvColumn("Item Title")]
        public string ItemTitle { get; private set; }

        [CsvColumn("Note")]
        public string Note { get; private set; }

        [CsvColumn("Sales Tax")]
        public string SalesTax { get; private set; }

        [CsvColumn("Balance")]
        public string Balance { get; private set; }

        [CsvColumn("Country Code")]
        public string Country { get; private set; }

        [CsvColumn("Balance Impact")]
        public string BalanceImpact { get; private set; }

        public override DateTime TimeStampUtc
        {
            get
            {
                DateTime date = DateTime.ParseExact(Date, "d/M/yyyy", CultureInfo.InvariantCulture);
                TimeSpan time = TimeSpan.ParseExact(Time, @"hh\:mm\:ss", CultureInfo.InvariantCulture);

                DateTimeOffset dto;
                if (TimeZone == "CET")
                {
                    dto = new DateTimeOffset(date + time, TimeSpan.FromHours(+1));
                    return dto.UtcDateTime;
                }
                if (TimeZone == "CEST")
                {
                    dto = new DateTimeOffset(date + time, TimeSpan.FromHours(+2));
                    return dto.UtcDateTime;
                }

                throw new InvalidOperationException($"Expecting all times from PayPal to be specified as CET or CEST, not '{TimeZone}'.");
            }
        }
    }
}