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

using AxCrypt.Abstractions;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.IO;
using AxCrypt.Core.Service;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Cli
{
    /// <summary>
    /// Implementations of the CLI commands. All cryptography is delegated to the shared
    /// AxCrypt.Core library. Nothing here logs or prints passwords, keys, or plaintext.
    /// </summary>
    public static class Commands
    {
        public static async Task<int> EncryptAsync(ArgumentParser arguments)
        {
            string inputPath = arguments.Require("input");
            string outputPath = arguments.Get("output") ?? inputPath + ".axx";

            EnsureInputExists(inputPath);
            EnsureOutputWritable(outputPath, arguments.Has("force"));

            string password = PasswordInput.Resolve(arguments, confirm: !arguments.Has("password") && !arguments.Has("password-file") && Environment.GetEnvironmentVariable(PasswordInput.PasswordEnvironmentVariable) == null);

            EncryptionParameters parameters = new EncryptionParameters(new V2Aes256CryptoFactory().CryptoId, new Passphrase(password));
            IEnumerable<UserPublicKey> recipients = LoadPublicKeys(arguments.GetAll("recipient-public-key"));
            if (recipients.Any())
            {
                await parameters.AddAsync(recipients);
            }

            EncryptedProperties properties = new EncryptedProperties(Path.GetFileName(inputPath));

            try
            {
                using (FileStream source = File.OpenRead(inputPath))
                {
                    using (FileStream destination = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        AxCryptFile.Encrypt(source, destination, properties, parameters, AxCryptOptions.EncryptWithCompression, new ProgressContext());
                    }
                }
            }
            catch (Exception)
            {
                TryDelete(outputPath);
                throw;
            }

            Console.Error.WriteLine($"Encrypted '{inputPath}' -> '{outputPath}'.");
            return ExitCodes.Success;
        }

        public static Task<int> DecryptAsync(ArgumentParser arguments)
        {
            string inputPath = arguments.Require("input");
            string? outputPath = arguments.Get("output");

            EnsureInputExists(inputPath);

            string password = PasswordInput.Resolve(arguments, confirm: false);
            LogOnIdentity identity = BuildIdentity(arguments, password);

            using (FileStream source = File.OpenRead(inputPath))
            {
                using (IAxCryptDocument document = New<AxCryptFactory>().CreateDocument(identity.DecryptionParameters(), new ProgressStream(source, new ProgressContext())))
                {
                    if (!document.PassphraseIsValid)
                    {
                        throw new CommandLineException("Wrong password or key for this file.", ExitCodes.WrongPasswordOrKey);
                    }

                    outputPath ??= Path.Combine(Path.GetDirectoryName(Path.GetFullPath(inputPath)) ?? ".", document.FileName);
                    EnsureOutputWritable(outputPath, arguments.Has("force"));

                    try
                    {
                        using (FileStream destination = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            document.DecryptTo(destination);
                        }
                    }
                    catch (Exception)
                    {
                        TryDelete(outputPath);
                        throw;
                    }
                }
            }

            Console.Error.WriteLine($"Decrypted '{inputPath}' -> '{outputPath}'.");
            return Task.FromResult(ExitCodes.Success);
        }

        public static Task<int> ShowAsync(ArgumentParser arguments)
        {
            string inputPath = arguments.Require("input");
            EnsureInputExists(inputPath);

            string password = PasswordInput.Resolve(arguments, confirm: false);
            LogOnIdentity identity = BuildIdentity(arguments, password);

            using (FileStream source = File.OpenRead(inputPath))
            {
                using (IAxCryptDocument document = New<AxCryptFactory>().CreateDocument(identity.DecryptionParameters(), new ProgressStream(source, new ProgressContext())))
                {
                    if (!document.PassphraseIsValid)
                    {
                        throw new CommandLineException("Wrong password or key for this file.", ExitCodes.WrongPasswordOrKey);
                    }

                    Console.WriteLine($"File name       : {document.FileName}");
                    Console.WriteLine($"Created (UTC)   : {document.CreationTimeUtc:u}");
                    Console.WriteLine($"Modified (UTC)  : {document.LastWriteTimeUtc:u}");
                    IEnumerable<UserPublicKey> recipients = document.AsymmetricRecipients;
                    Console.WriteLine($"Key sharing     : {(recipients.Any() ? string.Join(", ", recipients.Select(r => r.Email.ToString())) : "(none)")}");
                }
            }
            return Task.FromResult(ExitCodes.Success);
        }

        public static Task<int> KeyGenAsync(ArgumentParser arguments)
        {
            string email = arguments.Require("email");
            string outputDirectory = arguments.Get("output") ?? ".";
            int bits = int.TryParse(arguments.Get("bits"), out int parsedBits) ? parsedBits : 4096;
            if (bits < 2048)
            {
                throw new CommandLineException("Key size must be at least 2048 bits.", ExitCodes.UsageError);
            }

            EmailAddress emailAddress;
            try
            {
                emailAddress = EmailAddress.Parse(email);
            }
            catch (Exception)
            {
                throw new CommandLineException($"'{email}' is not a valid e-mail address.", ExitCodes.UsageError);
            }

            Directory.CreateDirectory(outputDirectory);
            string privatePath = Path.Combine(outputDirectory, $"{emailAddress}-keypair.axx");
            string publicPath = Path.Combine(outputDirectory, $"{emailAddress}-public.json");
            EnsureOutputWritable(privatePath, arguments.Has("force"));
            EnsureOutputWritable(publicPath, arguments.Has("force"));

            string password = PasswordInput.Resolve(arguments, confirm: !arguments.Has("password") && !arguments.Has("password-file") && Environment.GetEnvironmentVariable(PasswordInput.PasswordEnvironmentVariable) == null);

            Console.Error.WriteLine($"Generating a {bits}-bit RSA key pair. This can take a while...");
            UserKeyPair keyPair = new UserKeyPair(emailAddress, bits);

            File.WriteAllBytes(privatePath, keyPair.ToArray(new Passphrase(password)));
            UserPublicKey publicKey = new UserPublicKey(emailAddress, keyPair.KeyPair.PublicKey);
            File.WriteAllText(publicPath, New<IStringSerializer>().Serialize(publicKey));

            Console.Error.WriteLine($"Private key pair (password-protected): {privatePath}");
            Console.Error.WriteLine($"Public key (shareable)               : {publicPath}");
            Console.Error.WriteLine("Keep the private key pair file safe. Anyone with the file AND the password can decrypt files shared with this key.");
            return Task.FromResult(ExitCodes.Success);
        }

        public static async Task<int> RecipientsAsync(ArgumentParser arguments, string subCommand)
        {
            string filePath = arguments.Require("file");
            EnsureInputExists(filePath);

            string password = PasswordInput.Resolve(arguments, confirm: false);
            LogOnIdentity identity = BuildIdentity(arguments, password);

            if (string.Equals(subCommand, "list", StringComparison.OrdinalIgnoreCase))
            {
                using (FileStream source = File.OpenRead(filePath))
                {
                    using (IAxCryptDocument document = New<AxCryptFactory>().CreateDocument(identity.DecryptionParameters(), new ProgressStream(source, new ProgressContext())))
                    {
                        if (!document.PassphraseIsValid)
                        {
                            throw new CommandLineException("Wrong password or key for this file.", ExitCodes.WrongPasswordOrKey);
                        }
                        foreach (UserPublicKey recipient in document.AsymmetricRecipients)
                        {
                            Console.WriteLine(recipient.Email);
                        }
                    }
                }
                return ExitCodes.Success;
            }

            if (!string.Equals(subCommand, "add", StringComparison.OrdinalIgnoreCase))
            {
                throw new CommandLineException($"Unknown recipients sub-command '{subCommand}'. Use 'add' or 'list'.", ExitCodes.UsageError);
            }

            IReadOnlyList<string> publicKeyPaths = arguments.GetAll("public-key");
            if (publicKeyPaths.Count == 0)
            {
                throw new CommandLineException("recipients add requires at least one --public-key <file>.", ExitCodes.UsageError);
            }
            List<UserPublicKey> newRecipients = LoadPublicKeys(publicKeyPaths).ToList();

            string temporaryPlain = filePath + ".tmp-plain";
            string temporaryEncrypted = filePath + ".tmp-axx";
            try
            {
                EncryptedProperties properties;

                using (FileStream source = File.OpenRead(filePath))
                {
                    using (IAxCryptDocument document = New<AxCryptFactory>().CreateDocument(identity.DecryptionParameters(), new ProgressStream(source, new ProgressContext())))
                    {
                        if (!document.PassphraseIsValid)
                        {
                            throw new CommandLineException("Wrong password or key for this file.", ExitCodes.WrongPasswordOrKey);
                        }

                        properties = EncryptedProperties.Create(document);
                        using (FileStream plain = new FileStream(temporaryPlain, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            document.DecryptTo(plain);
                        }
                    }
                }

                EncryptionParameters parameters = new EncryptionParameters(new V2Aes256CryptoFactory().CryptoId, new Passphrase(password));
                await parameters.AddAsync(properties.SharedKeyHolders);
                await parameters.AddAsync(newRecipients);

                using (FileStream plain = File.OpenRead(temporaryPlain))
                {
                    using (FileStream destination = new FileStream(temporaryEncrypted, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        AxCryptFile.Encrypt(plain, destination, properties, parameters, AxCryptOptions.EncryptWithCompression, new ProgressContext());
                    }
                }

                File.Copy(temporaryEncrypted, filePath, overwrite: true);
                Console.Error.WriteLine($"Added {newRecipients.Count} recipient(s) to '{filePath}'.");
            }
            finally
            {
                WipeAndDelete(temporaryPlain);
                TryDelete(temporaryEncrypted);
            }
            return ExitCodes.Success;
        }

        private static LogOnIdentity BuildIdentity(ArgumentParser arguments, string password)
        {
            string? keyPairPath = arguments.Get("key-file");
            if (keyPairPath == null)
            {
                return new LogOnIdentity(password);
            }

            if (!File.Exists(keyPairPath))
            {
                throw new CommandLineException($"Key pair file not found: {keyPairPath}", ExitCodes.FileNotFound);
            }
            if (!UserKeyPair.TryLoad(File.ReadAllBytes(keyPairPath), new Passphrase(password), out UserKeyPair keyPair))
            {
                throw new CommandLineException("Could not load the key pair file. Wrong password or invalid file.", ExitCodes.WrongPasswordOrKey);
            }
            return new LogOnIdentity(new UserKeyPair[] { keyPair }, new Passphrase(password));
        }

        private static IEnumerable<UserPublicKey> LoadPublicKeys(IReadOnlyList<string> paths)
        {
            List<UserPublicKey> keys = new List<UserPublicKey>();
            foreach (string path in paths)
            {
                if (!File.Exists(path))
                {
                    throw new CommandLineException($"Public key file not found: {path}", ExitCodes.FileNotFound);
                }
                try
                {
                    keys.Add(New<IStringSerializer>().Deserialize<UserPublicKey>(File.ReadAllText(path)));
                }
                catch (Exception)
                {
                    throw new CommandLineException($"'{path}' is not a valid AxCrypt public key file (expected the JSON format produced by 'axcrypt keygen').", ExitCodes.InvalidOrCorruptFile);
                }
            }
            return keys;
        }

        private static void EnsureInputExists(string path)
        {
            if (!File.Exists(path))
            {
                throw new CommandLineException($"Input file not found: {path}", ExitCodes.FileNotFound);
            }
        }

        private static void EnsureOutputWritable(string path, bool force)
        {
            if (File.Exists(path) && !force)
            {
                throw new CommandLineException($"Output file already exists: {path}. Use --force to overwrite.", ExitCodes.UsageError);
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        /// <summary>
        /// Best-effort overwrite of a temporary plaintext file before deletion, so that
        /// plaintext does not linger on disk longer than necessary.
        /// </summary>
        private static void WipeAndDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    long length = new FileInfo(path).Length;
                    using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None))
                    {
                        byte[] zeros = new byte[64 * 1024];
                        long remaining = length;
                        while (remaining > 0)
                        {
                            int chunk = (int)Math.Min(remaining, zeros.Length);
                            stream.Write(zeros, 0, chunk);
                            remaining -= chunk;
                        }
                        stream.Flush(true);
                    }
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
