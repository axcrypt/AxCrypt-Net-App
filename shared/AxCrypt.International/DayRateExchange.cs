using AxCrypt.International.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.International
{
    public class DayRateExchange : IExchange
    {
        private ExchangeCurrency _currencyExchange;

        private DateTime _day;

        public CurrencyInfo To { get; }

        public CurrencyInfo From { get; }

        public DayRateExchange(CurrencyInfo from, CurrencyInfo to, DateTime day)
        {
            From = from;
            To = to;
            _currencyExchange = new ExchangeCurrency(to);
            _day = day;
        }

        public decimal? Precise(decimal? from)
        {
            if (!from.HasValue)
            {
                return from;
            };

            return _currencyExchange.Daily(from.Value, From, _day);
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