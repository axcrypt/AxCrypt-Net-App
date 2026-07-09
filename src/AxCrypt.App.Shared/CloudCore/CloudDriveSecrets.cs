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

using System;
using System.Collections.Generic;

namespace AxCrypt.App.Shared.CloudCore
{
    /// <summary>
    /// Central access point for third-party cloud drive OAuth application credentials.
    ///
    /// No secrets are committed to this repository. Values are resolved in this order:
    /// 1. An environment variable with the given name (useful for development and CI).
    /// 2. Build-time injected values from an optional, git-ignored partial class file
    ///    'CloudDriveSecrets.BuildTime.cs' (see 'CloudDriveSecrets.BuildTime.cs.template').
    ///    Official AxCrypt AB release pipelines generate that file from protected CI secrets.
    /// 3. The empty string, which disables the corresponding cloud drive integration.
    ///
    /// Community builds work without any of these values; only the optional cloud drive
    /// sign-in features are unavailable when a credential is absent.
    /// </summary>
    internal static partial class CloudDriveSecrets
    {
        public const string DropBoxAppKey = "AXCRYPT_DROPBOX_APP_KEY";
        public const string DropBoxAppSecret = "AXCRYPT_DROPBOX_APP_SECRET";
        public const string GoogleClientIdIos = "AXCRYPT_GOOGLE_CLIENT_ID_IOS";
        public const string GoogleClientIdAndroid = "AXCRYPT_GOOGLE_CLIENT_ID_ANDROID";
        public const string GoogleClientIdDesktop = "AXCRYPT_GOOGLE_CLIENT_ID_DESKTOP";
        public const string GoogleClientSecretDesktop = "AXCRYPT_GOOGLE_CLIENT_SECRET_DESKTOP";
        public const string OneDriveClientId = "AXCRYPT_ONEDRIVE_CLIENT_ID";

        private static readonly Dictionary<string, string> _buildTimeSecrets = CreateBuildTimeSecrets();

        private static Dictionary<string, string> CreateBuildTimeSecrets()
        {
            Dictionary<string, string> secrets = new Dictionary<string, string>(StringComparer.Ordinal);
            AddBuildTimeSecrets(secrets);
            return secrets;
        }

        /// <summary>
        /// Implemented, if at all, by the git-ignored 'CloudDriveSecrets.BuildTime.cs'
        /// generated during official release builds. Never commit an implementation.
        /// </summary>
        static partial void AddBuildTimeSecrets(IDictionary<string, string> secrets);

        /// <summary>
        /// Gets the credential with the given well-known name, or the empty string if it
        /// is not configured. Never log or display the returned value.
        /// </summary>
        public static string Get(string name)
        {
            string? fromEnvironment = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrEmpty(fromEnvironment))
            {
                return fromEnvironment;
            }
            return _buildTimeSecrets.TryGetValue(name, out string? fromBuild) ? fromBuild : string.Empty;
        }
    }
}
