using AxCrypt.International;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Reports.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class TransactionAmounts
    {
        [JsonProperty("currency")]
        public CurrencyInfo Currency { get; set; }

        [JsonProperty("total")]
        public decimal? Total { get; set; }

        [JsonProperty("vat")]
        public decimal? Vat { get; set; }

        [JsonProperty("fee")]
        public decimal? Fee { get; set; }

        public static TransactionAmounts operator +(TransactionAmounts left, TransactionAmounts right)
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

            TransactionAmounts sum = new TransactionAmounts()
            {
                Currency = left.Currency,
                Total = left.Total.HasValue || right.Total.HasValue ? left.Total.GetValueOrDefault() + right.Total.GetValueOrDefault() : new decimal?(),
                Fee = left.Fee.HasValue || right.Fee.HasValue ? left.Fee.GetValueOrDefault() + right.Fee.GetValueOrDefault() : new decimal?(),
                Vat = left.Vat.HasValue || right.Vat.HasValue ? left.Vat.GetValueOrDefault() + right.Vat.GetValueOrDefault() : new decimal?(),
            };

            return sum;
        }
    }
}