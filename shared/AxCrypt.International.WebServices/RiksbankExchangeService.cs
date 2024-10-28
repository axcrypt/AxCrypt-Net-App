using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using AxCrypt.International.Abstractions;
using AxCrypt.International.WebServices.SweaWebService;

namespace AxCrypt.International.WebServices
{
    /// <summary>
    ///
    /// </summary>
    /// <remarks>
    /// https://www.riksbank.se/sv/statistik/sok-rantor--valutakurser/oppet-api/
    /// https://www.riksbank.se/sv/Rantor-och-valutakurser/serier-for-webbservices/
    /// </remarks>
    public class RiksbankExchangeService : IExchangeService
    {
        private const string SOURCE_NAME = "RB";

        private static readonly CustomBinding _binding = CreateBinding();

        private static CustomBinding CreateBinding()
        {
            CustomBinding binding = new CustomBinding();
            binding.Elements.Add(new TextMessageEncodingBindingElement(MessageVersion.Soap11, Encoding.UTF8));
            binding.Elements.Add(new HttpsTransportBindingElement());

            return binding;
        }

        private static readonly EndpointAddress _address = new EndpointAddress("https://swea.riksbank.se:443/sweaWS/services/SweaWebServiceHttpSoap12Endpoint");

        public IEnumerable<ExchangeRate> MonthlyAverage(DateMonth month)
        {
            Result result;
            using (SweaWebServicePortTypeClient client = new SweaWebServicePortTypeClient(_binding, _address))
            {
                result = client.getMonthlyAverageExchangeRates(month.FirstDay.Year, month.FirstDay.Month, LanguageType.en);
            }

            return RatesFromResult(result, ExchangeRateSamplePeriod.Month, month);
        }

        public IEnumerable<ExchangeRate> Day(DateTime effectiveDate)
        {
            GroupSeries[] groupSeries;
            using (SweaWebServicePortTypeClient client = new SweaWebServicePortTypeClient(_binding, _address))
            {
                groupSeries = client.getInterestAndExchangeNames(130, LanguageType.en);
            }

            DateTime actualDate = effectiveDate;
            while (true)
            {
                GroupSeries[] groupSeriesForDate = groupSeries.Where(g => g.datefrom <= actualDate && g.dateto.GetValueOrDefault(DateTime.MaxValue) >= actualDate).ToArray();
                SearchRequestParameters parameters = new SearchRequestParameters()
                {
                    avg = true,
                    datefrom = actualDate.Date,
                    dateto = actualDate.Date,
                    languageid = LanguageType.en,
                    searchGroupSeries = groupSeriesForDate.Select(gs => new SearchGroupSeries() { groupid = gs.groupid, seriesid = gs.seriesid, }).ToArray(),
                };
                Result result;
                using (SweaWebServicePortTypeClient client = new SweaWebServicePortTypeClient(_binding, _address))
                {
                    result = client.getInterestAndExchangeRates(parameters);
                }
                IEnumerable<ExchangeRate> rates = new ExchangeRate[0];
                if (result.groups != null)
                {
                    rates = RatesFromResult(result, ExchangeRateSamplePeriod.Day, new DatePeriod(effectiveDate));
                }
                if (rates.Count() == groupSeriesForDate.Count())
                {
                    return rates;
                }
                actualDate = actualDate.AddDays(-1);
            }
        }

        private static IEnumerable<ExchangeRate> RatesFromResult(Result result, ExchangeRateSamplePeriod samplePeriod, IDatePeriod period)
        {
            List<ExchangeRate> rates = new List<ExchangeRate>();
            foreach (ResultGroup group in result.groups)
            {
                if (group.groupid != "130")
                {
                    throw new InvalidOperationException($"Unexpected GroupId {group.groupid} ({group.groupname}) from Riksbanken.");
                }

                foreach (ResultSeries series in group.series)
                {
                    string currency = series.seriesname.Substring(series.seriesname.Length - 3, 3);
                    double? value = series.resultrows[0].average ?? series.resultrows[0].value;
                    if (!value.HasValue)
                    {
                        continue;
                    }
                    decimal rate = (decimal)value.Value;
                    decimal unit = (decimal)series.unit.GetValueOrDefault(1);

                    rates.Add(new ExchangeRate(SOURCE_NAME, period, samplePeriod, new CurrencyInfo(currency), new CurrencyInfo("SEK"), rate / unit));
                }
            }

            return rates;
        }
    }
}