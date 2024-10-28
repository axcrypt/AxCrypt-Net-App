using AxCrypt.Reports.Abstractions;
using NLight.IO.Text;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Reports.Model.Csv
{
    public abstract class CsvRecord<T> : ICsvRecord<T>, IEquatable<T> where T : class, ICsvRecord<T>, new()
    {
        public abstract DateTime TimeStampUtc { get; }

        public virtual IEnumerable<string> Fields
        {
            get
            {
                List<string> fields = new List<string>();
                foreach (PropertyInfo property in Properties)
                {
                    fields.Add((string)property.GetValue(this));
                }

                return fields;
            }
        }

        public virtual IEnumerable<string> Header
        {
            get
            {
                List<string> header = new List<string>();
                foreach (string propertyName in PropertyNames)
                {
                    header.Add(ColumnName(propertyName));
                }

                return header;
            }
        }

        public virtual T Fill(IDataRecord row)
        {
            foreach (PropertyInfo property in Properties)
            {
                property.SetValue(this, Column(row, property.Name));
            }
            return Invariant();
        }

        protected virtual T Invariant()
        {
            return this as T;
        }

        public bool Equals(T other)
        {
            if (ReferenceEquals(other, null) || GetType() != other.GetType())
            {
                return false;
            }

            return EqualityComparer(other);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as T);
        }

        public override int GetHashCode()
        {
            int hashCode = 0;
            foreach (PropertyInfo property in Properties)
            {
                hashCode ^= property.GetValue(this).GetHashCode();
            }

            return hashCode;
        }

        protected virtual bool EqualityComparer(T other)
        {
            foreach (PropertyInfo property in Properties)
            {
                if (!property.GetValue(this).Equals(property.GetValue(other)))
                {
                    return false;
                }
            }

            return true;
        }

        private string Column(IDataRecord row, string propertyName)
        {
            string columnName = ColumnName(propertyName);

            object columnValue = row[columnName];
            if (columnValue is DBNull)
            {
                return string.Empty;
            }
            return (string)columnValue;
        }

        private string ColumnName(string propertyName)
        {
            return ((CsvColumnAttribute)Attribute.GetCustomAttribute(GetType().GetProperty(propertyName), typeof(CsvColumnAttribute))).CsvColumn;
        }

        private IEnumerable<string> PropertyNames
        {
            get
            {
                List<string> csvColumnPropertyNames = Properties.Select(p => p.Name).ToList();
                return csvColumnPropertyNames;
            }
        }

        private IEnumerable<PropertyInfo> Properties
        {
            get
            {
                return GetType().GetProperties().Where(p => p.CustomAttributes.Any(c => c.AttributeType == typeof(CsvColumnAttribute)));
            }
        }
    }
}