using AxCrypt.Api.Model;
using AxCrypt.Api.Model.Entitlement;
using AxCrypt.Common;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Extensions;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.Service.Entitlement
{
    /// <summary>
    /// Implement entitlement service functionality for a device, using a local and a remote service instance. This
    /// class determines the interaction and behavior when the services cooperate to provide a robust service
    /// despite possible remote outages.
    /// An instance operates on behalf an identity, or an anonymous one.
    /// </summary>
    public class DeviceEntitlementService : IEntitlementService
    {
        private IEntitlementService _localService;

        private IEntitlementService _remoteService;

        public DeviceEntitlementService(IEntitlementService localService, IEntitlementService remoteService)
        {
            _localService = localService;
            _remoteService = remoteService;
        }

        public IEntitlementService Refresh()
        {
            return this;
        }

        public LogOnIdentity Identity
        {
            get
            {
                return _remoteService.Identity;
            }
        }

        /// <summary>
        /// Fetches the free user usage count.
        /// </summary>
        /// <returns>
        /// The free user usage count.
        /// </returns>
        public async Task<EntitlementApiModel> GetUserUsageCountAsync(string subsLevel)
        {
            EntitlementApiModel localUserSecrets = await _localService.GetUserUsageCountAsync(subsLevel).Free();
            if (!New<AxCryptOnlineState>().IsOnline || Identity == LogOnIdentity.Empty)
            {
                return localUserSecrets;
            }

            try
            {
                EntitlementApiModel remoteSecrets = await _remoteService.GetUserUsageCountAsync(subsLevel).Free();
                if (remoteSecrets == null)
                {
                    return localUserSecrets;
                }

                if (localUserSecrets != remoteSecrets)
                {
                    await _localService.SyncUserUsageCount(remoteSecrets).Free();
                }

                return remoteSecrets;
            }
            catch (ApiException aex)
            {
                await aex.HandleApiExceptionAsync();
            }

            return localUserSecrets;
        }

        public async Task<bool> InsertUserUsageCount(EntitlementRequestOptions entitlementRequest)
        {
            if (New<AxCryptOnlineState>().IsOnline)
            {
                try
                {
                    await _remoteService.InsertUserUsageCount(entitlementRequest).Free();
                }
                catch (ApiException aex)
                {
                    await aex.HandleApiExceptionAsync();
                }
            }

            return await _localService.InsertUserUsageCount(entitlementRequest).Free();
        }

        public async Task<bool> SyncUserUsageCount(EntitlementApiModel entitlementRequest)
        {
            if (New<AxCryptOnlineState>().IsOnline)
            {
                try
                {
                    await _remoteService.SyncUserUsageCount(entitlementRequest).Free();
                }
                catch (ApiException aex)
                {
                    await aex.HandleApiExceptionAsync();
                }
            }

            return await _localService.SyncUserUsageCount(entitlementRequest).Free();
        }
    }
}