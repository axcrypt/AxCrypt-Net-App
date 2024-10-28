using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Reports.Model.Csv
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CsvColumnAttribute : Attribute
    {
        public CsvColumnAttribute(string csvColumn)
        {
            CsvColumn = csvColumn;
        }

        public string CsvColumn { get; set; }
    }
}