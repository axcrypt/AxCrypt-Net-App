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

using System.Reflection;

using AxCrypt.Abstractions;

namespace AxCrypt.Cli
{
    public static class Program
    {
        private static readonly string[] _booleanFlags = new string[] { "force", "help" };

        public static async Task<int> Main(string[] args)
        {
            string command = args.Length > 0 ? args[0].ToLowerInvariant() : "help";
            string[] rest = args.Skip(1).ToArray();

            try
            {
                switch (command)
                {
                    case "encrypt":
                    case "decrypt":
                    case "show":
                    case "keygen":
                    case "recipients":
                        CliRuntime.Initialize();
                        break;

                    default:
                        break;
                }

                switch (command)
                {
                    case "encrypt":
                        return await Commands.EncryptAsync(new ArgumentParser(rest, _booleanFlags));

                    case "decrypt":
                        return await Commands.DecryptAsync(new ArgumentParser(rest, _booleanFlags));

                    case "show":
                    case "info":
                        return await Commands.ShowAsync(new ArgumentParser(rest, _booleanFlags));

                    case "keygen":
                        return await Commands.KeyGenAsync(new ArgumentParser(rest, _booleanFlags));

                    case "recipients":
                        if (rest.Length == 0)
                        {
                            throw new CommandLineException("recipients requires a sub-command: add | list.", ExitCodes.UsageError);
                        }
                        return await Commands.RecipientsAsync(new ArgumentParser(rest.Skip(1).ToArray(), _booleanFlags), rest[0]);

                    case "version":
                    case "--version":
                        return Version();

                    case "help":
                    case "--help":
                    case "-h":
                        return Help();

                    default:
                        Console.Error.WriteLine($"Unknown command '{command}'.");
                        Help();
                        return ExitCodes.UsageError;
                }
            }
            catch (CommandLineException cle)
            {
                Console.Error.WriteLine($"Error: {cle.Message}");
                return cle.ExitCode;
            }
            catch (AxCryptException ace)
            {
                // A structured error from the core library, e.g. an invalid or corrupt file.
                // Never include passwords, keys, or content in output.
                Console.Error.WriteLine($"Error: {ace.Message} (status: {ace.ErrorStatus})");
                return ExitCodes.InvalidOrCorruptFile;
            }
            catch (UnauthorizedAccessException uae)
            {
                Console.Error.WriteLine($"Error: access denied. {uae.Message}");
                return ExitCodes.GeneralError;
            }
            catch (IOException ioe)
            {
                Console.Error.WriteLine($"Error: I/O failure. {ioe.Message}");
                return ExitCodes.GeneralError;
            }
            catch (Exception e)
            {
                // Last-resort handler: report the error type and message only —
                // never passwords, keys, or content.
                Console.Error.WriteLine($"Error: unexpected failure ({e.GetType().Name}): {e.Message}");
                return ExitCodes.GeneralError;
            }
        }

        private static int Version()
        {
            string version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
            Console.WriteLine($"axcrypt {version}");
            Console.WriteLine("Copyright (C) AxCrypt AB. License GPL-3.0-or-later <https://www.gnu.org/licenses/gpl-3.0.html>.");
            Console.WriteLine("This is free software: you are free to change and redistribute it.");
            Console.WriteLine("There is NO WARRANTY, to the extent permitted by law.");
            Console.WriteLine();
            Console.WriteLine("AxCrypt is a trademark of AxCrypt AB. This build is only an official");
            Console.WriteLine("AxCrypt build if it is digitally signed by AxCrypt AB.");
            return ExitCodes.Success;
        }

        private static int Help()
        {
            Console.WriteLine("axcrypt - AxCrypt file encryption from the command line");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  axcrypt encrypt    --input <file> [--output <file.axx>] [--recipient-public-key <pub.json>]... [--force]");
            Console.WriteLine("  axcrypt decrypt    --input <file.axx> [--output <file>] [--key-file <keypair.axx>] [--force]");
            Console.WriteLine("  axcrypt show       --input <file.axx> [--key-file <keypair.axx>]");
            Console.WriteLine("  axcrypt keygen     --email <address> [--output <dir>] [--bits 4096] [--force]");
            Console.WriteLine("  axcrypt recipients add  --file <file.axx> --public-key <pub.json>...");
            Console.WriteLine("  axcrypt recipients list --file <file.axx>");
            Console.WriteLine("  axcrypt version");
            Console.WriteLine("  axcrypt help");
            Console.WriteLine();
            Console.WriteLine("Password input (in order of precedence):");
            Console.WriteLine("  --password <pwd>        Discouraged: visible in shell history and process lists.");
            Console.WriteLine("  --password-file <file>  First line of the file is used.");
            Console.WriteLine("  AXCRYPT_PASSWORD        Environment variable, recommended for automation.");
            Console.WriteLine("  (interactive)           Hidden prompt, recommended for interactive use.");
            Console.WriteLine();
            Console.WriteLine("Exit codes: 0 success, 1 general error, 2 usage error, 3 wrong password/key,");
            Console.WriteLine("            4 file not found, 5 invalid or corrupt file.");
            Console.WriteLine();
            Console.WriteLine("Full reference: docs/CLI.md");
            return ExitCodes.Success;
        }
    }
}
