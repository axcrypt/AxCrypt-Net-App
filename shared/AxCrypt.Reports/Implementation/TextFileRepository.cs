using AxCrypt.International;
using AxCrypt.International.Abstractions;
using AxCrypt.Reports.Abstractions;
using AxCrypt.Reports.Model;
using AxCrypt.Reports.Model.Csv;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Reports.Implementation
{
    public class TextFileRepository : IRepository
    {
        public IEnumerable<TransactionRecord> Load(PersistentName name, IDatePeriod period)
        {
            using (TextReader reader = New<ITextPersistence>().LoadFrom(name))
            {
                return CsvData<StringCsvRecord>.Load(reader, ',').Where(s => period.In(DateTime.ParseExact(s.Utc, "u", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal))).Select(s => s.ToTransaction()).ToList();
            }
        }

        public void Save(PersistentName name, IEnumerable<TransactionRecord> records)
        {
            IEnumerable<TransactionRecord> existingRecords;
            existingRecords = Load(name, new DatePeriod());

            IEnumerable<TransactionRecord> mergedRecords = records.Where(r => r != TransactionRecord.Empty).Union(existingRecords).ToList();
            IEnumerable<TransactionRecord> fixedRecords = FixupReferences(mergedRecords);
            using (TextWriter writer = New<ITextPersistence>().SaveTo(name))
            {
                CsvData<StringCsvRecord>.Save(fixedRecords.Select(t => t.ToStringCsv()), writer, ',');
            }
        }

        public void ClearAll(PersistentName name)
        {
            New<ITextPersistence>().ClearAll(name);
        }

        private IEnumerable<TransactionRecord> FixupReferences(IEnumerable<TransactionRecord> mergedRecords)
        {
            IEnumerable<TransactionRecord> countryLess = mergedRecords.Where(r => r.AffectsRevenue && r.Country == LocaleInfo.Empty && r.IdReference.Length > 0);
            List<TransactionRecord> fixUpped = new List<TransactionRecord>();

            foreach (TransactionRecord record in countryLess)
            {
                TransactionRecord reference = mergedRecords.Where(r => r.Id == record.IdReference && r.Country != LocaleInfo.Empty).FirstOrDefault();
                if (reference != null)
                {
                    record.Country = reference.Country;
                }

                fixUpped.Add(record);
            }

            return fixUpped.Union(mergedRecords);
        }
    }
}