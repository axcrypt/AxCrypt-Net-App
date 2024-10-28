using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.International.Abstractions
{
    public interface IDatePeriod : IEnumerable<DateMonth>
    {
        DateTime FirstDay { get; }

        DateTime LastDay { get; }

        bool In(DateTime day);
    }
}