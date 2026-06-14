using AxCrypt.Abstractions;
using AxCrypt.Abstractions.Rest;
using AxCrypt.Api.Model;
using AxCrypt.Api.Model.Groups;
using AxCrypt.Api.Model.MFA;
using AxCrypt.Common;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Api
{
    /// <summary>
    /// Provide basic api services using the AxCrypt API. All connection errors are thrown as OfflineApiExceptions, which must be caught and
    /// handled by the caller, and should be treated as 'temporarily offline'. They root cause can be both Internet connection issues as well
    /// as the servers being down.
    /// </summary>
    public class AxCryptApiClient
    {
        private Uri BaseUrl { get; }

        private TimeSpan Timeout { get; }

        private ApiCaller Caller { get; } = new ApiCaller();

        /// <summary>
        /// Initializes a new instance of the <see cref="AxCryptApiClient"/> class.
        /// </summary>
        /// <param name="identity">The identity on whos behalf to make the call.</param>
        public AxCryptApiClient(RestIdentity identity, Uri baseUrl, TimeSpan timeout)
        {
            Identity = identity;
            BaseUrl = baseUrl;
            Timeout = timeout;
        }

        public RestIdentity Identity { get; }

        /// <summary>
        /// Get a user summary anonymously, typically as an initial call to check the status of the account etc.
        /// </summary>
        /// <param name="email">The user name/email</param>
        /// <returns>The user summary</returns>
        public async Task<UserAccount> GetAllAccountsUserAccountAsync(string userName)
        {
            if (userName == null)
            {
                throw new ArgumentNullException(nameof(userName));
            }

            Uri resource = BaseUrl.PathCombine("users/all/accounts/{0}".With(ApiCaller.PathSegmentEncode(userName)));

            RestResponse restResponse = await Caller.RestAsync(new RestIdentity(), new RestRequest(resource, Timeout)).Free();
            if (restResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return new UserAccount(userName, SubscriptionLevel.Unknown, AccountStatus.NotFound);
            }
            if (restResponse.StatusCode == HttpStatusCode.BadRequest)
            {
                return new UserAccount(userName, SubscriptionLevel.Unknown, AccountStatus.InvalidName);
            }
            ApiCaller.EnsureStatusOk(restResponse);

            UserAccount userAccount = Serializer.Deserialize<UserAccount>(restResponse.Content);
            return userAccount;
        }

        /// <summary>
        /// Gets the user account with the provided credentials.
        /// </summary>
        /// <returns>All account information for the user.</returns>
        /// <exception cref="System.InvalidOperationException">There must be an identity and password to attempt to get private account information.</exception>
        public async Task<UserAccount> MyAccountAsync()
        {
            if (string.IsNullOrEmpty(Identity.User) || string.IsNullOrEmpty(Identity.Password))
            {
                throw new InvalidOperationException("There must be an identity and password to attempt to get private account information.");
            }

            Uri resource = BaseUrl.PathCombine("usersv2/my/account");

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest(resource, Timeout)).Free();
            ApiCaller.EnsureStatusOk(restResponse);

            UserAccount userAccount = Serializer.Deserialize<UserAccount>(restResponse.Content);
            return userAccount;
        }

        public async Task<AccountKey> MyAccountKeysCurrentAsync()
        {
            Uri resource = BaseUrl.PathCombine("users/my/account/keys/current");

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest(resource, Timeout)).Free();
            if (restResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            ApiCaller.EnsureStatusOk(restResponse);

            AccountKey accountKey = Serializer.Deserialize<AccountKey>(restResponse.Content);
            return accountKey;
        }

        public async Task PutMyAccountAsync(UserAccount account)
        {
            Uri resource = BaseUrl.PathCombine("users/my/account");

            RestContent content = new RestContent(Serializer.Serialize(account));
            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("PUT", resource, Timeout, content)).Free();
            ApiCaller.EnsureStatusOk(restResponse);
        }

        /// <summary>
        /// Uploads a key pair to server. The operation is idempotent.
        /// </summary>
        /// <param name="accountKeys">The account keys to upload.</param>
        public async Task PutMyAccountKeysAsync(IEnumerable<AccountKey> accountKeys)
        {
            Uri resource = BaseUrl.PathCombine("users/my/account/keys");

            RestContent content = new RestContent(Serializer.Serialize(accountKeys));
            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("PUT", resource, Timeout, content)).Free();
            ApiCaller.EnsureStatusOk(restResponse);
        }

        /// <summary>
        /// Start a Premium Trial period for the user.
        /// </summary>
        /// <returns></returns>
        public async Task PostMyAccountPremiumTrial()
        {
            Uri resource = BaseUrl.PathCombine("users/my/account/premiumtrial");

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("POST", resource, Timeout));
            ApiCaller.EnsureStatusOk(restResponse);
        }

        /// <summary>
        /// Changes the password of the user.
        /// </summary>
        /// <param name="newPassword">The new password.</param>
        /// <returns></returns>
        public async Task PutMyPasswordAsync(string newPassword)
        {
            Uri resource = BaseUrl.PathCombine("users/my/account/password");

            PasswordResetParameters passwordResetParameters = new PasswordResetParameters(newPassword, string.Empty);
            RestContent content = new RestContent(Serializer.Serialize(passwordResetParameters));
            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("PUT", resource, Timeout, content)).Free();
            ApiCaller.EnsureStatusOk(restResponse);
        }

        /// <summary>
        /// Gets the public key of any user. If the user does not exist, he or she is invited by the current user.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <returns></returns>
        public async Task<AccountKey> GetAllAccountsOtherUserPublicKeyAsync(string userName)
        {
            if (userName == null)
            {
                throw new ArgumentNullException(nameof(userName));
            }

            Uri resource = BaseUrl.PathCombine("users/all/accounts/{0}/key".With(ApiCaller.PathSegmentEncode(userName)));

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest(resource, Timeout)).Free();
            ApiCaller.EnsureStatusOk(restResponse);

            AccountKey accountKey = Serializer.Deserialize<AccountKey>(restResponse.Content);
            return accountKey;
        }

        /// <summary>
        /// Gets the public key of any user. If the user does not exist, he or she is invited by the current user.
        /// The invitation is customized with the selected language/culture and personalized message.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <param name="customParameters">The message custom parameters.</param>
        /// <returns></returns>
        public async Task<AccountKey> PostAllAccountsOtherUserInvitePublicKeyAsync(string userName, CustomMessageParameters customParameters)
        {
            if (userName == null)
            {
                throw new ArgumentNullException(nameof(userName));
            }

            Uri resource = BaseUrl.PathCombine("users/all/accounts/invite/{0}/key".With(ApiCaller.PathSegmentEncode(userName)));

            RestContent content = new RestContent(Serializer.Serialize(customParameters));

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("POST", resource, Timeout, content)).Free();
            ApiCaller.EnsureStatusOk(restResponse);

            AccountKey accountKey = Serializer.Deserialize<AccountKey>(restResponse.Content);
            return accountKey;
        }

        /// <summary>
        /// Gets the public key of any user. If the user does not exist, he or she is invited by the current user.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <returns></returns>
        public async Task<AccountKey> GetPublicApiKeyAllAccountsOtherUserPublicKeyAsync(string userName)
        {
            if (userName == null)
            {
                throw new ArgumentNullException(nameof(userName));
            }

            Uri resource = BaseUrl.PathCombine("public/all/accounts/{0}/key".With(ApiCaller.PathSegmentEncode(userName)));

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest(resource, Timeout)).Free();
            ApiCaller.EnsureStatusOk(restResponse);

            AccountKey accountKey = Serializer.Deserialize<AccountKey>(restResponse.Content);
            return accountKey;
        }

        public async Task PostAllAccountsUserAsync(string userName, CultureInfo culture, string signUpFrom, string? utm = null, string? platForm = null)
        {
            if (userName == null)
            {
                throw new ArgumentNullException(nameof(userName));
            }
            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }

            Uri resource = BaseUrl.PathCombine("usersv2/all/accounts/{0}?culture={1}&utm={2}&signUpFrom={3}&platform={4}".With(ApiCaller.EncodePathParams(userName), culture.Name, utm ?? "", signUpFrom, platForm));

            RestResponse restResponse = await Caller.RestAsync(new RestIdentity(), new RestRequest("POST", resource, Timeout)).Free();
            ApiCaller.EnsureStatusOk(restResponse);
        }

        public async Task<bool> GetAllAccountsUserVerify(string verification)
        {
            if (string.IsNullOrEmpty(Identity.User))
            {
                throw new InvalidOperationException("There must be an identity to attempt to verify.");
            }

            Uri resource = BaseUrl.PathCombine("users/all/accounts/{0}/verify?verification={1}".With(Identity.User, verification));
            RestResponse restResponse = await Caller.RestAsync(new RestIdentity(), new RestRequest("GET", resource, Timeout)).Free();

            if (restResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
            ApiCaller.EnsureStatusOk(restResponse);
            return true;
        }

        public async Task PutAllAccountsUserPasswordAsync(string verification)
        {
            if (string.IsNullOrEmpty(Identity.User) || string.IsNullOrEmpty(Identity.Password))
            {
                throw new InvalidOperationException("There must be an identity and password to attempt to verify the account information.");
            }

            Uri resource = BaseUrl.PathCombine("users/all/accounts/{0}/password".With(Identity.User));

            PasswordResetParameters passwordResetParameters = new PasswordResetParameters(Identity.Password, verification);
            RestContent content = new RestContent(Serializer.Serialize(passwordResetParameters));
            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("PUT", resource, Timeout, content)).Free();
            ApiCaller.EnsureStatusOk(restResponse);
        }

        public async Task<bool> PostFeedbackAsync(string subject, string message)
        {
            if (subject == null)
            {
                throw new ArgumentNullException(nameof(subject));
            }
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            if (message.Length == 0)
            {
                throw new ArgumentException("Message must contain something.", nameof(message));
            }

            Uri resource = BaseUrl.PathCombine("users/my/account/feedback");

            FeedbackData feedbackData = new FeedbackData(subject, message);
            RestContent content = new RestContent(Serializer.Serialize(feedbackData));
            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("POST", resource, Timeout, content)).Free();
            ApiCaller.EnsureStatusOk(restResponse);
            if (restResponse.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            return false;
        }

        public async Task<AccountTip> GetAccountTipAsync(AppTypes appType)
        {
            if (New<AxCryptOnlineState>().IsOffline)
            {
                return new AccountTip();
            }

            if (Identity.IsEmpty)
            {
                return new AccountTip();
            }

            Uri resource = BaseUrl.PathCombine($"users/my/account/tip?apptype={(int)appType}");
            RestResponse response = await Caller.RestAsync(Identity, new RestRequest("GET", resource, Timeout));
            ApiCaller.EnsureStatusOk(response);

            AccountTip tip = Serializer.Deserialize<AccountTip>(response.Content);
            return tip;
        }

        public async Task<AxCryptVersion> AxCryptUpdateAsync(Version currentVersion, string cultureName, ClientPlatformKind platform, string appEnvironment)
        {
            string platformParameter = string.Empty;
            switch (platform)
            {
                case ClientPlatformKind.WindowsDesktop:
                    platformParameter = "windows";
                    break;

                case ClientPlatformKind.Mac:
                    platformParameter = "macos";
                    break;

                case ClientPlatformKind.Linux:
                    platformParameter = "linux";
                    break;

                default:
                    throw new NotSupportedException($"App doesn't support updating on {platform} platform");
            }

            Uri resource = BaseUrl.PathCombine($"users/axcrypt/{platformParameter}/appversion?version={currentVersion?.ToString() ?? string.Empty}&culture={cultureName}&env={appEnvironment}");
            if (New<AxCryptOnlineState>().IsOffline)
            {
                return AxCryptVersion.Empty;
            }

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest(resource, Timeout)).Free();
            ApiCaller.EnsureStatusOk(restResponse);
            AxCryptVersion axCryptVersion = Serializer.Deserialize<AxCryptVersion>(restResponse.Content);
            return axCryptVersion;
        }

        public async Task<bool> PostCreateSubscriptionAsync(StoreKitTransaction[] skTransactions)
        {
            if (skTransactions == null)
            {
                throw new ArgumentNullException(nameof(skTransactions));
            }

            Uri resource = BaseUrl.PathCombine("purchase/inapppurchase/create/subscription");
            RestContent content = new RestContent(Serializer.Serialize(skTransactions));
            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("POST", resource, Timeout, content)).Free();
            ApiCaller.EnsureStatusOk(restResponse);

            bool subscriptionCreatedSuccessfully = Serializer.Deserialize<bool>(restResponse.Content);
            return subscriptionCreatedSuccessfully;
        }

        public async Task<bool> PostSubscriptionByGooglePaymentAsync(GooglePurchaseInfo googlePaymentTrans)
        {
            if (googlePaymentTrans == null)
            {
                throw new ArgumentNullException(nameof(googlePaymentTrans));
            }

            Uri resource = BaseUrl.PathCombine("purchase/googlepayment/create/subscription");
            RestContent content = new RestContent(Serializer.Serialize(googlePaymentTrans));
            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("POST", resource, Timeout, content)).Free();
            ApiCaller.EnsureStatusOk(restResponse);

            bool subscriptionCreatedSuccessfully = Serializer.Deserialize<bool>(restResponse.Content);
            return subscriptionCreatedSuccessfully;
        }

        public async Task<PurchaseSettings> GetInAppPurchaSettingsAsync(string eventType)
        {
            if (string.IsNullOrEmpty(Identity.User) || string.IsNullOrEmpty(Identity.Password))
            {
                throw new InvalidOperationException("There must be an identity and password to attempt to get in app purchase information.");
            }

            Uri resource = BaseUrl.PathCombine($"purchase/inapppurchase/settings?eventtype={eventType}");

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest(resource, Timeout)).Free();
            ApiCaller.EnsureStatusOk(restResponse);

            PurchaseSettings inAppPurchaseMember = Serializer.Deserialize<PurchaseSettings>(restResponse.Content);
            return inAppPurchaseMember;
        }

        public async Task<bool> GetAutoRenewalStatusAsync()
        {
            if (string.IsNullOrEmpty(Identity.User) || string.IsNullOrEmpty(Identity.Password))
            {
                throw new InvalidOperationException("There must be an identity and password to attempt to get private account information.");
            }

            Uri resource = BaseUrl.PathCombine("purchase/account/autorenewal/status");

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest(resource, Timeout)).Free();
            ApiCaller.EnsureStatusOk(restResponse);

            bool isCancelSubscription = Serializer.Deserialize<bool>(restResponse.Content);
            return isCancelSubscription;
        }

        public async Task<bool> DeleteUserAsync()
        {
            if (string.IsNullOrEmpty(Identity.User) || string.IsNullOrEmpty(Identity.Password))
            {
                throw new InvalidOperationException("There must be an identity and password to attempt to get private account information.");
            }

            Uri resource = BaseUrl.PathCombine("users/my/account");

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("DELETE", resource, Timeout)).Free();
            ApiCaller.EnsureStatusOk(restResponse);

            bool userAccountDeleted = Serializer.Deserialize<bool>(restResponse.Content);
            return userAccountDeleted;
        }

        /// <summary>
        /// Gets the user membered group keys.
        /// </summary>
        /// <returns>All group keys for the user.</returns>
        /// <exception cref="System.InvalidOperationException">There must be an identity and password to attempt to get private account information.</exception>
        public async Task<IEnumerable<GroupKeyPairApiModel>> ListMembershipGroupKeyAsync()
        {
            if (string.IsNullOrEmpty(Identity.User) || string.IsNullOrEmpty(Identity.Password))
            {
                throw new InvalidOperationException("There must be an identity and password to attempt to get private account information.");
            }

            Uri resource = BaseUrl.PathCombine("groupinfo/memberships");

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest(resource, Timeout)).Free();
            ApiCaller.EnsureStatusOk(restResponse);

            IEnumerable<GroupKeyPairApiModel> userGroupKeys = Serializer.Deserialize<IEnumerable<GroupKeyPairApiModel>>(restResponse.Content);
            return userGroupKeys;
        }

        public async Task PostPrioritySupportAsync(string subject, string message)
        {
            if (subject == null)
            {
                throw new ArgumentNullException(nameof(subject));
            }
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            if (message.Length == 0)
            {
                throw new ArgumentException("Message must contain something.", nameof(message));
            }

            Uri resource = BaseUrl.PathCombine("users/my/account/support");

            FeedbackData prioritySupport = new FeedbackData(subject, message);
            RestContent content = new RestContent(Serializer.Serialize(prioritySupport));
            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("POST", resource, Timeout, content)).Free();
            ApiCaller.EnsureStatusOk(restResponse);
        }

        public async Task<MultiFactorAuthOTPApiModel> SendMFAOTPAsync(string userName)
        {
            if (userName == null)
            {
                throw new ArgumentNullException(nameof(userName));
            }
            Uri resource = BaseUrl.PathCombine($"users/mfauth/sendotp");

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("POST", resource, Timeout)).Free();
            ApiCaller.EnsureStatusOk(restResponse);

            MultiFactorAuthOTPApiModel authOTPApiModel = Serializer.Deserialize<MultiFactorAuthOTPApiModel>(restResponse.Content);
            return authOTPApiModel;
        }

        public async Task<bool> UpdateRememberMeOnMFAInfoAsync(MultiFactorAuthApiModel multiFactorAuthApi)
        {
            if (multiFactorAuthApi == null)
            {
                throw new ArgumentNullException(nameof(multiFactorAuthApi));
            }
            Uri resource = BaseUrl.PathCombine($"users/mfauth/saveremember");
            RestContent content = new RestContent(Serializer.Serialize(multiFactorAuthApi));

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("POST", resource, Timeout, content)).Free();
            ApiCaller.EnsureStatusOk(restResponse);

            bool updated = Serializer.Deserialize<bool>(restResponse.Content);
            return updated;
        }

        public async Task PostSetMyAccountViewerPlanAsync()
        {
            Uri resource = BaseUrl.PathCombine("users/setfreeuser/{0}".With(ApiCaller.EncodePathParams(Identity.User)));

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("POST", resource, Timeout));
            ApiCaller.EnsureStatusOk(restResponse);
        }

        public async Task<IEnumerable<PricingInfoApiModel>> GetPurchasePricingAsync(string signUpFrom, string discountCode, string ip, int members, string country)
        {
            Uri resource;
            if (string.IsNullOrEmpty(country))
            {
                resource = BaseUrl.PathCombine("inappsubs/pricing?signUpFrom={0}&discountCode={1}&ip={2}&members={3}".With(signUpFrom, discountCode, ip, members.ToString()));
            }
            else
            {
                resource = BaseUrl.PathCombine("inappsubs/pricing?signUpFrom={0}&discountCode={1}&ip={2}&members={3}&culture={4}".With(signUpFrom, discountCode, ip, members.ToString(), country));
            }

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("GET", resource, Timeout));
            ApiCaller.EnsureStatusOk(restResponse);

            return Serializer.Deserialize<IEnumerable<PricingInfoApiModel>>(restResponse.Content);
        }

        public async Task<Uri> GetCheckoutSessionUrlAsync(PurchaseInfoApiModel purchaseInfoApiModel)
        {
            Uri resource = BaseUrl.PathCombine("inappsubs/checkoutsession");
            RestContent content = new RestContent(Serializer.Serialize(purchaseInfoApiModel));

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("PUT", resource, Timeout, content));
            ApiCaller.EnsureStatusOk(restResponse);

            return Serializer.Deserialize<Uri>(restResponse.Content);
        }

        public async Task<string> GetPayPalCheckoutSessionUrlAsync(int subsMonths, string currency, string ip)
        {
            Uri resource = BaseUrl.PathCombine("inappsubs/paypal/checkoutsession?subsMonths={0}&currency={1}&ip={2}".With(subsMonths.ToString(), currency, ip));

            RestResponse restResponse = await Caller.RestAsync(Identity, new RestRequest("GET", resource, Timeout));
            ApiCaller.EnsureStatusOk(restResponse);

            return Serializer.Deserialize<string>(restResponse.Content);
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