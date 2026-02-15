using AxCrypt.Abstractions;
using AxCrypt.App.Shared.Providers;
using AxCrypt.App.Shared.ViewModels.Authentication;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.CloudCore.DropBox
{
    public class DropBoxAuthenticator
    {
        public string? AccessToken { get; private set; }

        public OAuth2Auth? Auth;

        public DropBoxAuthenticator()
        {
            InitializeUserAccessToken();
        }

        private void InitializeUserAccessToken()
        {
            IEnumerable<DropBoxAccessInfo> dropBoxAccessInfos = New<FileProvidersUserAccessInfo>().DropBoxAccessInfo.ToList();
            if (!dropBoxAccessInfos.Any())
            {
                InitializeOAuth();
                return;
            }

            DropBoxAccessInfo activeAccessToken = dropBoxAccessInfos.SingleOrDefault(gdi => ValidAccessToken(gdi))!;
            if (activeAccessToken != null)
            {
                AccessToken = activeAccessToken.AccessToken;
                return;
            }

            InitializeOAuth();
        }

        private string? _codeVerifier;

        private string? _codeChallenge;

        private string GenerateCodeVerifier()
        {
            const int length = 32;
            byte[] bytes = new byte[length];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            return _codeVerifier = Base64UrlEncode(bytes);
        }

        private string GenerateCodeChallenge(string codeVerifier)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                return _codeChallenge = Base64UrlEncode(challengeBytes);
            }
        }

        private string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                          .Replace("+", "-")
                          .Replace("/", "_")
                          .Replace("=", "");
        }

        public async void InitializeOAuth()
        {
            string authUrl = $"{DropBoxConfiguration.AuthorizeUrl}" +
                                $"?client_id={DropBoxConfiguration.ClientIdOrAppKey}" +
                                $"&redirect_uri={DropBoxConfiguration.RedirectUrl}" +
                                $"&response_type=code" +
                                $"&code_challenge={GenerateCodeChallenge(GenerateCodeVerifier())}" +
                                $"&code_challenge_method=S256";

            Auth = new OAuth2Auth(
                   clientId: DropBoxConfiguration.ClientIdOrAppKey,
                   clientSecret: "",
                   scope: "",
                   authorizeUrl: new Uri(DropBoxConfiguration.AuthorizeUrl),
                   redirectUrl: new Uri(DropBoxConfiguration.RedirectUrl),
                   accessTokenUrl: new Uri(DropBoxConfiguration.AccessTokenUrl),
                   authUrl,
                   isUsingNativeUI: true);
            Auth.Authorized += async (sender, e) => await OnAuthenticationCompleted(sender, e);
        }

        private async Task OnAuthenticationCompleted(object sender, string authCode)
        {
            if (authCode == null)
            {
                return;
            }

            bool tokenGenerated = await GetRefreshTokenAsync(authCode);
            if (tokenGenerated)
            {
                await Auth.RaiseCompletedEventAsync();
            }
        }

        private async Task<bool> GetRefreshTokenAsync(string authCode)
        {
            if (authCode == null)
            {
                return false;
            }

            try
            {
                DropBoxAccessInfo dropBoxAccessInfo = await ExchangeCodeForAccessTokenAsync(authCode, _codeVerifier!);

                if (dropBoxAccessInfo != null)
                {
                    AccessToken = dropBoxAccessInfo.AccessToken;
                    StoreUserAccessToken(dropBoxAccessInfo);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return false;
        }

        private async Task<DropBoxAccessInfo> ExchangeCodeForAccessTokenAsync(string authorizationCode, string codeVerifier)
        {
            HttpClient client = new HttpClient();

            string tokenUrl = DropBoxConfiguration.AccessTokenUrl;
            string clientId = DropBoxConfiguration.ClientIdOrAppKey;
            string redirectUri = DropBoxConfiguration.RedirectUrl;

            Dictionary<string, string> requestBody = new Dictionary<string, string>
            {
                { "code", authorizationCode },
                { "code_verifier", codeVerifier },
                { "grant_type", "authorization_code" },
                { "redirect_uri", redirectUri },
                { "client_id", clientId }
            };

            FormUrlEncodedContent content = new FormUrlEncodedContent(requestBody);

            HttpResponseMessage response = await client.PostAsync(tokenUrl, content);
            response.EnsureSuccessStatusCode();

            string responseContent = await response.Content.ReadAsStringAsync();
            DropBoxAccessInfo tokenResponse = Serializer.Deserialize<DropBoxAccessInfo>(responseContent);

            return tokenResponse;
        }

        private static IStringSerializer Serializer
        {
            get
            {
                return New<IStringSerializer>();
            }
        }

        private void OnAuthenticationError(object sender, AuthenticationResult e)
        {
            //Show popup with error message
            return;
        }

        private void OnAuthenticationCompleted(object sender, AuthenticationResult e)
        {
            if (!e.ClaimsPrincipal.Identity!.IsAuthenticated)
            {
                //Show popup with "failed to login" error message
                return;
            }

            DropBoxAccessInfo dropBoxAccessInfo = new DropBoxAccessInfo(e.AccessToken, Convert.ToInt64(e.ExpiresOn), "", e.Scopes.First(), e.TokenType, e.AuthenticationResultMetadata.TokenSource.ToString(), e.Account.HomeAccountId.ObjectId, e.Account.HomeAccountId.TenantId);
            //InitializeUserCredential(dropBoxAccessInfo);
            AccessToken = dropBoxAccessInfo.AccessToken;
            StoreUserAccessToken(dropBoxAccessInfo);
        }

        private bool ValidAccessToken(DropBoxAccessInfo dropBoxAccessInfo)
        {
            DateTime expirationDateTime = dropBoxAccessInfo.CreatedDateTimeUtc.AddSeconds(dropBoxAccessInfo.ExpiresInSeconds);
            return expirationDateTime > New<INow>().Utc;
        }

        private bool RefreshUserAccessToken()
        {
            return false;
        }

        public void RemoveExpiredDropBoxToken()
        {
            DropBoxAccessInfo dropBoxAccessInfo = New<FileProvidersUserAccessInfo>().DropBoxAccessInfo.SingleOrDefault(dbi => dbi.AccessToken == AccessToken)!;
            if (dropBoxAccessInfo == null)
            {
                return;
            }

            New<FileProvidersUserAccessInfo>().Remove(dropBoxAccessInfo);
        }

        private static void StoreUserAccessToken(DropBoxAccessInfo dropBoxAccessInfo)
        {
            New<FileProvidersUserAccessInfo>().Add(dropBoxAccessInfo);
        }
    }
}
