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


using AxCrypt.Content;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.IO;
using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.Session
{
    public class VaultSettings : IDisposable
    {
        private readonly Dictionary<string, VaultFolder> _watchedFolders = new();

        public async Task InitializeVaultSettings()
        {
            string vaultFolderpath = New<UserSettings>().VaultEncryptDataPath;
            if (string.IsNullOrEmpty(vaultFolderpath) || !New<IDataContainer>(vaultFolderpath).IsAvailable)
            {
                return;
            }

            await CreateVaultWatchedFolderAsync(new VaultFolder(vaultFolderpath, Resolve.KnownIdentities.DefaultEncryptionIdentity.Tag));
        }

        public async Task CreateVaultWatchedFolderAsync(VaultFolder vaultFolder)
        {
            ArgumentNullException.ThrowIfNull(vaultFolder);

            if (!await IsValidVaultPath(vaultFolder.Path))
                return;

            string VaultFolderPath = vaultFolder.Path;

            if (!VaultFolderPath.EndsWith("\\"))
                VaultFolderPath += "\\";

            if (_watchedFolders.ContainsKey(VaultFolderPath))
                return;

            vaultFolder.Changed += VaultWatchedFolder_Changed!;
            _watchedFolders.Add(VaultFolderPath, vaultFolder);

            await AddVaultWatchedFolderAsync(VaultFolderPath);
        }

        public async Task AddVaultWatchedFolderAsync(string vaultFolder)
        {
            await Resolve.SessionNotify.NotifyAsync(new SessionNotification(SessionNotificationType.VaultFolderAdded, Resolve.KnownIdentities.DefaultEncryptionIdentity, vaultFolder));
        }

        private async void VaultWatchedFolder_Changed(object sender, FileWatcherEventArgs e)
        {
            await Resolve.SessionNotify.NotifyAsync(new SessionNotification(SessionNotificationType.VaultFolderChange, e.FullNames));
        }

        public void RemoveVaultWatchedFolder(string folderPath)
        {
            if (_watchedFolders.ContainsKey(folderPath))
                _watchedFolders.Remove(folderPath);
        }

        public virtual async Task RemoveAndDecryptVaultWatchedFolder(IDataItem folderInfo)
        {
            if (folderInfo == null)
            {
                throw new ArgumentNullException(nameof(folderInfo));
            }

            await Resolve.SessionNotify.NotifyAsync(new SessionNotification(SessionNotificationType.VaultFolderRemoved, Resolve.KnownIdentities.DefaultEncryptionIdentity, folderInfo.FullName));
        }

        public async Task<bool> IsValidVaultPath(string vaultFolderPath)
        {
            if (string.IsNullOrEmpty(vaultFolderPath) || !New<IDataContainer>(vaultFolderPath).IsAvailable)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle,
                    Texts.InvalidVaultSetting);
                return false;
            }

            if (New<FileFilter>().IsForbiddenFolder(vaultFolderPath))
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.SystemFolderForbiddenText.InvariantFormat(vaultFolderPath));
                return false;
            }

            bool isWatched = New<FileSystemState>().WatchedFolders.Any(wf => vaultFolderPath.Trim('\\').Contains(wf.Path.Trim('\\')));
            if (isWatched)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle,
                    Texts.UnableAddSecuredFolderText);
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            foreach (VaultFolder watcher in _watchedFolders.Values)
            {
                watcher.Dispose();
            }
        }
    }
}
