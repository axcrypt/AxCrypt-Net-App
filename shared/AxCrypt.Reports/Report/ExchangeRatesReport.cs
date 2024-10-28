using AxCrypt.International;
using AxCrypt.International.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Reports.Report
{
    public class ExchangeRatesReport
    {
        private IDatePeriod _period;

        public ExchangeRatesReport(DateMonthPeriod months)
        {
            _period = months;
        }

        public ExchangeRatesReport(DateMonth month)
        {
            _period = month;
        }

        public IEnumerable<ExchangeRate> Report
        {
            get
            {
                List<ExchangeRate> rates = new List<ExchangeRate>();
                IEnumerable<ExchangeRate> periodRates;

                periodRates = LastDayRates(_period);
                rates.AddRange(periodRates);
                foreach (DateMonth month in _period)
                {
                    periodRates = MonthlyAverageRates(month);
                    rates.AddRange(periodRates);
                    periodRates = LastDayRates(month);
                    rates.AddRange(periodRates);
                }

                return rates.OrderBy(r => r.Source).ThenBy(r => r.From.ToString()).ThenBy(r => r.To.ToString()).ThenBy(r => r.ValidityPeriod.FirstDay);
            }
        }

        private static IEnumerable<ExchangeRate> MonthlyAverageRates(DateMonth month)
        {
            IExchangeService exchange = New<IExchangeService>();
            return exchange.MonthlyAverage(month).Select(r => new ExchangeRate(r.Source, r.ValidityPeriod, r.SamplePeriod, r.From, r.To, ExchangeCurrency.Precise(r.Rate)));
        }

        private static IEnumerable<ExchangeRate> LastDayRates(IDatePeriod period)
        {
            IExchangeService exchange = New<IExchangeService>();

            return exchange.Day(period.LastDay).Select(r => new ExchangeRate(r.Source, period, r.SamplePeriod, r.From, r.To, ExchangeCurrency.Precise(r.Rate)));
        }

        public string Name
        {
            get
            {
                return $"{_period}-Rates";
            }
        }
    }
}