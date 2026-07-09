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

using NUnit.Framework;

using AxCrypt.Cli;

namespace AxCrypt.Cli.Test
{
    /// <summary>
    /// End-to-end tests for the CLI, exercising the real AxCrypt.Core crypto stack
    /// (no fakes): encryption/decryption round trip, wrong password handling,
    /// corrupted file handling, argument validation, key generation and key sharing,
    /// and temporary plaintext cleanup.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class TestCliCommands
    {
        private string _testDirectory = string.Empty;

        private const string TestPassword = "unit-test-passphrase-1234";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "AxCryptCliTest", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDirectory);

            // Isolate CLI settings (key wrap iteration calibration etc.) from the user profile.
            Environment.SetEnvironmentVariable("AXCRYPT_CLI_WORKFOLDER", Path.Combine(_testDirectory, "WorkFolder"));
            // Provide the password without any interactive prompt.
            Environment.SetEnvironmentVariable("AXCRYPT_PASSWORD", TestPassword);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Environment.SetEnvironmentVariable("AXCRYPT_CLI_WORKFOLDER", null);
            Environment.SetEnvironmentVariable("AXCRYPT_PASSWORD", null);
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch (IOException)
            {
            }
        }

        private string CreatePlaintextFile(string name, string content)
        {
            string path = Path.Combine(_testDirectory, name);
            File.WriteAllText(path, content);
            return path;
        }

        private static Task<int> RunAsync(params string[] args)
        {
            return Program.Main(args);
        }

        [Test]
        public async Task EncryptDecryptRoundTripRestoresExactContent()
        {
            string content = "Hello, AxCrypt! Round trip with some unicode: åäö € 你好." + new string('x', 100_000);
            string plain = CreatePlaintextFile("roundtrip.txt", content);
            string encrypted = plain + ".axx";
            string decrypted = Path.Combine(_testDirectory, "roundtrip-out.txt");

            Assert.That(await RunAsync("encrypt", "--input", plain, "--output", encrypted), Is.EqualTo(ExitCodes.Success));
            Assert.That(File.Exists(encrypted), Is.True);
            Assert.That(File.ReadAllBytes(encrypted), Is.Not.EqualTo(File.ReadAllBytes(plain)), "Encrypted file must not equal plaintext.");

            Assert.That(await RunAsync("decrypt", "--input", encrypted, "--output", decrypted), Is.EqualTo(ExitCodes.Success));
            Assert.That(File.ReadAllText(decrypted), Is.EqualTo(content));
        }

        [Test]
        public async Task DecryptRestoresOriginalFileNameWhenNoOutputGiven()
        {
            string content = "original name preservation";
            string plain = CreatePlaintextFile("original-name.txt", content);
            string encrypted = Path.Combine(_testDirectory, "renamed-container.axx");

            Assert.That(await RunAsync("encrypt", "--input", plain, "--output", encrypted), Is.EqualTo(ExitCodes.Success));
            File.Delete(plain);

            Assert.That(await RunAsync("decrypt", "--input", encrypted), Is.EqualTo(ExitCodes.Success));
            Assert.That(File.ReadAllText(Path.Combine(_testDirectory, "original-name.txt")), Is.EqualTo(content));
        }

        [Test]
        public async Task DecryptWithWrongPasswordFailsWithWrongPasswordExitCode()
        {
            string plain = CreatePlaintextFile("wrongpassword.txt", "secret content");
            string encrypted = plain + ".axx";
            Assert.That(await RunAsync("encrypt", "--input", plain, "--output", encrypted), Is.EqualTo(ExitCodes.Success));

            string decrypted = Path.Combine(_testDirectory, "wrongpassword-out.txt");
            int status = await RunAsync("decrypt", "--input", encrypted, "--output", decrypted, "--password", "not-the-right-password");

            Assert.That(status, Is.EqualTo(ExitCodes.WrongPasswordOrKey));
            Assert.That(File.Exists(decrypted), Is.False, "No output must be produced on failure.");
        }

        [Test]
        public async Task DecryptCorruptedFileFailsCleanly()
        {
            string plain = CreatePlaintextFile("corrupt.txt", "will be corrupted");
            string encrypted = plain + ".axx";
            Assert.That(await RunAsync("encrypt", "--input", plain, "--output", encrypted), Is.EqualTo(ExitCodes.Success));

            byte[] bytes = File.ReadAllBytes(encrypted);
            for (int i = 0; i < Math.Min(bytes.Length, 32); ++i)
            {
                bytes[i] ^= 0xFF;
            }
            File.WriteAllBytes(encrypted, bytes);

            string decrypted = Path.Combine(_testDirectory, "corrupt-out.txt");
            int status = await RunAsync("decrypt", "--input", encrypted, "--output", decrypted);

            Assert.That(status, Is.EqualTo(ExitCodes.InvalidOrCorruptFile).Or.EqualTo(ExitCodes.WrongPasswordOrKey));
            Assert.That(File.Exists(decrypted), Is.False, "No output must be produced on failure.");
        }

        [Test]
        public async Task MissingRequiredArgumentIsUsageError()
        {
            Assert.That(await RunAsync("encrypt"), Is.EqualTo(ExitCodes.UsageError));
            Assert.That(await RunAsync("decrypt"), Is.EqualTo(ExitCodes.UsageError));
            Assert.That(await RunAsync("keygen"), Is.EqualTo(ExitCodes.UsageError));
            Assert.That(await RunAsync("recipients"), Is.EqualTo(ExitCodes.UsageError));
        }

        [Test]
        public async Task UnknownCommandIsUsageError()
        {
            Assert.That(await RunAsync("frobnicate"), Is.EqualTo(ExitCodes.UsageError));
        }

        [Test]
        public async Task MissingInputFileIsFileNotFound()
        {
            string missing = Path.Combine(_testDirectory, "does-not-exist.txt");
            Assert.That(await RunAsync("encrypt", "--input", missing), Is.EqualTo(ExitCodes.FileNotFound));
        }

        [Test]
        public async Task ExistingOutputWithoutForceIsRefused()
        {
            string plain = CreatePlaintextFile("no-overwrite.txt", "content");
            string existing = CreatePlaintextFile("no-overwrite.axx", "pre-existing");

            Assert.That(await RunAsync("encrypt", "--input", plain, "--output", existing), Is.EqualTo(ExitCodes.UsageError));
            Assert.That(File.ReadAllText(existing), Is.EqualTo("pre-existing"), "Existing file must be untouched.");

            Assert.That(await RunAsync("encrypt", "--input", plain, "--output", existing, "--force"), Is.EqualTo(ExitCodes.Success));
        }

        [Test]
        public async Task VersionAndHelpSucceed()
        {
            Assert.That(await RunAsync("version"), Is.EqualTo(ExitCodes.Success));
            Assert.That(await RunAsync("help"), Is.EqualTo(ExitCodes.Success));
            Assert.That(await RunAsync(), Is.EqualTo(ExitCodes.Success));
        }

        [Test]
        public async Task KeygenEncryptWithRecipientAndDecryptWithKeyFile()
        {
            string keyDirectory = Path.Combine(_testDirectory, "keys");
            const string email = "test@example.com";

            Assert.That(await RunAsync("keygen", "--email", email, "--output", keyDirectory, "--bits", "2048"), Is.EqualTo(ExitCodes.Success));
            string publicKeyPath = Path.Combine(keyDirectory, $"{email}-public.json");
            string keyPairPath = Path.Combine(keyDirectory, $"{email}-keypair.axx");
            Assert.That(File.Exists(publicKeyPath), Is.True);
            Assert.That(File.Exists(keyPairPath), Is.True);

            string content = "shared with a recipient";
            string plain = CreatePlaintextFile("shared.txt", content);
            string encrypted = plain + ".axx";
            Assert.That(await RunAsync("encrypt", "--input", plain, "--output", encrypted, "--recipient-public-key", publicKeyPath), Is.EqualTo(ExitCodes.Success));

            // The recipient decrypts with the private key pair file, NOT the file password.
            string decrypted = Path.Combine(_testDirectory, "shared-out.txt");
            int status = await RunAsync("decrypt", "--input", encrypted, "--output", decrypted, "--key-file", keyPairPath);
            Assert.That(status, Is.EqualTo(ExitCodes.Success));
            Assert.That(File.ReadAllText(decrypted), Is.EqualTo(content));
        }

        [Test]
        public async Task RecipientsAddThenListShowsRecipientAndLeavesNoPlaintextBehind()
        {
            string keyDirectory = Path.Combine(_testDirectory, "keys2");
            const string email = "recipient2@example.com";
            Assert.That(await RunAsync("keygen", "--email", email, "--output", keyDirectory, "--bits", "2048"), Is.EqualTo(ExitCodes.Success));
            string publicKeyPath = Path.Combine(keyDirectory, $"{email}-public.json");

            string content = "add a recipient after the fact";
            string plain = CreatePlaintextFile("later-share.txt", content);
            string encrypted = plain + ".axx";
            Assert.That(await RunAsync("encrypt", "--input", plain, "--output", encrypted), Is.EqualTo(ExitCodes.Success));

            Assert.That(await RunAsync("recipients", "add", "--file", encrypted, "--public-key", publicKeyPath), Is.EqualTo(ExitCodes.Success));

            // No temporary plaintext or temporary container may remain.
            Assert.That(File.Exists(encrypted + ".tmp-plain"), Is.False, "Temporary plaintext must be wiped and deleted.");
            Assert.That(File.Exists(encrypted + ".tmp-axx"), Is.False, "Temporary container must be deleted.");

            // The file still decrypts with the password, and the round trip is intact.
            string decrypted = Path.Combine(_testDirectory, "later-share-out.txt");
            Assert.That(await RunAsync("decrypt", "--input", encrypted, "--output", decrypted), Is.EqualTo(ExitCodes.Success));
            Assert.That(File.ReadAllText(decrypted), Is.EqualTo(content));

            // And the recipient decrypts with the key pair file.
            string keyPairPath = Path.Combine(keyDirectory, $"{email}-keypair.axx");
            string decryptedByRecipient = Path.Combine(_testDirectory, "later-share-recipient.txt");
            Assert.That(await RunAsync("decrypt", "--input", encrypted, "--output", decryptedByRecipient, "--key-file", keyPairPath), Is.EqualTo(ExitCodes.Success));
            Assert.That(File.ReadAllText(decryptedByRecipient), Is.EqualTo(content));
        }

        [Test]
        public async Task CrossPlatformRelativePathsWork()
        {
            string previousDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(_testDirectory);
            try
            {
                File.WriteAllText("relative.txt", "relative path content");
                Assert.That(await RunAsync("encrypt", "--input", "relative.txt", "--output", "relative.txt.axx"), Is.EqualTo(ExitCodes.Success));
                Assert.That(await RunAsync("decrypt", "--input", "relative.txt.axx", "--output", Path.Combine(".", "relative-out.txt")), Is.EqualTo(ExitCodes.Success));
                Assert.That(File.ReadAllText("relative-out.txt"), Is.EqualTo("relative path content"));
            }
            finally
            {
                Directory.SetCurrentDirectory(previousDirectory);
            }
        }
    }
}
