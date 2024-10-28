using AxCrypt.Abstractions;
using AxCrypt.International.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.International
{
    public class DateMonth : IDatePeriod
    {
        public DateTime FirstDay { get; }

        public DateTime LastDay { get; }

        public DateMonth(DateTime dateInMonth)
            : this(dateInMonth.Year, dateInMonth.Month)
        {
        }

        public DateMonth(int year, int month)
        {
            FirstDay = new DateTime(year, month, 1);
            LastDay = FirstDay.AddMonths(1).AddDays(-1);
            if (!IsValid(FirstDay))
            {
                throw new ArgumentOutOfRangeException(nameof(month), "A month must be completely in the past in local time zone.");
            }
        }

        public bool In(DateTime day)
        {
            day = day.Date;
            return FirstDay <= day && LastDay >= day;
        }

        public static bool IsValid(DateTime dayUtc)
        {
            DateTime utcNow = New<INow>().Utc.Date;
            DateTime firstDayOfThisMonth = new DateTime(utcNow.Year, utcNow.Month, 1);
            if (dayUtc.Date >= firstDayOfThisMonth)
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return FirstDay.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        }

        public IEnumerator<DateMonth> GetEnumerator()
        {
            yield return new DateMonth(FirstDay);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}