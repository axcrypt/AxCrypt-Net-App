using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.International
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class CurrencyValue : IEquatable<CurrencyValue>
    {
        [JsonProperty("value")]
        public decimal? Value { get; }

        [JsonProperty("currency")]
        public CurrencyInfo Currency { get; }

        public CurrencyValue(CurrencyInfo currency)
            : this(null, currency)
        {
        }

        public CurrencyValue(decimal? value, CurrencyInfo currency)
        {
            Value = value;
            Currency = currency;
        }

        public static CurrencyValue operator +(CurrencyValue left, CurrencyValue right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }
            if (left.Currency != right.Currency)
            {
                throw new ArgumentException($"Can't add different currencies {left.Currency} and {right.Currency}.");
            }

            decimal? valueSum = left.Value.HasValue || right.Value.HasValue ? left.Value.GetValueOrDefault() + right.Value.GetValueOrDefault() : new decimal?();
            CurrencyValue sum = new CurrencyValue(valueSum, left.Currency);

            return sum;
        }

        public override string ToString()
        {
            return $"{Currency} {Value}";
        }

        public override bool Equals(object obj)
        {
            CurrencyValue other = obj as CurrencyValue;
            if (other == null)
            {
                return false;
            }
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return Currency.GetHashCode() ^ Value.GetHashCode();
        }

        public static bool operator ==(CurrencyValue left, CurrencyValue right)
        {
            if (object.ReferenceEquals(left, right))
            {
                return true;
            }
            if ((object)left == null)
            {
                return false;
            }
            return left.Equals(right);
        }

        public static bool operator !=(CurrencyValue left, CurrencyValue right)
        {
            return !(left == right);
        }

        #region IEquatable<CurrencyValue> Members

        public bool Equals(CurrencyValue other)
        {
            if ((object)other == null)
            {
                return false;
            }

            return Currency == other.Currency && Value == other.Value;
        }

        #endregion IEquatable<CurrencyValue> Members
    }
}