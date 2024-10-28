using AxCrypt.International;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Reports.Model
{
    public class CurrencyAccount : IEquatable<CurrencyAccount>
    {
        public SourceProviderName Source { get; }

        public CurrencyInfo Currency { get; }

        public CurrencyAccount(SourceProviderName source, CurrencyInfo currency)
        {
            Source = source;
            Currency = currency;
        }

        public override string ToString()
        {
            return $"{Source}:{Currency}";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CurrencyAccount);
        }

        public override int GetHashCode()
        {
            return Source.GetHashCode() ^ Currency.GetHashCode();
        }

        public bool Equals(CurrencyAccount other)
        {
            if (ReferenceEquals(other, null) || GetType() != other.GetType())
            {
                return false;
            }

            return Source == other.Source && Currency == other.Currency;
        }

        public static bool operator ==(CurrencyAccount left, CurrencyAccount right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(CurrencyAccount left, CurrencyAccount right)
        {
            return !(left == right);
        }
    }
}