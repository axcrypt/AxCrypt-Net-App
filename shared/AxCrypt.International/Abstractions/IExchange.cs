using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.International.Abstractions
{
    public interface IExchange
    {
        CurrencyInfo To { get; }

        CurrencyInfo From { get; }

        decimal? Precise(decimal? from);

        decimal? Round(decimal? from);
    }
}