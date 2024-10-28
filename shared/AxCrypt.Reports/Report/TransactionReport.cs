using AxCrypt.International;
using AxCrypt.International.Abstractions;
using AxCrypt.Reports.Abstractions;
using AxCrypt.Reports.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Reports.Report
{
    public class TransactionReport
    {
        public IDatePeriod Period { get; }

        private PersistentName _repository;

        public TransactionReport(DateMonthPeriod months, PersistentName repository)
        {
            Period = months;
            _repository = repository;
        }

        public TransactionReport(DateMonth month, PersistentName repository)
        {
            Period = month;
            _repository = repository;
        }

        public IEnumerable<TransactionRecord> Report
        {
            get
            {
                return TransactionRecords();
            }
        }

        public string Name
        {
            get
            {
                return $"{Period}-Transactions";
            }
        }

        public IEnumerable<TransactionRecord> TransactionRecords()
        {
            IEnumerable<TransactionRecord> records = New<IRepository>().Load(_repository, Period);
            return records;
        }
    }
}