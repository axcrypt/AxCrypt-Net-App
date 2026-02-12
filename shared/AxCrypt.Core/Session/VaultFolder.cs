#region Coypright and License

/*
 * AxCrypt - Copyright 2026, AxCrypt AB, All Rights Reserved
 *
 * This file is part of AxCrypt.
 *
 * AxCrypt is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * AxCrypt is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with AxCrypt.  If not, see <http://www.gnu.org/licenses/>.
 *
 * The source is maintained at http://bitbucket.org/AxCrypt-net please visit for
 * updates, contributions and contact with the author. You may also visit
 * http://www.axcrypt.net for more information about the author.
*/

#endregion Coypright and License

using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using Newtonsoft.Json;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.Session
{
    public class VaultFolder 
    {
        [JsonProperty("path")]
        public string Path { get; private set; }

        private IFileWatcher _fileWatcher;

        public event EventHandler<FileWatcherEventArgs> Changed;

        [JsonConstructor]
        private VaultFolder()
        {
            Tag = IdentityPublicTag.Empty;
            KeyShares = new List<EmailAddress>();
        }

        public VaultFolder(string path, IdentityPublicTag publicTag)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Path = path.NormalizeFolderPath();
            Tag = publicTag;
            KeyShares = new List<EmailAddress>();
            InitializeFileWatcher();
        }

        public VaultFolder(VaultFolder vaultFolder, IEnumerable<UserPublicKey> keyShares)
        {
            if (vaultFolder == null)
            {
                throw new ArgumentNullException(nameof(vaultFolder));
            }

            Path = vaultFolder.Path;
            Tag = vaultFolder.Tag;
            KeyShares = keyShares.Select(ks => ks.Email).ToArray();
        }

        private void InitializeFileWatcher()
        {
            if (New<IDataContainer>(Path).IsAvailable)
            {
                _fileWatcher = New<IFileWatcher>(Path);
                _fileWatcher.FileChanged += _VaultfileWatcher_FileChanged!;
                _fileWatcher.IncludeSubdirectories = true;
            }
        }

        private void _VaultfileWatcher_FileChanged(object sender, FileWatcherEventArgs e)
        {
            if (!New<LicensePolicy>().Capabilities.Has(LicenseCapability.Vault))
            {
                return;
            }

            OnChanged(e);
        }

        protected virtual void OnChanged(FileWatcherEventArgs e)
        {
            Changed?.Invoke(this, e);
        }

        [JsonProperty("publicTag")]
        public IdentityPublicTag Tag
        {
            get;
            private set;
        }

        [JsonProperty("keyShares")]
        public IEnumerable<EmailAddress> KeyShares
        {
            get;
            private set;
        }

        public bool Matches(string path)
        {
            return string.Compare(Path, path, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public void Dispose()
        {
            _fileWatcher.Dispose();
        }
    }
}
