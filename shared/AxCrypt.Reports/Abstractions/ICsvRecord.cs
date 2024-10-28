using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Reports.Abstractions
{
    public interface ICsvRecord<T>
    {
        DateTime TimeStampUtc { get; }

        T Fill(IDataRecord reader);

        IEnumerable<string> Header { get; }

        IEnumerable<string> Fields { get; }
    }
}