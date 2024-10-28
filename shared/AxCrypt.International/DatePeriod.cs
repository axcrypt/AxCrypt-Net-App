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
    public class DatePeriod : IDatePeriod
    {
        public DatePeriod()
            : this(DateTime.MinValue, DateTime.MaxValue)
        {
        }

        public DatePeriod(DateTime oneDay)
            : this(oneDay, oneDay)
        {
        }

        public DatePeriod(DateTime firstDay, DateTime lastDay)
        {
            if (firstDay > lastDay)
            {
                throw new ArgumentException("First day cannot be after last day.", nameof(firstDay));
            }

            FirstDay = firstDay.Date;
            LastDay = lastDay.Date;
        }

        public DateTime FirstDay { get; }

        public DateTime LastDay { get; }

        public IEnumerator<DateMonth> GetEnumerator()
        {
            for (DateTime month = FirstDay; month <= LastDay; month = month.AddMonths(1))
            {
                yield return new DateMonth(month);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool In(DateTime day)
        {
            day = day.Date;
            return FirstDay <= day && LastDay >= day;
        }

        public override string ToString()
        {
            if (FirstDay == LastDay)
            {
                return FirstDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            if (FirstDay.Day != 1 || LastDay.AddDays(1).Day != 1)
            {
                return $"{FirstDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}--{LastDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
            }
            if (FirstDay.Year == LastDay.Year && FirstDay.Month == LastDay.Month)
            {
                return FirstDay.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            }
            return $"{FirstDay.ToString("yyyy-MM", CultureInfo.InvariantCulture)}--{LastDay.ToString("yyyy-MM", CultureInfo.InvariantCulture)}";
        }
    }
}