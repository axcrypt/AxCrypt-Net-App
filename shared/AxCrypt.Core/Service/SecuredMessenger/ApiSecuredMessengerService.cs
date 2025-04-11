#region Coypright and License

/*
 * AxCrypt - Copyright 2023, All Rights Reserved
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
using AxCrypt.Api.Model.SecuredMessenger;
using AxCrypt.Api.SecuredMessenger;
using AxCrypt.Common;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.UI;
using AxCrypt.Core.Extensions;

namespace AxCrypt.Core.Service.SecuredMessenger
{
    public class ApiSecuredMessengerService : ISecuredMessengerService
    {
        private SecureMsgrDbApiClient _apiClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiSecuredMessengerService"/> class.
        /// </summary>
        /// <param name="apiClient">The API client to use.</param>
        public ApiSecuredMessengerService(SecureMsgrDbApiClient apiClient)
        {
            if (apiClient == null)
            {
                throw new ArgumentNullException(nameof(apiClient));
            }

            _apiClient = apiClient;
        }

        public ISecuredMessengerService Refresh()
        {
            return this;
        }

        /// <summary>
        /// Gets the identity this instance works with.
        /// </summary>
        /// <value>
        /// The identity.
        /// </value>
        public LogOnIdentity Identity
        {
            get
            {
                return new LogOnIdentity(EmailAddress.Parse(_apiClient.Identity.User), Passphrase.Create(_apiClient.Identity.Password));
            }
        }

        public async Task<IEnumerable<SecuredMessengerApiModel>> GetListAsync(RequestOptions requestOptions)
        {
            try
            {
                return await _apiClient.GetListAsync(requestOptions);
            }
            catch (ApiException ex)
            {
                throw ex;
            }
            catch (UnauthorizedException)
            {
            }
            return new List<SecuredMessengerApiModel>();
        }

        public async Task<IEnumerable<SecuredMessengerApiModel>> GetSentListAsync(RequestOptions requestOptions)
        {
            try
            {
                return await _apiClient.GetSentListAsync(requestOptions);
            }
            catch (ApiException ex)
            {
                throw ex;
            }
            catch (UnauthorizedException)
            {
            }
            return new List<SecuredMessengerApiModel>();
        }

        public async Task<IEnumerable<SecuredMessengerApiModel>> GetUnreadListAsync(RequestOptions requestOptions)
        {
            try
            {
                return await _apiClient.GetUnreadListAsync(requestOptions);
            }
            catch (ApiException ex)
            {
                throw ex;
            }
            catch (UnauthorizedException)
            {
            }
            return new List<SecuredMessengerApiModel>();
        }

        public async Task<bool> CreateAsync(SecuredMessengerApiModel messengerApiModel)
        {
            try
            {
                return await _apiClient.PostCreateAsync(messengerApiModel);
            }
            catch (ApiException)
            {
            }
            catch (UnauthorizedException)
            {
            }
            return false;
        }

        public async Task<SecuredMessengerRootApiModel> GetAsync(Guid id, string userEmail)
        {
            try
            {
                return await _apiClient.GetAsync(id, userEmail);
            }
            catch (ApiException ex)
            {
                throw ex;
            }
            catch (UnauthorizedException)
            {
            }
            return new SecuredMessengerRootApiModel();
        }

        public async Task<bool> UpdateAsync(IEnumerable<Guid> ids, string userEmail, bool isUnread)
        {
            try
            {
                return await _apiClient.UpdateAsync(ids, userEmail, isUnread);
            }
            catch (ApiException)
            {
            }
            catch (UnauthorizedException)
            {
            }
            return false;
        }

        public async Task<bool> DeleteAsync(IEnumerable<Guid> ids, string user, SecureMsgrFilterTab securedMessengerFilter)
        {
            try
            {
                return await _apiClient.DeleteAsync(ids, user, securedMessengerFilter);
            }
            catch (ApiException)
            {
            }
            catch (UnauthorizedException)
            {
            }
            return false;
        }

        public Task<bool> SaveMessagelist(SecuredMessengerApiModel model)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SavemessagesAsync(SecuredMessengerRootApiModel model)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<SecuredMessengerRootApiModel>> GetSecMsgWithSearchFiltersAsync(SecureMsgrFilterTab securedMessengerFilterTab, RequestOptions requestOptions)
        {
            try
            {
                return await _apiClient.GetSecMsgWithSearchFilterAsync(securedMessengerFilterTab, requestOptions);
            }
            catch (ApiException)
            {
            }
            catch (UnauthorizedException)
            {
            }
            return new List<SecuredMessengerRootApiModel>();
        }

        public async Task<UserPublicKey> OtherPublicKeyAsync(EmailAddress email)
        {
            return (await _apiClient.GetAllAccountsOtherUserPublicKeyAsync(email.Address).Free()).ToUserPublicKey();
        }

        public async Task<long> GetFreeUserSecuredMessengerLimit(string userEmail)
        {
            if (string.IsNullOrEmpty(_apiClient.Identity.User))
            {
                throw new InvalidOperationException("The account service requires a user.");
            }
            try
            {
                return await _apiClient.GetFreeUserSecuredMessengerLimit(userEmail).Free();
            }
            catch (UnauthorizedException)
            {
                return 0;
            }
        }

        public async Task<bool> UpdateFreeUserSecuredMessengerLimit(string userEmail)
        {
            if (string.IsNullOrEmpty(_apiClient.Identity.User))
            {
                throw new InvalidOperationException("The account service requires a user.");
            }
            try
            {
                return await _apiClient.UpdateFreeUserSecuredMessengerLimit(userEmail).Free();
            }
            catch (UnauthorizedException)
            {
                return false;
            }
        }
    }
}