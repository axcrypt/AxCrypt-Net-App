using AxCrypt.Api.Model;
using AxCrypt.Api.Model.Entitlement;
using AxCrypt.Core.Crypto;

namespace AxCrypt.Core.Service.Entitlement
{
    /// <summary>
    /// The account service. Methods and properties to work with an account.
    /// </summary>
    public interface IEntitlementService
    {
        /// <summary>
        /// Refresh all values by ensuring flushing any caches etc.
        /// </summary>
        IEntitlementService Refresh();

        /// <summary>
        /// Gets the identity this instance works with.
        /// </summary>
        /// <value>
        /// The identity.
        /// </value>
        LogOnIdentity Identity { get; }

        /// <summary>
        /// Gets the user usage count.
        /// </summary>
        /// <returns>A free user used and max usage count.</returns>
        Task<EntitlementApiModel> GetUserUsageCountAsync(string subsLevel);

        /// <summary>
        /// save the user usage.
        /// </summary>
        Task<bool> InsertUserUsageCount(EntitlementRequestOptions requestOptions);

        /// <summary>
        /// sync the user usage.
        /// </summary>
        Task<bool> SyncUserUsageCount(EntitlementApiModel requestOptions);
    }
}