using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Reports.Model.Csv
{
    public static class StringCsvConverterExtensions
    {
        public static StringCsvRecord ToStringCsv(this TransactionRecord transaction)
        {
            if (transaction == TransactionRecord.Empty)
            {
                return StringCsvRecord.Empty;
            }

            return new StringCsvRecord()
            {
                Id = transaction.Id,
                Utc = transaction.Utc.ToString("u"),
                Record = transaction.ToString(),
            };
        }

        public static TransactionRecord ToTransaction(this StringCsvRecord record)
        {
            if (record.Equals(StringCsvRecord.Empty))
            {
                return TransactionRecord.Empty;
            }

            return TransactionRecord.Parse(record.Record);
        }
    }
}