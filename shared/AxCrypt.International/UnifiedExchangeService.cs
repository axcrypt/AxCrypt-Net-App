using AxCrypt.International.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.International
{
    /// <summary>
    /// This class satisfies the requirements for Swedish VAT currency conversion: http://www4.skatteverket.se/rattsligvagledning/edition/2014.4/321577.html ,
    /// provided that the first service is from Riksbank, and the second from ECB.
    /// </summary>
    public class UnifiedExchangeService : IExchangeService
    {
        private List<IExchangeService> _services;

        public UnifiedExchangeService(params IExchangeService[] services)
        {
            _services = services.ToList();
        }

        public IEnumerable<ExchangeRate> Day(DateTime date)
        {
            List<ExchangeRate> unified = new List<ExchangeRate>();

            foreach (IExchangeService service in _services)
            {
                Unify(unified, service.Day(date));
            }

            return unified;
        }

        public IEnumerable<ExchangeRate> MonthlyAverage(DateMonth month)
        {
            List<ExchangeRate> unified = new List<ExchangeRate>();

            foreach (IExchangeService service in _services)
            {
                Unify(unified, service.MonthlyAverage(month));
            }

            return unified;
        }

        private static void Unify(List<ExchangeRate> unified, IEnumerable<ExchangeRate> rates)
        {
            foreach (ExchangeRate rate in rates)
            {
                if (unified.Any(r => r.From == rate.From))
                {
                    continue;
                }
                ExchangeRate reverseRate = unified.FirstOrDefault(r => r.To == rate.From && r.From == rate.To);
                if (reverseRate != null)
                {
                    unified.Add(new ExchangeRate(rate.Source, rate.ValidityPeriod, rate.SamplePeriod, rate.From, rate.To, 1 / reverseRate.Rate));
                    continue;
                }
                unified.Add(rate);
            }
        }
    }
}