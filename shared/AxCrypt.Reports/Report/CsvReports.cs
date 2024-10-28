using AxCrypt.International;
using AxCrypt.Reports.Model.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AxCrypt.Reports.Report
{
    public class CsvReports
    {
        private Reports _reports;

        private LocaleInfo _locale;

        public CsvReports(DateMonthPeriod months, LocaleInfo locale)
        {
            _reports = new Reports(months);
            _locale = locale;
        }

        public IEnumerable<CsvReportFile> ReportFiles()
        {
            List<CsvReportFile> reportFiles = new List<CsvReportFile>(); ;

            reportFiles.AddRange(RevenueReportFiles());
            reportFiles.AddRange(MossVatReportFiles());
            reportFiles.AddRange(TransactionReportFiles());
            reportFiles.AddRange(ExchangeRatesReportFiles());

            return reportFiles;
        }

        public IEnumerable<string> Revenue()
        {
            return RevenueReportFiles().Select(r => r.Csv);
        }

        public IEnumerable<CsvReportFile> RevenueReportFiles()
        {
            List<CsvReportFile> csvReportFiles = new List<CsvReportFile>();

            IEnumerable<RevenueReport> reports = _reports.RevenueReports();

            using (StringWriter writer = new StringWriter())
            {
                List<RevenueReportCsvRecord> csvRecords = new List<RevenueReportCsvRecord>();
                foreach (RevenueReport report in reports)
                {
                    csvRecords.AddRange(report.Report.ToCsv(_locale, report.Month));
                }

                CsvData<RevenueReportCsvRecord>.Save(csvRecords, writer, _locale.Delimiter);
                CsvReportFile reportFile = new CsvReportFile($"{_reports.Months}-Revenue", writer.ToString());
                csvReportFiles.Add(reportFile);
            }

            return csvReportFiles;
        }

        public IEnumerable<string> MossVat()
        {
            return MossVatReportFiles().Select(r => r.Csv);
        }

        public IEnumerable<CsvReportFile> MossVatReportFiles()
        {
            List<CsvReportFile> csvReportFiles = new List<CsvReportFile>();

            IEnumerable<MossReport> reports = _reports.MossReports();
            foreach (MossReport report in reports)
            {
                using (StringWriter writer = new StringWriter())
                {
                    CsvData<MossReportCsvRecord>.Save(report.Report.ToCsv(_locale, report.Months), writer, _locale.Delimiter);
                    CsvReportFile reportFile = new CsvReportFile(report.Name, writer.ToString());
                    csvReportFiles.Add(reportFile);
                }
            }

            return csvReportFiles;
        }

        public IEnumerable<CsvReportFile> TransactionReportFiles()
        {
            List<CsvReportFile> csvReportFiles = new List<CsvReportFile>();

            using (StringWriter writer = new StringWriter())
            {
                List<TransactionReportCsvRecord> csvRecords = new List<TransactionReportCsvRecord>();
                IEnumerable<TransactionReport> reports = _reports.TransactionReports();
                foreach (TransactionReport report in reports)
                {
                    csvRecords.AddRange(report.Report.ToCsv(_locale));
                }

                CsvData<TransactionReportCsvRecord>.Save(csvRecords, writer, _locale.Delimiter);
                CsvReportFile reportFile = new CsvReportFile($"{_reports.Months}-Transactions", writer.ToString());
                csvReportFiles.Add(reportFile);
            }

            return csvReportFiles;
        }

        public IEnumerable<CsvReportFile> ExchangeRatesReportFiles()
        {
            List<CsvReportFile> csvReportFiles = new List<CsvReportFile>();

            using (StringWriter writer = new StringWriter())
            {
                List<ExchangeRateCsvRecord> csvRecords = new List<ExchangeRateCsvRecord>();
                IEnumerable<ExchangeRatesReport> reports = _reports.ExchangeRatesReports();
                foreach (ExchangeRatesReport report in reports)
                {
                    csvRecords.AddRange(report.Report.ToCsv(_locale));
                }
                CsvData<ExchangeRateCsvRecord>.Save(csvRecords, writer, _locale.Delimiter);
                CsvReportFile reportFile = new CsvReportFile($"{_reports.Months}-Rates", writer.ToString());
                csvReportFiles.Add(reportFile);
            }

            return csvReportFiles;
        }
    }
}