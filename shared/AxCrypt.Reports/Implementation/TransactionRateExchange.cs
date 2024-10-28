using AxCrypt.International;
using AxCrypt.International.Abstractions;
using AxCrypt.Reports.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AxCrypt.Reports.Implementation
{
    public class TransactionRateExchange : IExchange
    {
        private TransactionAmounts _from;

        private TransactionAmounts _to;

        public TransactionRateExchange(TransactionAmounts from, TransactionAmounts to)
        {
            _from = from ?? throw new ArgumentNullException(nameof(from));
            _to = to ?? throw new ArgumentNullException(nameof(to));
        }

        public CurrencyInfo To => _to.Currency;

        public CurrencyInfo From => _from.Currency;

        public decimal? Precise(decimal? from)
        {
            if (!from.HasValue)
            {
                return from;
            }

            return (_to.Total / _from.Total) * from;
        }

        public decimal? Round(decimal? from)
        {
            if (!from.HasValue)
            {
                return from;
            }

            return To.Round(Precise(from).Value);
        }
    }
}