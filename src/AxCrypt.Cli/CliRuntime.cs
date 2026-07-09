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
using AxCrypt.Abstractions.Algorithm;
using AxCrypt.Common;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Desktop;
using AxCrypt.Mono;
using AxCrypt.Mono.Portable;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Cli
{
    /// <summary>
    /// Bootstraps the AxCrypt type system for command-line use. All cryptographic
    /// functionality is provided by the existing shared libraries (AxCrypt.Core et al);
    /// nothing crypto-related is implemented in the CLI itself.
    ///
    /// The CLI runs fully offline. No AxCrypt AB account or server infrastructure is
    /// required or contacted.
    /// </summary>
    public static class CliRuntime
    {
        private static bool _isInitialized;

        /// <summary>
        /// The work folder used for CLI settings (e.g. calibrated key wrap iterations).
        /// Override with the AXCRYPT_CLI_WORKFOLDER environment variable.
        /// </summary>
        public static string WorkFolderPath { get; private set; } = string.Empty;

        public static void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }
            _isInitialized = true;

            WorkFolderPath = ResolveWorkFolderPath();
            Directory.CreateDirectory(WorkFolderPath);

            RuntimeEnvironment.RegisterTypeFactories();
            Resolve.RegisterTypeFactories(WorkFolderPath, new Assembly[] { typeof(AxCryptFile).Assembly });
            DesktopFactory.RegisterTypeFactories();

            TypeMap.Register.Singleton<INow>(() => new Now());
            TypeMap.Register.Singleton<IReport>(() => new Report(WorkFolderPath, 100 * 1024));
            TypeMap.Register.Singleton<IUIThread>(() => new CliUIThread());
            TypeMap.Register.Singleton<IEmailParser>(() => new EmailParser());
            TypeMap.Register.Singleton<AxCryptOnlineState>(() => new AxCryptOnlineState());
            TypeMap.Register.Singleton<FileLocker>(() => new FileLocker());

            // The CLI is offline and unlicensed by design; use the forced-premium policy so
            // that the strongest available crypto (AES-256) is always used. This does not
            // enable any AxCrypt AB online services.
            TypeMap.Register.Singleton<LicensePolicy>(() => new PremiumForcedLicensePolicy());
            TypeMap.Register.New<ISystemCryptoPolicy>(() => new ProCryptoPolicy());
            TypeMap.Register.New<ICryptoPolicy>(() => New<LicensePolicy>().Capabilities.CryptoPolicy);

            // Portable (fully managed, cross-platform) algorithm implementations.
            TypeMap.Register.New<AxCryptHMACSHA1>(() => PortableFactory.AxCryptHMACSHA1());
            TypeMap.Register.New<HMACSHA512>(() => PortableFactory.HMACSHA512());
            TypeMap.Register.New<Aes>(() => PortableFactory.AesManaged());
            TypeMap.Register.New<Sha1>(() => PortableFactory.SHA1Managed());
            TypeMap.Register.New<Sha256>(() => PortableFactory.SHA256Managed());
            TypeMap.Register.New<CryptoStreamBase>(() => PortableFactory.CryptoStream());
            TypeMap.Register.New<RandomNumberGenerator>(() => PortableFactory.RandomNumberGenerator());
        }

        private static string ResolveWorkFolderPath()
        {
            string? overridden = Environment.GetEnvironmentVariable("AXCRYPT_CLI_WORKFOLDER");
            string path = !string.IsNullOrEmpty(overridden)
                ? overridden
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AxCrypt", "Cli");
            if (!path.EndsWith(Path.DirectorySeparatorChar))
            {
                path += Path.DirectorySeparatorChar;
            }
            return path;
        }
    }
}
