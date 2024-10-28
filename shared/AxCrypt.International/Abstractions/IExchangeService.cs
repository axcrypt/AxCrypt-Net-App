using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.International.Abstractions
{
    public interface IExchangeService
    {
        IEnumerable<ExchangeRate> MonthlyAverage(DateMonth month);

        IEnumerable<ExchangeRate> Day(DateTime date);
    }
}