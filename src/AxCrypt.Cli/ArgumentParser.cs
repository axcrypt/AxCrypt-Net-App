// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) AxCrypt AB
//
// This file is part of AxCrypt.
//
// AxCrypt is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// AxCrypt is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with AxCrypt. If not, see <https://www.gnu.org/licenses/>.

namespace AxCrypt.Cli
{
    /// <summary>
    /// Minimal dependency-free option parser: supports '--name value', '--name=value',
    /// repeatable options, and boolean flags.
    /// </summary>
    public sealed class ArgumentParser
    {
        private readonly Dictionary<string, List<string>> _options = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> _flags;

        private readonly List<string> _positional = new List<string>();

        public ArgumentParser(IEnumerable<string> args, IEnumerable<string>? booleanFlags = null)
        {
            _flags = new HashSet<string>(booleanFlags ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            string[] arguments = args.ToArray();
            for (int i = 0; i < arguments.Length; ++i)
            {
                string argument = arguments[i];
                if (!argument.StartsWith("--", StringComparison.Ordinal))
                {
                    _positional.Add(argument);
                    continue;
                }

                string name = argument.Substring(2);
                string? value = null;

                int equals = name.IndexOf('=');
                if (equals >= 0)
                {
                    value = name.Substring(equals + 1);
                    name = name.Substring(0, equals);
                }
                else if (!_flags.Contains(name))
                {
                    if (i + 1 >= arguments.Length)
                    {
                        throw new CommandLineException($"Missing value for option --{name}.", ExitCodes.UsageError);
                    }
                    value = arguments[++i];
                }

                if (!_options.TryGetValue(name, out List<string>? values))
                {
                    values = new List<string>();
                    _options[name] = values;
                }
                values.Add(value ?? bool.TrueString);
            }
        }

        public IReadOnlyList<string> Positional => _positional;

        public bool Has(string name)
        {
            return _options.ContainsKey(name);
        }

        public string? Get(string name)
        {
            return _options.TryGetValue(name, out List<string>? values) ? values[values.Count - 1] : null;
        }

        public string Require(string name)
        {
            string? value = Get(name);
            if (string.IsNullOrEmpty(value))
            {
                throw new CommandLineException($"Required option --{name} is missing. See 'axcrypt help'.", ExitCodes.UsageError);
            }
            return value;
        }

        public IReadOnlyList<string> GetAll(string name)
        {
            return _options.TryGetValue(name, out List<string>? values) ? values : Array.Empty<string>();
        }
    }
}
