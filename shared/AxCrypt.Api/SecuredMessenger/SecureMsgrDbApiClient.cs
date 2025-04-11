using AxCrypt.Abstractions;
using AxCrypt.Abstractions.Rest;
using AxCrypt.Api.Model;
using AxCrypt.Api.Model.SecuredMessenger;
using AxCrypt.Common;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Api.SecuredMessenger
{
    public class SecureMsgrDbApiClient
    {
        private Uri BaseUrl { get; }

        private TimeSpan Timeout { get; }

        private ApiCaller Caller { get; } = new ApiCaller();

        /// <summary>
        /// Initializes a new instance of the <see cref="AxCryptApiClient"/> class.
        /// </summary>
        /// <param name="identity">The identity on whos behalf to make the call.</param>
        public SecureMsgrDbApiClient(RestIdentity identity, Uri baseUrl, TimeSpan timeout)
        {
            Identity = identity;
            BaseUrl = baseUrl;
            Timeout = timeout;
        }

        public RestIdentity Identity { get; }

        public async Task<IEnumerable<SecuredMessengerApiModel>> GetListAsync(RequestOptions requestOptions)
        {
            Uri resource = BaseUrl.PathCombine($"securedmessenger/inbox/list");

            RestContent reqContent = new RestContent(Serializer.Serialize(requestOptions));

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("POST", resource, Timeout, reqContent)).Free();

            ApiCaller.EnsureStatusOk(restResponse);

            return Serializer.Deserialize<IEnumerable<SecuredMessengerApiModel>>(restResponse.Content);
        }

        public async Task<IEnumerable<SecuredMessengerApiModel>> GetSentListAsync(RequestOptions requestOptions)
        {
            Uri resource = BaseUrl.PathCombine($"securedmessenger/sent/list");

            RestContent reqContent = new RestContent(Serializer.Serialize(requestOptions));

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("POST", resource, Timeout, reqContent)).Free();

            ApiCaller.EnsureStatusOk(restResponse);

            return Serializer.Deserialize<IEnumerable<SecuredMessengerApiModel>>(restResponse.Content);
        }

        public async Task<IEnumerable<SecuredMessengerApiModel>> GetUnreadListAsync(RequestOptions requestOptions)
        {
            Uri resource = BaseUrl.PathCombine($"securedmessenger/unread/list");

            RestContent reqContent = new RestContent(Serializer.Serialize(requestOptions));

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("POST", resource, Timeout, reqContent)).Free();

            ApiCaller.EnsureStatusOk(restResponse);

            return Serializer.Deserialize<IEnumerable<SecuredMessengerApiModel>>(restResponse.Content);
        }

        public async Task<bool> PostCreateAsync(SecuredMessengerApiModel messenger)
        {
            if (messenger == null)
            {
                throw new ArgumentNullException(nameof(messenger));
            }

            Uri resource = BaseUrl.PathCombine("securedmessenger/send".With());

            RestContent reqContent = new RestContent(Serializer.Serialize(messenger));

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("POST", resource, Timeout, reqContent)).Free();

            ApiCaller.EnsureStatusOk(restResponse);

            return Serializer.Deserialize<bool>(restResponse.Content);
        }

        public async Task<SecuredMessengerRootApiModel> GetAsync(Guid id, string userEmail)
        {
            Uri resource = BaseUrl.PathCombine($"securedmessenger/get/{id}?userEmail={ApiCaller.EncodePathParams(userEmail)}");

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("GET", resource, Timeout)).Free();

            ApiCaller.EnsureStatusOk(restResponse);

            return Serializer.Deserialize<SecuredMessengerRootApiModel>(restResponse.Content);
        }

        public async Task<bool> UpdateAsync(IEnumerable<Guid> ids, string userEmail, bool isUnread)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            Uri resource = BaseUrl.PathCombine($"securedmessenger/updatestatus/{ApiCaller.EncodePathParams(userEmail)}?isUnread={isUnread}");

            RestContent reqContent = new RestContent(Serializer.Serialize(ids));

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("PUT", resource, Timeout, reqContent)).Free();

            ApiCaller.EnsureStatusOk(restResponse);

            return Serializer.Deserialize<bool>(restResponse.Content);
        }

        public async Task<bool> DeleteAsync(IEnumerable<Guid> ids, string userEmail, SecureMsgrFilterTab securedMessengerFilter)
        {
            Uri resource = BaseUrl.PathCombine($"securedmessenger/{securedMessengerFilter}/delete/{ApiCaller.EncodePathParams(userEmail)}");

            RestContent reqContent = new RestContent(Serializer.Serialize(ids));

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("PUT", resource, Timeout, reqContent)).Free();

            ApiCaller.EnsureStatusOk(restResponse);

            return Serializer.Deserialize<bool>(restResponse.Content);
        }

        public async Task<IEnumerable<SecuredMessengerRootApiModel>> GetSecMsgWithSearchFilterAsync(SecureMsgrFilterTab securedMessengerFilter, RequestOptions requestOptions)
        {
            Uri resource = BaseUrl.PathCombine($"securedmessenger/{securedMessengerFilter}/searchfilter");

            RestContent reqContent = new RestContent(Serializer.Serialize(requestOptions));

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("POST", resource, Timeout, reqContent)).Free();

            ApiCaller.EnsureStatusOk(restResponse);

            return Serializer.Deserialize<IEnumerable<SecuredMessengerRootApiModel>>(restResponse.Content);
        }

        /// <summary>
        /// Gets the public key of any user. If the user does not exist, he or she is invited by the current user.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <returns></returns>
        public async Task<AccountKey> GetAllAccountsOtherUserPublicKeyAsync(string userName)
        {
            Uri resource = BaseUrl.PathCombine($"securedmessenger/useraccount/{ApiCaller.EncodePathParams(userName)}/key");

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("GET", resource, Timeout)).Free();
            ApiCaller.EnsureStatusOk(restResponse);

            AccountKey accountKey = Serializer.Deserialize<AccountKey>(restResponse.Content);
            return accountKey;
        }

        public async Task<long> GetFreeUserSecuredMessengerLimit(string userEmail)
        {
            if (userEmail == null)
            {
                throw new ArgumentNullException(nameof(userEmail));
            }

            Uri resource = BaseUrl.PathCombine($"securedmessenger/nonpayinguser/sendlimit?userEmail=" + ApiCaller.PathSegmentEncode(userEmail));

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest(resource, Timeout)).Free();
            ApiCaller.EnsureStatusOk(restResponse);

            return Serializer.Deserialize<long>(restResponse.Content);
        }

        public async Task<bool> UpdateFreeUserSecuredMessengerLimit(string userEmail)
        {
            if (userEmail == null)
            {
                throw new ArgumentNullException(nameof(userEmail));
            }

            Uri resource = BaseUrl.PathCombine($"securedmessenger/nonpayinguser/sendlimit?userEmail=" + ApiCaller.PathSegmentEncode(userEmail));

            RestContent content = new RestContent(Serializer.Serialize(userEmail));
            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("POST", resource, Timeout, content)).Free();
            ApiCaller.EnsureStatusOk(restResponse);

            return Serializer.Deserialize<bool>(restResponse.Content);
        }

        #region Private helpers

        private static IStringSerializer Serializer
        {
            get
            {
                return New<IStringSerializer>();
            }
        }

        #endregion Private helpers
    }
}