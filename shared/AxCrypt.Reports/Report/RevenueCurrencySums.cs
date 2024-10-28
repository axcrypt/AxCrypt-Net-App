using AxCrypt.International;
using AxCrypt.International.Abstractions;
using AxCrypt.Reports.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AxCrypt.Reports.Report
{
    public class RevenueCurrencySums
    {
        public CurrencyAccount Account { get; private set; }

        public decimal Balance { get; private set; }

        public TransactionAmounts TotalSums { get; private set; }

        public TransactionAmounts SekSwedenSums { get; private set; }

        public TransactionAmounts SekEuSums { get; private set; }

        public TransactionAmounts SekExportSums { get; private set; }

        private DateMonth _month;

        public RevenueCurrencySums(CurrencyAccount account, DateMonth month)
        {
            Account = account;
            _month = month;

            TotalSums = new TransactionAmounts() { Currency = Account.Currency, Total = 0m, Fee = 0m, Vat = 0m, };
            SekSwedenSums = new TransactionAmounts() { Currency = CurrencyInfo.SEK, Total = 0m, Fee = 0m, Vat = 0m, };
            SekEuSums = new TransactionAmounts() { Currency = CurrencyInfo.SEK, Total = 0m, Fee = 0m, Vat = 0m, };
            SekExportSums = new TransactionAmounts() { Currency = CurrencyInfo.SEK, Total = 0m, Fee = 0m, Vat = 0m, };
        }

        public void Add(TransactionRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            CurrencyAccount recordAccount = new CurrencyAccount(record.Source, record.AmountsAccount.Currency);
            if (recordAccount != Account)
            {
                throw new ArgumentException($"Account must be {Account} but is {recordAccount}.", nameof(record));
            }

            AdjustBalance(record);

            if (!record.AffectsRevenue)
            {
                return;
            }

            IExchange paymentToAccountExchange = new MonthAverageRateExchange(record.AmountsPayment.Currency, record.AmountsAccount.Currency, new DateMonth(record.Utc));
            paymentToAccountExchange = paymentToAccountExchange.UseOrFromTransaction(record.AmountsPayment, record.AmountsAccount);

            TransactionAmounts accountAmonts = record.AmountsAccount.Merge(record.AmountsPayment, paymentToAccountExchange);
            TotalSums += accountAmonts;

            IExchange accountToSekExchange = new MonthAverageRateExchange(record.AmountsAccount.Currency, CurrencyInfo.SEK, new DateMonth(record.Utc));
            accountToSekExchange = accountToSekExchange.UseOrFromTransaction(record.AmountsAccount, record.AmountsPayment);

            TransactionAmounts sekAmounts = record.AmountsAccount.Exchange(accountToSekExchange);

            IExchange paymentToSekExchange = new MonthAverageRateExchange(record.AmountsPayment.Currency, CurrencyInfo.SEK, new DateMonth(record.Utc));
            paymentToSekExchange = paymentToSekExchange.UseOrFromTransaction(record.AmountsPayment, record.AmountsAccount);

            sekAmounts = sekAmounts.Merge(record.AmountsPayment, paymentToSekExchange);

            if (sekAmounts.Vat.HasValue && record.Country == LocaleInfo.SE)
            {
                SekSwedenSums += sekAmounts;
            }
            if (sekAmounts.Vat.HasValue && record.Country != LocaleInfo.SE)
            {
                SekEuSums += sekAmounts;
            }
            if (!sekAmounts.Vat.HasValue)
            {
                SekExportSums += sekAmounts;
            }
        }

        private void AdjustBalance(TransactionRecord record)
        {
            if (!record.AffectsBalance)
            {
                return;
            }
            if (record.AccountBalance.HasValue)
            {
                Balance = record.AccountBalance.Value;
                return;
            }
            if (record.AmountsAccount.Total.HasValue)
            {
                Balance += record.AmountsAccount.Total.Value + record.AmountsAccount.Fee.GetValueOrDefault() + record.AmountsAccount.Vat.GetValueOrDefault();
                return;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return TotalSums.Total == 0m;
            }
        }
    }
}