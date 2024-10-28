using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Reports.Abstractions
{
    public class PersistentName
    {
        public string Name { get; }

        public PersistentName(string name)
        {
            Name = EnsureName(name);
        }

        private static string EnsureName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException("A name cannot be empty.");
            }
            if (name.Length > 10)
            {
                throw new ArgumentException("A name cannot be at most 10 characters.");
            }

            name = name.ToLowerInvariant();
            if (name[0] < 'a' || name[0] > 'z')
            {
                throw new ArgumentException("A v must start with a letter a-z.");
            }

            if (name.Any(c => !((c >= 'a' && c <= 'z') || char.IsDigit(c))))
            {
                throw new ArgumentException("A name must only contain letters a-z and digits.");
            }

            return name;
        }
    }
}