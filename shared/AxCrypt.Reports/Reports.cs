using AxCrypt.International;
using AxCrypt.Reports.Abstractions;
using AxCrypt.Reports.Report;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AxCrypt.Reports
{
    public class Reports
    {
        public static readonly PersistentName PersistentName = new PersistentName("AxReports");

        public DateMonthPeriod Months { get; }

        public Reports(DateMonthPeriod months)
        {
            Months = months;
        }

        public IEnumerable<MossReport> MossReports()
        {
            MossReport report = new MossReport(Months, PersistentName);

            return new MossReport[] { report };
        }

        public IEnumerable<RevenueReport> RevenueReports()
        {
            List<RevenueReport> reports = new List<RevenueReport>();
            foreach (DateMonth month in Months)
            {
                RevenueReport report = new RevenueReport(month, PersistentName);
                reports.Add(report);
            }

            return reports;
        }

        public IEnumerable<TransactionReport> TransactionReports()
        {
            List<TransactionReport> reports = new List<TransactionReport>();
            foreach (DateMonth month in Months)
            {
                TransactionReport report = new TransactionReport(month, PersistentName);
                reports.Add(report);
            }

            return reports;
        }

        public IEnumerable<ExchangeRatesReport> ExchangeRatesReports()
        {
            List<ExchangeRatesReport> reports = new List<ExchangeRatesReport>();

            ExchangeRatesReport report = new ExchangeRatesReport(Months);

            return new ExchangeRatesReport[] { report, };
        }
    }
}