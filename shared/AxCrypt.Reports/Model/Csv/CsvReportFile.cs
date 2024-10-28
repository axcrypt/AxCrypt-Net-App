using AxCrypt.International;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Reports.Model.Csv
{
    public class CsvReportFile
    {
        public string Name { get; }

        public string Csv { get; }

        public CsvReportFile(string baseName, string csv)
        {
            Csv = csv;
            Name = $"{baseName}.csv";
        }
    }
}