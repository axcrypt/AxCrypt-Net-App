using AxCrypt.Abstractions;
using AxCrypt.Abstractions.Rest;
using AxCrypt.Api.Model;
using AxCrypt.Api.Model.TextEncryption;
using AxCrypt.Common;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Api
{
    /// <summary>
    /// Provide basic api services using the AxCrypt API. All connection errors are thrown as OfflineApiExceptions, which must be caught and
    /// handled by the caller, and should be treated as 'temporarily offline'. They root cause can be both Internet connection issues as well
    /// as the servers being down.
    /// </summary>
    public class AxTextEncryptionApiClient
    {
        private Uri BaseUrl { get; }

        private TimeSpan Timeout { get; }

        private ApiCaller Caller { get; } = new ApiCaller();

        /// <summary>
        /// Initializes a new instance of the <see cref="AxTextEncryptionApiClient"/> class.
        /// </summary>
        /// <param name="identity">The identity on whos behalf to make the call.</param>
        public AxTextEncryptionApiClient(RestIdentity identity, Uri baseUrl, TimeSpan timeout)
        {
            Identity = identity;
            BaseUrl = baseUrl;
            Timeout = timeout;
        }

        public RestIdentity Identity { get; }

        public async Task<Guid> ShareTextAsync(TextEncryptionApiModel textEncryptionApiModel)
        {
            if (textEncryptionApiModel == null)
            {
                throw new ArgumentNullException(nameof(textEncryptionApiModel));
            }

            Uri resource = BaseUrl.PathCombine("textencryption/share".With());

            RestContent content = new RestContent(Serializer.Serialize(textEncryptionApiModel));
            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("POST", resource, Timeout, content)).Free();
            ApiCaller.EnsureStatusOk(restResponse);

            return Serializer.Deserialize<Guid>(restResponse.Content);
        }

        public async Task<IEnumerable<AccountKey>> GetUserPublicKeyAsync(IEnumerable<string> userEmails)
        {
            if (userEmails == null)
            {
                throw new ArgumentNullException(nameof(userEmails));
            }

            Uri resource = BaseUrl.PathCombine($"textencryption/share/key");
            RestContent content = new RestContent(Serializer.Serialize(userEmails));
            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("POST", resource, Timeout, content)).Free();
            ApiCaller.EnsureStatusOk(restResponse);

            IEnumerable<AccountKey> accountKeys = Serializer.Deserialize<IEnumerable<AccountKey>>(restResponse.Content);
            return accountKeys;
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