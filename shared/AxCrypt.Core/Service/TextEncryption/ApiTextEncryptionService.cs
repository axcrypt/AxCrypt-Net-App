#region Coypright and License

/*
 * AxCrypt - Copyright 2026, All Rights Reserved
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
 * The source is maintained at https://bitbucket.org/axcryptab/axcrypt-net-git please visit for
 * updates, contributions and contact with the author. You may also visit
 * http://www.axcrypt.net for more information about the author.
*/

#endregion Coypright and License

using AxCrypt.Api;
using AxCrypt.Api.Model;
using AxCrypt.Api.Model.TextEncryption;
using AxCrypt.Common;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.UI;

namespace AxCrypt.Core.Service.TextEncryption
{
    public class ApiTextEncryptionService : ITextEncryptionService
    {
        private AxTextEncryptionApiClient _apiClient;

        public ApiTextEncryptionService(AxTextEncryptionApiClient apiClient)
        {
            if (apiClient == null)
            {
                throw new ArgumentNullException(nameof(apiClient));
            }

            _apiClient = apiClient;
        }

        public ITextEncryptionService Refresh()
        {
            return this;
        }

        public LogOnIdentity Identity
        {
            get
            {
                return new LogOnIdentity(EmailAddress.Parse(_apiClient.Identity.User), Passphrase.Create(_apiClient.Identity.Password));
            }
        }

        public async Task<Guid> ShareTextAsync(TextEncryptionApiModel textEncryptionApiModel)
        {
            if (string.IsNullOrEmpty(_apiClient.Identity.User))
            {
                throw new InvalidOperationException("The account service requires a user.");
            }

            try
            {
                Guid result = await _apiClient.ShareTextAsync(textEncryptionApiModel).Free();
                return result;
            }
            catch (UnauthorizedException ex)
            {
                throw ex;
            }
        }

        public async Task<IEnumerable<UserPublicKey>> GetUserPublicKeyAsync(IEnumerable<EmailAddress> sharedUsers)
        {
            if (string.IsNullOrEmpty(_apiClient.Identity.User))
            {
                throw new InvalidOperationException("The account service requires a user.");
            }

            try
            {
                IEnumerable<string> users = sharedUsers.Select(ue => ue.Address);

                IEnumerable<AccountKey> userAccountKeys = (await _apiClient.GetUserPublicKeyAsync(users).Free());
                if (userAccountKeys == null)
                {
                    return Enumerable.Empty<UserPublicKey>();
                }

                return userAccountKeys.Select(uak => uak.ToUserPublicKey());
            }
            catch (UnauthorizedException ex)
            {
                throw ex;
            }
        }
    }
}