using AxCrypt.International;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AxCrypt.Reports.Model.Csv
{
    public static class StripeBalanceCsvConverterExtensions
    {
        public static TransactionRecord ToTransaction(this StripeBalanceCsvRecord record)
        {
            DateTime utc = record.TimeStampUtc;
            if (!DateMonth.IsValid(utc))
            {
                return TransactionRecord.Empty;
            }

            CurrencyInfo customerCurrency = new CurrencyInfo(record.CustomerFacingCurrency);
            decimal customerAmount = customerCurrency.SmartParse(record.CustomerFacingAmount);
            decimal customerVat = customerCurrency.SmartParse(record.CustomerFacingAmountVat);

            CurrencyInfo balanceCurrency = new CurrencyInfo(record.Currency);
            decimal balanceAmount = balanceCurrency.SmartParse(record.Amount);
            decimal balanceFee = balanceCurrency.SmartParse(record.Fee);

            TransactionAmounts customerFacingAmounts = new TransactionAmounts()
            {
                Currency = customerCurrency,
                Total = customerAmount.ZeroAsNull(),
                Vat = (-customerVat).ZeroAsNull(),
            };

            TransactionAmounts balanceAmounts = new TransactionAmounts()
            {
                Currency = balanceCurrency,
                Total = balanceAmount.ZeroAsNull(),
                Fee = (-balanceFee).ZeroAsNull(),
            };

            TransactionRecord transaction = new TransactionRecord()
            {
                Id = record.Id,
                Utc = utc,
                Source = new SourceProviderName("Stripe"),
                Description = record.Description,
                Type = record.Type,
                AmountsAccount = balanceAmounts,
                AmountsPayment = customerFacingAmounts,
                Country = LocaleInfo.Create(record.VatCountry),
                AffectsBalance = true,
                AffectsRevenue = AffectsRevenue(record),
            };

            return transaction;
        }

        private static bool AffectsRevenue(StripeBalanceCsvRecord record)
        {
            switch (record.Type)
            {
                case "payout":
                    return false;

                case "charge":
                case "adjustment":
                case "refund":
                case "stripe_fee":
                    return true;

                default:
                    throw new InvalidOperationException($"Unknown value in type column from Stripe: {record.Type}.");
            }
        }
    }
}