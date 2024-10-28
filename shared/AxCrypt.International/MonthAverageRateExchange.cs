using AxCrypt.International.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AxCrypt.International
{
    public class MonthAverageRateExchange : IExchange
    {
        private ExchangeCurrency _currencyExchange;

        private DateMonth _month;

        public MonthAverageRateExchange(CurrencyInfo from, CurrencyInfo to, DateMonth month)
        {
            From = from;
            To = to;
            _currencyExchange = new ExchangeCurrency(to);
            _month = month;
        }

        public CurrencyInfo From { get; }

        public CurrencyInfo To { get; }

        public decimal? Precise(decimal? from)
        {
            if (!from.HasValue)
            {
                return from;
            };

            return _currencyExchange.Monthly(from.Value, From, _month);
        }

        public decimal? Round(decimal? from)
        {
            decimal? precise = Precise(from);

            if (!precise.HasValue)
            {
                return precise;
            }

            return To.Round(precise.Value);
        }
    }
}