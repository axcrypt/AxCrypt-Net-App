using AxCrypt.International;
using AxCrypt.Reports.Model.Csv;
using AxCrypt.Reports.Properties;
using AxCrypt.Reports.Report;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Reports
{
    public class ZipReports
    {
        public byte[] ZipReport(DateMonthPeriod months, params LocaleInfo[] locales)
        {
            if (months == null)
            {
                throw new ArgumentNullException(nameof(months));
            }
            if (locales == null)
            {
                throw new ArgumentNullException(nameof(locales));
            }
            if (locales.Length == 0)
            {
                throw new ArgumentException("At least one culture must be provided.", nameof(locales));
            }

            using (MemoryStream stream = new MemoryStream())
            {
                using (ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
                {
                    ZipArchiveEntry readMe = zip.CreateEntry("ReadMe.txt");
                    using (StreamWriter writer = new StreamWriter(readMe.Open()))
                    {
                        writer.Write(Resources.ReadMe);
                    }

                    foreach (LocaleInfo locale in locales)
                    {
                        CsvReports csvReports = new CsvReports(months, locale);
                        List<CsvReportFile> reportFiles = new List<CsvReportFile>(); ;

                        foreach (CsvReportFile report in csvReports.ReportFiles())
                        {
                            ZipArchiveEntry entry = zip.CreateEntry(Path.Combine(locale.CountryName, report.Name).Replace("\\", "/"));
                            using (StreamWriter writer = new StreamWriter(entry.Open()))
                            {
                                writer.Write(report.Csv);
                            }
                        }
                    }
                }
                return stream.ToArray();
            }
        }
    }
}