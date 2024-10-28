using AxCrypt.Reports.Abstractions;
using NLight.IO.Text;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace AxCrypt.Reports.Model.Csv
{
    public static class CsvData<T> where T : ICsvRecord<T>, new()
    {
        public static IEnumerable<T> Load(TextReader reader, char delimiter)
        {
            if (reader == null || reader.Peek() == -1)
            {
                return new T[0];
            }

            List<T> records = new List<T>();
            using (IDataReader dataReader = GetReader(reader, delimiter))
            {
                while (dataReader.Read())
                {
                    records.Add(new T().Fill(dataReader));
                }
            }

            return records.OrderBy(r => r.TimeStampUtc).ToList();
        }

        private static IDataReader GetReader(TextReader reader, char delimiter)
        {
            DelimitedRecordReader recordReader = new DelimitedRecordReader(reader)
            {
                DelimiterCharacter = delimiter,
            };

            if (recordReader.ReadColumnHeaders() != ReadResult.Success)
            {
                throw new InvalidOperationException("Failed reading column headers");
            }

            return recordReader;
        }

        public static void Save(IEnumerable<T> records, TextWriter writer, char delimiter)
        {
            DelimitedRecordWriter recordWriter = new DelimitedRecordWriter(writer)
            {
                DelimiterCharacter = delimiter,
            };
            recordWriter.WriteRecord(new T().Header);
            foreach (T record in records)
            {
                recordWriter.WriteRecord(record.Fields);
            }
        }

        public static bool CanReadRecords(string headerRow)
        {
            foreach (string header in new T().Header)
            {
                if (!headerRow.Contains(header))
                {
                    return false;
                }
            }

            return true;
        }
    }
}