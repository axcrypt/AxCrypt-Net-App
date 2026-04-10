using AxCrypt.Api.Model;
using AxCrypt.Api.Model.Entitlement;
using AxCrypt.Core.Crypto;

namespace AxCrypt.Core.Service.Entitlement
{
    public class NullEntitlementService : IEntitlementService
    {
        private static readonly Task<bool> _completedTask = Task.FromResult(true);

        public NullEntitlementService(LogOnIdentity identity)
        {
            Identity = identity;
        }

        public IEntitlementService Refresh()
        {
            return this;
        }

        public LogOnIdentity Identity
        {
            get; private set;
        }

        public Task<EntitlementApiModel> GetUserUsageCountAsync(string subsLevel)
        {
            return Task.FromResult(EntitlementApiModel.Empty);
        }

        public Task<bool> InsertUserUsageCount(EntitlementRequestOptions secrets)
        {
            return _completedTask;
        }

        public Task<bool> SyncUserUsageCount(EntitlementApiModel secrets)
        {
            return _completedTask;
        }
    }
}