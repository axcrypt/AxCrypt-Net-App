using AxCrypt.International;

using System;

namespace AxCrypt.Reports.Model.Csv
{
    public static class PayPalTransactionCsvConverterExtensions
    {
        public static TransactionRecord ToTransaction(this PayPalTransactionCsvRecord record)
        {
            DateTime utc = record.TimeStampUtc;
            if (!DateMonth.IsValid(utc))
            {
                return TransactionRecord.Empty;
            }

            CurrencyInfo paymentCurrency = new CurrencyInfo(record.Currency);
            TransactionAmounts paymentAmounts = new TransactionAmounts()
            {
                Currency = paymentCurrency,
                Total = paymentCurrency.SmartParse(record.Gross).ZeroAsNull(),
                Fee = paymentCurrency.SmartParse(record.Fee).ZeroAsNull(),
                Vat = (-paymentCurrency.SmartParse(record.SalesTax)).ZeroAsNull(),
            };
            if (paymentAmounts.Total < 0 && paymentAmounts.Vat.HasValue)
            {
                paymentAmounts.Vat = (-paymentAmounts.Vat.Value).ZeroAsNull();
            }

            TransactionRecord transaction = new TransactionRecord()
            {
                Id = record.TransactionId,
                IdReference = record.ReferenceTransactionId,
                Utc = utc,
                Source = new SourceProviderName("PayPal"),
                Description = BuildDescription(record),
                Type = record.Type,
                AffectsRevenue = AffectsRevenue(record),
                AffectsBalance = AffectsBalance(record),
                AmountsPayment = paymentAmounts,
                AmountsAccount = new TransactionAmounts() { Currency = paymentCurrency, },
                AccountBalance = paymentCurrency.SmartParse(record.Balance),
                Country = LocaleInfo.Create(record.Country),
            };

            return transaction;
        }

        private static bool AffectsRevenue(PayPalTransactionCsvRecord record)
        {
            switch (record.Type)
            {
                case "Website Payment":
                case "Payment Refund":
                case "Chargeback":
                case "Subscription Payment":
                    return record.Status == "Completed";

                case "General Payment":
                case "Payment Reversal":
                case "Hold on Balance for Dispute Investigation":
                case "Cancellation of Hold for Dispute Resolution":
                case "General Withdrawal":
                case "General Currency Conversion":
                case "Shopping Cart Item":
                case "Mobile Payment":
                case "Express Checkout Payment":
                case "Payment Review Hold":
                case "Payment Review Release":
                case "PreApproved Payment Bill User Payment":
                case "General Account Correction":
                case "General Credit Card Deposit":
                case "Chargeback Fee":
                case "Order":
                case "General Authorization":
                case "Account Hold for Open Authorization":
                case "Reversal of General Account Hold":
                    return false;

                default:
                    throw new InvalidOperationException($"Unexpected payment type: {record.Type}");
            }
        }

        private static bool AffectsBalance(PayPalTransactionCsvRecord record)
        {
            switch (record.BalanceImpact)
            {
                case "Debit":
                case "Credit":
                    return true;

                case "Memo":
                    return false;

                default:
                    throw new InvalidOperationException($"Unexpected balance impact type: {record.Type}");
            }
        }

        private static string BuildDescription(PayPalTransactionCsvRecord record)
        {
            string description = record.ItemTitle;
            if (record.Note.Length > 0)
            {
                description += " " + record.Note;
            }

            return description;
        }
    }
}