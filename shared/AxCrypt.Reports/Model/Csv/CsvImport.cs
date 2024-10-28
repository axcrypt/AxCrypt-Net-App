using AxCrypt.Reports.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Reports.Model.Csv
{
    public static class CsvImport
    {
        private enum CsvRecordType
        {
            StripePayment,
            StripeBalance,
            PayPalTransaction,
        }

        public static void Import(Stream stream, PersistentName repository)
        {
            string header = GetHeaderLine(stream);

            if (TryPayPalTransaction(stream, repository, header))
            {
                return;
            }
            if (TryStripePayment(stream, repository, header))
            {
                return;
            }
            if (TryStripeBalance(stream, repository, header))
            {
                return;
            }

            throw new InvalidOperationException("Can't import records because the header is not recognized as a known CSV file.");
        }

        private static bool TryPayPalTransaction(Stream stream, PersistentName repository, string header)
        {
            if (CsvData<PayPalTransactionCsvRecord>.CanReadRecords(header))
            {
                using (TextReader reader = new StreamReader(stream, Encoding.UTF8, false, 65536, true))
                {
                    IEnumerable<PayPalTransactionCsvRecord> records = CsvData<PayPalTransactionCsvRecord>.Load(reader, ',');
                    IEnumerable<TransactionRecord> transactions = records.Select(r => r.ToTransaction());
                    New<IRepository>().Save(repository, transactions);
                }
                return true;
            }
            return false;
        }

        private static bool TryStripePayment(Stream stream, PersistentName repository, string header)
        {
            if (CsvData<StripePaymentCsvRecord>.CanReadRecords(header))
            {
                using (TextReader reader = new StreamReader(stream, Encoding.UTF8, false, 65536, true))
                {
                    IEnumerable<StripePaymentCsvRecord> records = CsvData<StripePaymentCsvRecord>.Load(reader, ',');
                    IEnumerable<TransactionRecord> transactions = records.Select(r => r.ToTransaction());
                    New<IRepository>().Save(repository, transactions);
                }
                return true;
            }
            return false;
        }

        private static bool TryStripeBalance(Stream stream, PersistentName repository, string header)
        {
            if (CsvData<StripeBalanceCsvRecord>.CanReadRecords(header))
            {
                using (TextReader reader = new StreamReader(stream, Encoding.UTF8, false, 65536, true))
                {
                    IEnumerable<StripeBalanceCsvRecord> records = CsvData<StripeBalanceCsvRecord>.Load(reader, ',');
                    IEnumerable<TransactionRecord> transactions = records.Select(r => r.ToTransaction());
                    New<IRepository>().Save(repository, transactions);
                }
                return true;
            }
            return false;
        }

        private static string GetHeaderLine(Stream stream)
        {
            try
            {
                using (TextReader reader = new StreamReader(stream, Encoding.UTF8, false, 65536, true))
                {
                    return reader.ReadLine();
                }
            }
            finally
            {
                stream.Position = 0;
            }
        }
    }
}