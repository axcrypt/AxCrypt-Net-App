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

using System.Text;

namespace AxCrypt.Cli
{
    /// <summary>
    /// Resolves the password for a command without ever logging or echoing it.
    ///
    /// Sources, in order of precedence:
    ///  1. --password (discouraged: visible in shell history and process lists; a warning is printed)
    ///  2. --password-file (first line of the file; use restrictive file permissions)
    ///  3. AXCRYPT_PASSWORD environment variable (recommended for automation)
    ///  4. Interactive prompt with no echo (recommended for interactive use)
    /// </summary>
    public static class PasswordInput
    {
        public const string PasswordEnvironmentVariable = "AXCRYPT_PASSWORD";

        public static string Resolve(ArgumentParser arguments, bool confirm)
        {
            string? password = arguments.Get("password");
            if (password != null)
            {
                Console.Error.WriteLine("Warning: passing --password on the command line exposes it to shell history and process lists. Prefer --password-file, the AXCRYPT_PASSWORD environment variable, or the interactive prompt.");
                return password;
            }

            string? passwordFile = arguments.Get("password-file");
            if (passwordFile != null)
            {
                if (!File.Exists(passwordFile))
                {
                    throw new CommandLineException($"Password file not found: {passwordFile}", ExitCodes.FileNotFound);
                }
                string? firstLine = File.ReadLines(passwordFile).FirstOrDefault();
                if (string.IsNullOrEmpty(firstLine))
                {
                    throw new CommandLineException("The password file is empty.", ExitCodes.UsageError);
                }
                return firstLine;
            }

            string? fromEnvironment = Environment.GetEnvironmentVariable(PasswordEnvironmentVariable);
            if (!string.IsNullOrEmpty(fromEnvironment))
            {
                return fromEnvironment;
            }

            if (Console.IsInputRedirected)
            {
                string? piped = Console.In.ReadLine();
                if (string.IsNullOrEmpty(piped))
                {
                    throw new CommandLineException("No password provided. Use --password-file, the AXCRYPT_PASSWORD environment variable, or run interactively.", ExitCodes.UsageError);
                }
                return piped;
            }

            string prompted = Prompt("Password: ");
            if (confirm)
            {
                string confirmation = Prompt("Confirm password: ");
                if (!string.Equals(prompted, confirmation, StringComparison.Ordinal))
                {
                    throw new CommandLineException("Passwords do not match.", ExitCodes.UsageError);
                }
            }
            if (prompted.Length == 0)
            {
                throw new CommandLineException("An empty password is not allowed.", ExitCodes.UsageError);
            }
            return prompted;
        }

        private static string Prompt(string prompt)
        {
            Console.Error.Write(prompt);
            StringBuilder builder = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.Error.WriteLine();
                    return builder.ToString();
                }
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (builder.Length > 0)
                    {
                        builder.Length -= 1;
                    }
                    continue;
                }
                if (key.KeyChar != '\0')
                {
                    builder.Append(key.KeyChar);
                }
            }
        }
    }
}
