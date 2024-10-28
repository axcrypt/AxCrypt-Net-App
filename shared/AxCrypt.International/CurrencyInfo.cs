using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AxCrypt.International
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class CurrencyInfo : IEquatable<CurrencyInfo>
    {
        public static readonly CurrencyInfo SEK = new CurrencyInfo("SEK");

        public static readonly CurrencyInfo EUR = new CurrencyInfo("EUR");

        [JsonProperty("currency")]
        private string _currency;

        public CurrencyInfo(string currency)
        {
            _currency = currency.ToUpperInvariant() ?? string.Empty;
        }

        /// <summary>
        /// Round to the appropriate number of decimal places, depending on the currency.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public decimal Round(decimal value)
        {
            return Math.Round(value, Decimals(), MidpointRounding.ToEven);
        }

        /// <summary>
        /// Round and display with the appropriate number of decimal places.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string Display(decimal value)
        {
            return Display(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Round and display with the appropriate number of decimal places.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string Display(decimal? value, CultureInfo cultureInfo)
        {
            if (!value.HasValue)
            {
                return string.Empty;
            }

            return string.Format(cultureInfo, string.Format(cultureInfo, "{{0:F{0}}}", Decimals()), Round(value.Value));
        }

        public int ToUnits(decimal value)
        {
            switch (Decimals())
            {
                case 0:
                    return (int)Math.Round(value, 0, MidpointRounding.ToEven); ;
                case 2:
                    return (int)Math.Round(value * 100, 0, MidpointRounding.ToEven);

                default:
                    throw new InvalidOperationException("Unsupported value for Decimals().");
            }
        }

        public decimal FromUnits(int value)
        {
            switch (Decimals())
            {
                case 0:
                    return value;

                case 2:
                    return (decimal)value / 100m;

                default:
                    throw new InvalidOperationException("Unsupported value for Decimals().");
            }
        }

        public decimal SmartParse(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Length == 0)
            {
                return 0m;
            }

            if (IsInteger(value))
            {
                return ParseIntegerWithPossibleThousands(value);
            }

            int possibleDecimalIndex = DecimalPosition(value);

            string integerValue = value.Substring(0, possibleDecimalIndex);
            string decimalFractionValue = "0" + CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator + value.Substring(possibleDecimalIndex + 1);
            decimal result = ParseIntegerWithPossibleThousands(integerValue);
            decimal sign = GetSign(result, integerValue);
            result += sign * decimal.Parse(decimalFractionValue, NumberStyles.Integer | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
            return result;
        }

        private static bool IsInteger(string value)
        {
            if (value.Length < "0.0".Length)
            {
                return true;
            }
            if (!char.IsDigit(value[value.Length - ".0".Length]))
            {
                return false;
            }
            if (value.Length < "0.00".Length)
            {
                return true;
            }
            if (!char.IsDigit(value[value.Length - ".00".Length]))
            {
                return false;
            }

            return true;
        }

        private static int DecimalPosition(string value)
        {
            if (value.Length < "0.0".Length)
            {
                throw new ArgumentException("value is too short to have decimals", nameof(value));
            }

            if (!char.IsDigit(value[value.Length - ".0".Length]))
            {
                return value.Length - ".0".Length;
            }

            if (value.Length < "0.00".Length)
            {
                throw new ArgumentException("value is too short to have decimals", nameof(value));
            }

            if (!char.IsDigit(value[value.Length - ".00".Length]))
            {
                return value.Length - ".00".Length;
            }

            throw new ArgumentException("No decimals found", nameof(value));
        }

        private static decimal GetSign(decimal result, string integerValue)
        {
            decimal sign = Math.Sign(result);
            if (sign != 0m)
            {
                return sign;
            }
            result = ParseIntegerWithPossibleThousands(integerValue + "1");
            return Math.Sign(result);
        }

        private static decimal ParseIntegerWithPossibleThousands(string value)
        {
            if (value.Length >= "0.00".Length)
            {
                char possibleThousand = value[value.Length - ",000".Length];
                if (char.IsDigit(possibleThousand) || possibleThousand.ToString() == CultureInfo.InvariantCulture.NumberFormat.NegativeSign)
                {
                    return decimal.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                }
                value = value.Replace(possibleThousand.ToString(), CultureInfo.InvariantCulture.NumberFormat.NumberGroupSeparator);
                return decimal.Parse(value, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
            }
            return decimal.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        private int Decimals()
        {
            switch (_currency)
            {
                case "JPY":
                    return 0;

                default:
                    return 2;
            }
        }

        private static readonly string[] _supported = new string[]
        {
            "EUR",
            "USD",
            "GBP",
            "AUD",
            "BRL",
            "CAD",
            "CZK",
            "HKD",
            "HUF",
            "ILS",
            "MYR",
            "MXN",
            "NOK",
            "NZD",
            "PHP",
            "PLN",
            "RUB",
            "SGD",
            "SEK",
            "CHF",
            "THB",
            "JPY",
        };

        private static IEnumerable<CurrencyInfo> GetSupported()
        {
            return _supported.Select(c => new CurrencyInfo(c)).ToArray();
        }

        public static readonly IEnumerable<CurrencyInfo> Supported = GetSupported();

        public bool IsSupported
        {
            get
            {
                return _supported.Contains(_currency);
            }
        }

        public override string ToString()
        {
            return _currency;
        }

        public override bool Equals(object obj)
        {
            CurrencyInfo other = obj as CurrencyInfo;
            if (other == null)
            {
                return false;
            }
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return _currency.GetHashCode();
        }

        public static bool operator ==(CurrencyInfo left, CurrencyInfo right)
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

        public static bool operator !=(CurrencyInfo left, CurrencyInfo right)
        {
            return !(left == right);
        }

        #region IEquatable<CurrencyInfo> Members

        public bool Equals(CurrencyInfo other)
        {
            if ((object)other == null)
            {
                return false;
            }

            return _currency == other._currency;
        }

        #endregion IEquatable<CurrencyInfo> Members
    }
}