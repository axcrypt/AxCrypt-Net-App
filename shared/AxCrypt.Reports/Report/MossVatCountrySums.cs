using AxCrypt.International;
using AxCrypt.International.Abstractions;
using AxCrypt.Reports.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AxCrypt.Reports.Report
{
    public class MossVatCountrySums
    {
        public MossVatCountrySums(LocaleInfo country, DateMonthPeriod months)
        {
            Country = country;
            Months = months;
            SekSums = new TransactionAmounts() { Currency = new CurrencyInfo("SEK"), };
            EurSums = new TransactionAmounts() { Currency = new CurrencyInfo("EUR"), };
        }

        public LocaleInfo Country { get; set; }

        public DateMonthPeriod Months { get; set; }

        public TransactionAmounts SekSums { get; set; }

        public TransactionAmounts EurSums { get; set; }

        public void Add(TransactionRecord record)
        {
            if (!record.AffectsRevenue)
            {
                return;
            }

            IExchange paymentToEurosExchange = new DayRateExchange(record.AmountsPayment.Currency, CurrencyInfo.EUR, Months.LastDay);
            TransactionAmounts eurAmounts = record.AmountsPayment.Exchange(paymentToEurosExchange);

            IExchange accountToEurosExchange = new DayRateExchange(record.AmountsAccount.Currency, CurrencyInfo.EUR, Months.LastDay);
            eurAmounts = eurAmounts.Merge(record.AmountsAccount, accountToEurosExchange);

            if (!eurAmounts.Vat.HasValue || record.Country == LocaleInfo.SE)
            {
                return;
            }

            IExchange accountToSekExchange = new MonthAverageRateExchange(record.AmountsAccount.Currency, CurrencyInfo.SEK, new DateMonth(record.Utc));
            accountToSekExchange = accountToSekExchange.UseOrFromTransaction(record.AmountsAccount, record.AmountsPayment);
            TransactionAmounts sekAmounts = record.AmountsAccount.Exchange(accountToSekExchange);

            IExchange paymentToSekExchange = new MonthAverageRateExchange(record.AmountsPayment.Currency, CurrencyInfo.SEK, new DateMonth(record.Utc));
            paymentToSekExchange = paymentToSekExchange.UseOrFromTransaction(record.AmountsPayment, record.AmountsAccount);
            sekAmounts = sekAmounts.Merge(record.AmountsPayment, paymentToSekExchange);

            EurSums += eurAmounts;
            SekSums += sekAmounts;
        }

        public bool IsEmpty
        {
            get
            {
                return !EurSums.Total.HasValue;
            }
        }
    }
}