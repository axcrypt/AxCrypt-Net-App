using AxCrypt.Api;
using AxCrypt.Api.Model;
using AxCrypt.Api.Model.Entitlement;
using AxCrypt.Common;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;

namespace AxCrypt.Core.Service.Entitlement
{
    public class ApiEntitlementService : IEntitlementService
    {
        private AxEntitlementApiClient _apiClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiEntitlementService"/> class.
        /// </summary>
        /// <param name="apiClient">The API client to use.</param>
        public ApiEntitlementService(AxEntitlementApiClient apiClient)
        {
            if (apiClient == null)
            {
                throw new ArgumentNullException(nameof(apiClient));
            }

            _apiClient = apiClient;
        }

        public IEntitlementService Refresh()
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

        /// <summary>
        /// Fetches the user usage count.
        /// </summary>
        /// <returns>
        /// The user usage count.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">The account service requires a user.</exception>
        public async Task<EntitlementApiModel> GetUserUsageCountAsync(string subsLevel)
        {
            if (string.IsNullOrEmpty(_apiClient.Identity.User))
            {
                throw new InvalidOperationException("The account service requires a user.");
            }

            try
            {
                EntitlementApiModel userSecrets = await _apiClient.GetUserUsageCountAsync(Identity.UserEmail.Address, subsLevel).Free();
                return userSecrets;
            }
            catch (UnauthorizedException)
            {
                return EntitlementApiModel.Empty;
            }
        }

        /// <summary>
        /// Saves the user usage count.
        /// </summary>
        /// <param name="requestApiModel">The entitlement count.</param>
        public async Task<bool> InsertUserUsageCount(EntitlementRequestOptions requestApiModel)
        {
            if (string.IsNullOrEmpty(_apiClient.Identity.User))
            {
                throw new InvalidOperationException("The account service requires a user.");
            }

            try
            {
                return await _apiClient.InsertUserUsageCount(requestApiModel).Free();
            }
            catch (UnauthorizedException uaex)
            {
                throw new PasswordException("Credentials are not valid for server access.", uaex);
            }
        }

        /// <summary>
        /// Sync the user usage count.
        /// </summary>
        /// <param name="entitlementApiModel">The entitlement count.</param>
        public async Task<bool> SyncUserUsageCount(EntitlementApiModel entitlementApiModel)
        {
            if (string.IsNullOrEmpty(_apiClient.Identity.User))
            {
                throw new InvalidOperationException("The account service requires a user.");
            }

            try
            {
                return await _apiClient.SyncUserUsageCount(entitlementApiModel).Free();
            }
            catch (UnauthorizedException uaex)
            {
                throw new PasswordException("Credentials are not valid for server access.", uaex);
            }
        }
    }
}