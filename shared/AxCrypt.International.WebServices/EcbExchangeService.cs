using AxCrypt.Abstractions;
using AxCrypt.International.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.International.WebServices
{
    public class EcbExchangeService : IExchangeService
    {
        private const string SOURCE_NAME = "ECB";

        public IEnumerable<ExchangeRate> MonthlyAverage(DateMonth month)
        {
            IList<ExchangeRate> rates = Rates(month);

            List<ExchangeRate> averageRates = new List<ExchangeRate>();
            foreach (var currency in rates.GroupBy(e => e.From))
            {
                decimal averageRate = currency.Average(c => c.Rate);
                averageRates.Add(new ExchangeRate(SOURCE_NAME, month, ExchangeRateSamplePeriod.Month, currency.Key, new CurrencyInfo("EUR"), averageRate));
            }

            return averageRates;
        }

        public IEnumerable<ExchangeRate> Day(DateTime date)
        {
            IList<ExchangeRate> rates = Rates(new DatePeriod(date));

            return rates;
        }

        public IList<ExchangeRate> Rates(IDatePeriod period)
        {
            IList<ExchangeRate> actualRates = GetDailyRates(period);

            List<ExchangeRate> effectivePeriodRates = new List<ExchangeRate>();
            for (DateTime effectiveDay = period.FirstDay; effectiveDay <= period.LastDay; effectiveDay = effectiveDay.AddDays(1))
            {
                IEnumerable<ExchangeRate> effectiveDayRates = GetEffectiveDayRate(actualRates, effectiveDay);
                effectivePeriodRates.AddRange(effectiveDayRates);
            }

            return effectivePeriodRates;
        }

        private static IEnumerable<ExchangeRate> GetEffectiveDayRate(IList<ExchangeRate> actualRates, DateTime effectiveDay)
        {
            IEnumerable<ExchangeRate> actualDayRates;
            DateTime actualDay = effectiveDay;
            while (true)
            {
                actualDayRates = actualRates.Where(r => r.ValidityPeriod.FirstDay == actualDay && r.ValidityPeriod.LastDay == actualDay);
                if (actualDayRates.Any())
                {
                    return actualDayRates.Select(r => new ExchangeRate(SOURCE_NAME, new DatePeriod(effectiveDay), ExchangeRateSamplePeriod.Day, r.From, r.To, r.Rate));
                }
                actualDay = actualDay.AddDays(-1);
            }
        }

        protected IList<ExchangeRate> GetDailyRates(IDatePeriod period)
        {
            Uri url = GetHistoricalUrl(period.FirstDay);
            List<ExchangeRate> dailyRates = new List<ExchangeRate>();
            using (WebResponse response = GetEcbResponse(url))
            {
                if (response == null)
                {
                    return dailyRates;
                }
                ReadEcbXml(dailyRates, response);
            }

            return dailyRates;
        }

        private static Uri GetHistoricalUrl(DateTime fromUtc)
        {
            DateTime today = New<INow>().Utc.ToLocalTime().Date;
            if (fromUtc.Date == today)
            {
                return new Uri("http://www.ecb.int/stats/eurofxref/eurofxref-daily.xml");
            }
            if (fromUtc > today.AddDays(-90))
            {
                return new Uri("http://www.ecb.europa.eu/stats/eurofxref/eurofxref-hist-90d.xml");
            }
            return new Uri("http://www.ecb.europa.eu/stats/eurofxref/eurofxref-hist.xml");
        }

        private static void ReadEcbXml(IList<ExchangeRate> dailyRates, WebResponse response)
        {
            DateTime dailyDay = DateTime.MinValue;
            using (XmlReader reader = XmlReader.Create(response.GetResponseStream(), new XmlReaderSettings() { CloseInput = true, }))
            {
                reader.Read();
                while (!reader.EOF)
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            dailyDay = ProcessElement(dailyRates, dailyDay, reader);
                            break;

                        default:
                            break;
                    }
                    reader.Read();
                }
            }
        }

        private static DateTime ProcessElement(IList<ExchangeRate> dailyRates, DateTime dailyDay, XmlReader reader)
        {
            if (reader.Name != "Cube")
            {
                return dailyDay;
            }

            if (!reader.HasAttributes)
            {
                return dailyDay;
            }

            decimal rate = 0m;
            string currency = string.Empty;
            while (reader.MoveToNextAttribute())
            {
                switch (reader.Name)
                {
                    case "time":
                        if (!DateTime.TryParse(reader.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out dailyDay))
                        {
                            throw new InvalidOperationException("Unexpected date format from ECB.");
                        }
                        break;

                    case "currency":
                        currency = reader.Value;
                        break;

                    case "rate":
                        if (!Decimal.TryParse(reader.Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out rate))
                        {
                            throw new InvalidOperationException("Unexpected rate format from ECB.");
                        }
                        rate = 1 / rate;
                        break;

                    default:
                        break;
                }
            }

            if (rate == 0 && currency == string.Empty)
            {
                return dailyDay;
            }

            dailyRates.Add(new ExchangeRate(SOURCE_NAME, new DatePeriod(dailyDay), ExchangeRateSamplePeriod.Day, new CurrencyInfo(currency), new CurrencyInfo("EUR"), rate));
            return dailyDay;
        }

        private WebResponse GetEcbResponse(Uri url)
        {
            try
            {
                WebRequest request = HttpWebRequest.Create(url);
                request.Timeout = 5000;
                return request.GetResponse();
            }
            catch (WebException)
            {
                return null;
            }
        }
    }
}