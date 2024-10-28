using AxCrypt.International;
using AxCrypt.Reports.Abstractions;
using AxCrypt.Reports.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Reports.Report
{
    public class MossReport
    {
        public DateMonthPeriod Months { get; }

        private PersistentName _repository;

        public MossReport(DateMonthPeriod months, PersistentName repository)
        {
            Months = months;
            _repository = repository;
        }

        public IEnumerable<MossVatCountrySums> Report
        {
            get
            {
                return MossVatSums();
            }
        }

        public string Name
        {
            get
            {
                return $"{Months}-MossVat";
            }
        }

        public IEnumerable<MossVatCountrySums> MossVatSums()
        {
            IEnumerable<TransactionRecord> records = New<IRepository>().Load(_repository, Months);

            Dictionary<LocaleInfo, MossVatCountrySums> sums = new Dictionary<LocaleInfo, MossVatCountrySums>();

            foreach (TransactionRecord record in records)
            {
                MossVatCountrySums countrySum;
                if (!sums.TryGetValue(record.Country, out countrySum))
                {
                    countrySum = new MossVatCountrySums(record.Country, Months);
                    sums[record.Country] = countrySum;
                }

                countrySum.Add(record);
            }

            return sums.Values.Where(m => !m.IsEmpty).OrderBy(m => m.Country.CountryName).ToList();
        }
    }
}