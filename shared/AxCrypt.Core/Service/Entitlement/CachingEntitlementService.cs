using AxCrypt.Abstractions;
using AxCrypt.Api.Model;
using AxCrypt.Api.Model.Entitlement;
using AxCrypt.Common;
using AxCrypt.Core.Crypto;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.Service.Entitlement
{
    public class CachingEntitlementService : IEntitlementService
    {
        private IEntitlementService _service;

        private CacheKey _key;

        public CachingEntitlementService(IEntitlementService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            _service = service;
            _key = CacheKey.RootKey.Subkey(nameof(CachingEntitlementService)).Subkey(service.Identity.UserEmail.Address).Subkey(service.Identity.Tag.ToString());
        }

        public IEntitlementService Refresh()
        {
            New<ICache>().RemoveItem(_key);
            return this;
        }

        public LogOnIdentity Identity
        {
            get
            {
                return _service.Identity;
            }
        }

        public async Task<EntitlementApiModel> GetUserUsageCountAsync(string subsLevel)
        {
            return await New<ICache>().UpdateItemAsync(async () => await _service.GetUserUsageCountAsync(subsLevel)).Free();
        }

        public async Task<bool> InsertUserUsageCount(EntitlementRequestOptions requestOptions)
        {
            return await New<ICache>().UpdateItemAsync(async () => await _service.InsertUserUsageCount(requestOptions), _key).Free();
        }

        public async Task<bool> SyncUserUsageCount(EntitlementApiModel entitlementApiModel)
        {
            return await New<ICache>().UpdateItemAsync(async () => await _service.SyncUserUsageCount(entitlementApiModel), _key).Free();
        }
    }
}