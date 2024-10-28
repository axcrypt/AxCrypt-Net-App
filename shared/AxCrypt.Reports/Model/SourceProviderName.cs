using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Reports.Model
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class SourceProviderName : IEquatable<SourceProviderName>
    {
        public static readonly SourceProviderName Stripe = new SourceProviderName("Stripe");

        public static readonly SourceProviderName PayPal = new SourceProviderName("PayPal");

        public SourceProviderName(string name)
        {
            switch (name)
            {
                case "PayPal":
                case "Stripe":
                    break;

                default:
                    throw new ArgumentException($"Invalid source provider name '{name}'.", name);
            }

            Name = name;
        }

        [JsonProperty("name")]
        public string Name { get; }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SourceProviderName);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public bool Equals(SourceProviderName other)
        {
            if (ReferenceEquals(other, null) || GetType() != other.GetType())
            {
                return false;
            }

            return Name == other.Name;
        }

        public static bool operator ==(SourceProviderName left, SourceProviderName right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(SourceProviderName left, SourceProviderName right)
        {
            return !(left == right);
        }
    }
}