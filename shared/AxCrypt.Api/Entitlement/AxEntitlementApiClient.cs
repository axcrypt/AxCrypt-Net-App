using AxCrypt.Abstractions;
using AxCrypt.Abstractions.Rest;
using AxCrypt.Api.Model;
using AxCrypt.Api.Model.Entitlement;
using AxCrypt.Common;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Api
{
    /// <summary>
    /// Provide basic api services using the AxCrypt API. All connection errors are thrown as OfflineApiExceptions, which must be caught and
    /// handled by the caller, and should be treated as 'temporarily offline'. They root cause can be both Internet connection issues as well
    /// as the servers being down.
    /// </summary>
    public class AxEntitlementApiClient
    {
        private Uri BaseUrl { get; }

        private TimeSpan Timeout { get; }

        private ApiCaller Caller { get; } = new ApiCaller();

        /// <summary>
        /// Initializes a new instance of the <see cref="AxEntitlementApiClient"/> class.
        /// </summary>
        /// <param name="identity">The identity on whos behalf to make the call.</param>
        public AxEntitlementApiClient(RestIdentity identity, Uri baseUrl, TimeSpan timeout)
        {
            Identity = identity;
            BaseUrl = baseUrl;
            Timeout = timeout;
        }

        public RestIdentity Identity { get; }

        /// <summary>
        /// Get a user usage count.
        /// </summary>
        /// <param name="email">The user name/email</param>
        /// <returns>The user secrets</returns>
        public async Task<EntitlementApiModel> GetUserUsageCountAsync(string userEmail, string subsLevel)
        {
            if (userEmail == null)
            {
                throw new ArgumentNullException(nameof(userEmail));
            }

            Uri resource = BaseUrl.PathCombine("entitlement/user/get");
            EntitlementRequestOptions entitlementApiModel = new(userEmail, null!)
            {
                SubscriptionLevel = subsLevel,
            };
            RestContent content = new RestContent(Serializer.Serialize(entitlementApiModel));
            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("POST", resource, Timeout, content)).Free();
            ApiCaller.EnsureStatusOk(restResponse);
            EntitlementApiModel userSecrets = Serializer.Deserialize<EntitlementApiModel>(restResponse.Content);
            return userSecrets;
        }

        /// <summary>
        /// Insert and Update a user usage count.
        /// </summary>
        /// <param name="email">The user name/email</param>
        /// <param feature="feature">The user used feature</param>
        /// <returns>The Free user used feature</returns>
        public async Task<bool> InsertUserUsageCount(EntitlementRequestOptions entitlementApiModel)
        {
            if (entitlementApiModel == null)
            {
                throw new ArgumentNullException(nameof(entitlementApiModel));
            }

            Uri resource = BaseUrl.PathCombine("entitlement/user/save");
            RestContent content = new RestContent(Serializer.Serialize(entitlementApiModel));

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("POST", resource, Timeout, content)).Free();
            ApiCaller.EnsureStatusOk(restResponse);

            return Serializer.Deserialize<bool>(restResponse.Content);
        }

        /// <summary>
        /// Sync the user usage count.
        /// </summary>
        /// <param name="email">The user name/email</param>
        /// <param feature="feature">The user used feature</param>
        /// <returns>Updated status as true/false</returns>
        public async Task<bool> SyncUserUsageCount(EntitlementApiModel entitlementApiModel)
        {
            if (entitlementApiModel == null)
            {
                throw new ArgumentNullException(nameof(entitlementApiModel));
            }

            Uri resource = BaseUrl.PathCombine("entitlement/user/update");
            RestContent content = new RestContent(Serializer.Serialize(entitlementApiModel));

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("POST", resource, Timeout, content)).Free();
            ApiCaller.EnsureStatusOk(restResponse);

            return Serializer.Deserialize<bool>(restResponse.Content);
        }

        private static IStringSerializer Serializer
        {
            get
            {
                return New<IStringSerializer>();
            }
        }
    }
}
