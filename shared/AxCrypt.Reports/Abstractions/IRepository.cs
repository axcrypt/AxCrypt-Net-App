using AxCrypt.International.Abstractions;
using AxCrypt.Reports.Model;
using System.Collections.Generic;

namespace AxCrypt.Reports.Abstractions
{
    public interface IRepository
    {
        void Save(PersistentName name, IEnumerable<TransactionRecord> records);

        IEnumerable<TransactionRecord> Load(PersistentName name, IDatePeriod period);

        void ClearAll(PersistentName name);
    }
}