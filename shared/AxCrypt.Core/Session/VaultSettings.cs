#region Coypright and License

/*
 * AxCrypt - Copyright 2025, AxCrypt AB, All Rights Reserved
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
using AxCrypt.Core.IO;
using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.Session
{
    public class VaultSettings
    {

        private VaultFolder _vaultFolder { get; set; }

        public void InitializeVaultSettings()
        {
            string vaultFolderpath = New<UserSettings>().VaultEncryptDataPath;

            if (string.IsNullOrEmpty(vaultFolderpath) || !New<IDataContainer>(vaultFolderpath).IsAvailable)
            {
                return;
            }

            CreateVaultWatchedFolderAsync(new VaultFolder(vaultFolderpath));
        }

        public async Task CreateVaultWatchedFolderAsync(VaultFolder vaultFolder)
        {
            if (vaultFolder == null)
            {
                throw new ArgumentNullException("vaultFolder");
            }

            if (string.IsNullOrEmpty(vaultFolder.Path) || !New<IDataContainer>(vaultFolder.Path).IsAvailable)
            {
                await New<IPopup>().ShowAsync(
                    PopupButtons.Ok,
                    Texts.WarningTitle,
                    Texts.InvalidVaultSetting);

                return;
            }

            vaultFolder.Changed += vaultWatchedFolder_Changed;
            _vaultFolder = vaultFolder;

            await AddVaultWatchedFolderAsync(vaultFolder.Path);
        }

        public async Task AddVaultWatchedFolderAsync(string vaultFolder)
        {
            await Resolve.SessionNotify.NotifyAsync(new SessionNotification(SessionNotificationType.VaultFolderAdded, Resolve.KnownIdentities.DefaultEncryptionIdentity, vaultFolder));
        }

        private async void vaultWatchedFolder_Changed(object sender, FileWatcherEventArgs e)
        {
            VaultFolder vaultFolder = (VaultFolder)sender;
            foreach (string fullName in e.FullNames)
            {
                IDataItem dataItem = New<IDataItem>(fullName);
                await Resolve.SessionNotify.NotifyAsync(new SessionNotification(SessionNotificationType.VaultFolderChange, dataItem.FullName));
            }
        }

        public virtual async Task RemoveAndDecryptVaultWatchedFolder(IDataItem dataItem)
        {
            if (dataItem == null)
            {
                throw new ArgumentNullException("folderInfo");
            }

            await Resolve.SessionNotify.NotifyAsync(new SessionNotification(SessionNotificationType.VaultFolderRemoved, Resolve.KnownIdentities.DefaultEncryptionIdentity, dataItem.FullName));
        }
    }
}
