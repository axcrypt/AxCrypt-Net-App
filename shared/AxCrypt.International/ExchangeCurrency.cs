using AxCrypt.Abstractions;
using AxCrypt.International.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.International
{
    public class ExchangeCurrency
    {
        private CurrencyInfo _to;

        public ExchangeCurrency(CurrencyInfo to)
        {
            _to = to;
        }

        public decimal Monthly(decimal amount, CurrencyInfo from, DateMonth month)
        {
            return MonthlyRate(from, month) * amount;
        }

        public decimal Daily(decimal amount, CurrencyInfo from, DateTime day)
        {
            return DailyRate(from, day) * amount;
        }

        public decimal MonthlyRate(CurrencyInfo from, DateMonth month)
        {
            IEnumerable<ExchangeRate> rates = New<IExchangeService>().MonthlyAverage(month);

            return Rate(rates, from);
        }

        public decimal DailyRate(CurrencyInfo from, DateTime day)
        {
            IEnumerable<ExchangeRate> rates = New<IExchangeService>().Day(day);

            return Rate(rates, from);
        }

        private decimal Rate(IEnumerable<ExchangeRate> rates, CurrencyInfo from)
        {
            if (from == _to)
            {
                return 1m;
            }

            IEnumerable<ExchangeRate> fromRates = rates.Where(r => r.From == from);
            ExchangeRate rate = fromRates.FirstOrDefault(r => r.To == _to);
            if (rate != null)
            {
                return Precise(rate.Rate);
            }
            rate = rates.Where(r => r.From == _to && r.To == from).FirstOrDefault();
            if (rate != null)
            {
                return Precise(1 / rate.Rate);
            }
            foreach (ExchangeRate bridgeRate in fromRates)
            {
                rate = rates.FirstOrDefault(r => r.From == bridgeRate.To && r.To == _to);
                if (rate != null)
                {
                    return Precise(bridgeRate.Rate * rate.Rate);
                }
            }
            throw new InvalidOperationException($"Can't change {from} to {_to}");
        }

        public static decimal Precise(decimal rate)
        {
            int decimals = 4;

            int magnitude = (int)Math.Floor(Math.Log10((double)rate));
            if (magnitude < 0)
            {
                decimals += -magnitude;
            }

            return Math.Round(rate, decimals, MidpointRounding.ToEven);
        }
    }
}