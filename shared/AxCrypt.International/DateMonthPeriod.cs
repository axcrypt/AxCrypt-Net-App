using AxCrypt.International.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.International
{
    public class DateMonthPeriod : IDatePeriod
    {
        public DateTime FirstDay { get; }

        public DateTime LastDay { get; }

        public DateMonthPeriod(DateTime dayInFirstMonth, DateTime dayInLastMonth)
            : this(new DateMonth(dayInFirstMonth), new DateMonth(dayInLastMonth))
        {
        }

        public DateMonthPeriod(DateMonth oneMonth)
            : this(oneMonth, oneMonth)
        {
        }

        public DateMonthPeriod(DateMonth firstMonth, DateMonth lastMonth)
        {
            if (firstMonth == null)
            {
                throw new ArgumentNullException(nameof(firstMonth));
            }
            if (lastMonth == null)
            {
                throw new ArgumentNullException(nameof(lastMonth));
            }
            if (firstMonth.FirstDay > lastMonth.FirstDay)
            {
                throw new ArgumentException("First month must be before last month.");
            }

            FirstDay = firstMonth.FirstDay;
            LastDay = lastMonth.LastDay;
        }

        public bool In(DateTime day)
        {
            day = day.Date;
            return FirstDay <= day && LastDay >= day;
        }

        public IEnumerator<DateMonth> GetEnumerator()
        {
            DateTime month = new DateTime(FirstDay.Year, FirstDay.Month, 1);
            DateTime lastMonth = new DateTime(LastDay.Year, LastDay.Month, 1);
            while (month <= lastMonth)
            {
                yield return new DateMonth(month);

                month = month.AddMonths(1);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return $"{FirstDay.ToString("yyyy-MM", CultureInfo.InvariantCulture)}--{LastDay.ToString("yyyy-MM", CultureInfo.InvariantCulture)}";
        }
    }
}