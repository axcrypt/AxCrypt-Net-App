using AxCrypt.Abstractions;
using AxCrypt.International;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Reports.Model
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class TransactionRecord : IEquatable<TransactionRecord>
    {
        public static readonly TransactionRecord Empty = new TransactionRecord();

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("utc")]
        public DateTime Utc { get; set; }

        [JsonProperty("id_reference")]
        public string IdReference { get; set; } = string.Empty;

        [JsonProperty("source")]
        public SourceProviderName Source { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("affects_revenue")]
        public bool AffectsRevenue { get; set; }

        [JsonProperty("affects_balance")]
        public bool AffectsBalance { get; set; }

        [JsonProperty("amounts_payment")]
        public TransactionAmounts AmountsPayment { get; set; }

        [JsonProperty("amounts_account")]
        public TransactionAmounts AmountsAccount { get; set; }

        [JsonProperty("account_balance")]
        public decimal? AccountBalance { get; set; }

        [JsonProperty("country")]
        public LocaleInfo Country { get; set; }

        public static TransactionRecord Parse(string value)
        {
            return New<IStringSerializer>().Deserialize<TransactionRecord>(value);
        }

        public override string ToString()
        {
            string s = New<IStringSerializer>().Serialize(this).Replace(Environment.NewLine, string.Empty);
            while (s.Contains("  "))
            {
                s = s.Replace("  ", " ");
            }
            return s;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TransactionRecord);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ Utc.GetHashCode();
        }

        public bool Equals(TransactionRecord other)
        {
            if (ReferenceEquals(other, null) || GetType() != other.GetType())
            {
                return false;
            }

            return Id == other.Id && Utc == other.Utc;
        }

        public static bool operator ==(TransactionRecord left, TransactionRecord right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(TransactionRecord left, TransactionRecord right)
        {
            return !(left == right);
        }
    }
}