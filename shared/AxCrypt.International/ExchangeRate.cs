using AxCrypt.International.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.International
{
    public class ExchangeRate
    {
        public ExchangeRate(string source, IDatePeriod validityPeriod, ExchangeRateSamplePeriod sample, CurrencyInfo from, CurrencyInfo to, decimal rate)
        {
            Source = source;
            ValidityPeriod = validityPeriod;
            SamplePeriod = sample;
            From = from;
            To = to;
            Rate = rate;
        }

        public string Source { get; }

        public IDatePeriod ValidityPeriod { get; }

        public ExchangeRateSamplePeriod SamplePeriod { get; }

        public CurrencyInfo From { get; }

        public CurrencyInfo To { get; }

        public decimal Rate { get; }
    }
}