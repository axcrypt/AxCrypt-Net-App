using AxCrypt.Abstractions;
using AxCrypt.Common;
using AxCrypt.International.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.International
{
    public class CachedExchangeService : IExchangeService
    {
        private readonly IExchangeService _service;

        private readonly CacheKey _masterCacheKey;

        private readonly CacheKey _dayRatesKey;

        private readonly CacheKey _monthlyAverageKey;

        public CachedExchangeService(IExchangeService service)
        {
            _service = service;
            _masterCacheKey = new CacheKey(nameof(CachedExchangeService));
            _dayRatesKey = _masterCacheKey.Subkey(nameof(Day));
            _monthlyAverageKey = _masterCacheKey.Subkey(nameof(MonthlyAverage));
        }

        public IEnumerable<ExchangeRate> Day(DateTime date)
        {
            return New<ICache>().GetItem(_dayRatesKey.Subkey(date.ToString("u")), () => _service.Day(date));
        }

        public IEnumerable<ExchangeRate> MonthlyAverage(DateMonth month)
        {
            return New<ICache>().GetItem(_monthlyAverageKey.Subkey(month.FirstDay.ToString("u")), () => _service.MonthlyAverage(month));
        }
    }
}