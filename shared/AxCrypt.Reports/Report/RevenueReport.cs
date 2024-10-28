using AxCrypt.International;
using AxCrypt.Reports.Abstractions;
using AxCrypt.Reports.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Reports.Report
{
    public class RevenueReport
    {
        public DateMonth Month { get; }

        private PersistentName _repository;

        public RevenueReport(DateMonth month, PersistentName repository)
        {
            Month = month;
            _repository = repository;
        }

        public IEnumerable<RevenueCurrencySums> Report
        {
            get
            {
                return RevenueSums();
            }
        }

        public string Name
        {
            get
            {
                return $"{Month}-Revenue";
            }
        }

        private IEnumerable<RevenueCurrencySums> RevenueSums()
        {
            IEnumerable<TransactionRecord> records = New<IRepository>().Load(_repository, Month);

            Dictionary<CurrencyAccount, RevenueCurrencySums> sums = new Dictionary<CurrencyAccount, RevenueCurrencySums>();

            foreach (TransactionRecord record in records)
            {
                CurrencyAccount account = new CurrencyAccount(record.Source, record.AmountsAccount.Currency);
                RevenueCurrencySums currencySum;
                if (!sums.TryGetValue(account, out currencySum))
                {
                    currencySum = new RevenueCurrencySums(account, Month);
                    sums[account] = currencySum;
                }

                currencySum.Add(record);
            }

            IEnumerable<IGrouping<CurrencyAccount, RevenueCurrencySums>> groups = sums.Values.Where(r => !r.IsEmpty).GroupBy(r => r.Account);
            IEnumerable<RevenueCurrencySums> stripeSums = groups.Where(g => g.Key.Source == SourceProviderName.Stripe).SelectMany(g => g).OrderBy(r => r.Account.Currency.ToString());
            IEnumerable<RevenueCurrencySums> payPalSums = groups.Where(g => g.Key.Source == SourceProviderName.PayPal).SelectMany(g => g).OrderBy(r => r.Account.Currency.ToString());

            return stripeSums.Concat(payPalSums).ToList();
        }
    }
}