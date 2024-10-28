using AxCrypt.International;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Reports.Model.Csv
{
    public static class StripePaymentCsvConverterExtensions
    {
        public static TransactionRecord ToTransaction(this StripePaymentCsvRecord record)
        {
            DateTime utc = record.TimeStampUtc;
            if (!DateMonth.IsValid(utc))
            {
                return TransactionRecord.Empty;
            }

            CurrencyInfo paymentCurrency = new CurrencyInfo(record.Currency);
            decimal paymentAmount = paymentCurrency.SmartParse(record.Amount);
            decimal paymentAmountRefunded = paymentCurrency.SmartParse(record.AmountRefunded);
            if (paymentAmountRefunded > 0m && paymentAmountRefunded != paymentAmount)
            {
                throw new NotImplementedException("We don't support partial refunds yet!");
            }

            TransactionAmounts paymentAmounts = new TransactionAmounts()
            {
                Currency = paymentCurrency,
                Total = paymentAmount.ZeroAsNull(),
                Vat = (-paymentCurrency.SmartParse(record.MetadataAmountVat)).ZeroAsNull(),
            };

            TransactionAmounts sekAmounts = new TransactionAmounts()
            {
                Currency = CurrencyInfo.SEK,
                Total = CurrencyInfo.SEK.SmartParse(record.ConvertedAmount).ZeroAsNull(),
                Fee = (-CurrencyInfo.SEK.SmartParse(record.Fee)).ZeroAsNull(),
            };

            if (paymentAmountRefunded != 0m)
            {
                AdjustForFullRefund(paymentCurrency, paymentAmounts, sekAmounts, record);
            }

            TransactionRecord transaction = new TransactionRecord()
            {
                Id = record.Id,
                Utc = utc,
                Source = new SourceProviderName("Stripe"),
                Description = record.Description,
                Type = record.Status,
                AmountsAccount = sekAmounts,
                AmountsPayment = paymentAmounts,
                Country = LocaleInfo.Create(record.CardIssueCountry),
                AffectsBalance = false,
                AffectsRevenue = AffectsRevenue(record),
            };

            return transaction;
        }

        private static void AdjustForFullRefund(CurrencyInfo paymentCurrency, TransactionAmounts paymentAmounts, TransactionAmounts sekAmounts, StripePaymentCsvRecord record)
        {
            decimal convertedAmountRefunded = CurrencyInfo.SEK.SmartParse(record.ConvertedAmountRefunded);
            if (convertedAmountRefunded == 0m)
            {
                throw new InvalidOperationException("When adjusting for full refund, the converted amount refunded must exist.");
            }

            decimal convertedAmount = CurrencyInfo.SEK.SmartParse(record.ConvertedAmount);
            decimal fee = -CurrencyInfo.SEK.SmartParse(record.Fee);
            sekAmounts.Fee = fee.ZeroAsNull();
            sekAmounts.Total = null;

            paymentAmounts.Total = null;
            paymentAmounts.Fee = null;
            paymentAmounts.Vat = null;
        }

        private static bool AffectsRevenue(StripePaymentCsvRecord record)
        {
            switch (record.Status)
            {
                case "Failed":
                    return false;

                case "Paid":
                case "Refunded":
                    return true;

                default:
                    throw new InvalidOperationException($"Unknown status from Stripe: {record.Status}.");
            }
        }
    }
}