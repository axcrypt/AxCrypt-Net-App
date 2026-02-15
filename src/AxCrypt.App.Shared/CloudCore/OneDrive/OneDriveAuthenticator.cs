using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AxCrypt.Abstractions;
using AxCrypt.App.Shared.Providers;
using AxCrypt.App.Shared.ViewModels.Authentication;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.CloudCore.OneDrive
{
    public class OneDriveAuthenticator
    {
        public string? AccessToken { get; private set; }
        public DateTimeOffset AccessTokenExpireOffset { get; private set; }

        public OAuth2Auth? Auth;

        public OneDriveAuthenticator()
        {
            InitializeUserAccessToken();
        }

        private void InitializeUserAccessToken()
        {
            IEnumerable<OneDriveAccessInfo> OneDriveAccessInfos = New<FileProvidersUserAccessInfo>().OneDriveAccessInfo.ToList();
            if (!OneDriveAccessInfos.Any())
            {
                InitializeOAuth();
                return;
            }

            OneDriveAccessInfo activeAccessToken = OneDriveAccessInfos.SingleOrDefault(gdi => ValidAccessToken(gdi))!;
            if (activeAccessToken != null)
            {
                AccessToken = activeAccessToken.AccessToken;
                AccessTokenExpireOffset = activeAccessToken.CreatedDateTimeUtc.AddSeconds(activeAccessToken.ExpiresInSeconds);
                return;
            }

            InitializeOAuth();
        }

        private async void InitializeOAuth()
        {
            string scope = Uri.EscapeDataString(string.Join(" ", OneDriveConfiguration.SCOPES));
            string authUrl = $"{OneDriveConfiguration.AUTHORIZE_URL}" +
                               $"?client_id={OneDriveConfiguration.CLIENT_ID}" +
                               $"&redirect_uri={OneDriveConfiguration.RedirectUrl}" +
                               $"&response_type=code" +
                               $"&scope={scope}";

            Auth = new OAuth2Auth(
                clientId: OneDriveConfiguration.CLIENT_ID,
                clientSecret: "",
                scope: scope,
                authorizeUrl: new Uri(OneDriveConfiguration.AUTHORIZE_URL),
                redirectUrl: new Uri(OneDriveConfiguration.RedirectUrl),
                accessTokenUrl: new Uri(OneDriveConfiguration.ACCESSTOKEN_URL),
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
                HttpClient httpClient = new HttpClient();
                string tokenEndpoint = OneDriveConfiguration.ACCESSTOKEN_URL;

                Dictionary<string, string> requestData = new Dictionary<string, string>
                {
                    { "code", authCode },
                    { "client_id", $"{OneDriveConfiguration.CLIENT_ID}" },
                    { "client_secret", "" },
                    { "redirect_uri", $"{OneDriveConfiguration.RedirectUrl}" },
                    { "grant_type", "authorization_code" }
                };

                FormUrlEncodedContent requestContent = new FormUrlEncodedContent(requestData);
                HttpResponseMessage response = await httpClient.PostAsync(tokenEndpoint, requestContent);
                response.EnsureSuccessStatusCode();

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    OneDriveAccessInfo oneDriveAccessInfo = Serializer.Deserialize<OneDriveAccessInfo>(responseContent);
                    AccessToken = oneDriveAccessInfo.AccessToken;
                    StoreUserAccessToken(oneDriveAccessInfo);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return false;
        }


        private static IStringSerializer Serializer
        {
            get
            {
                return New<IStringSerializer>();
            }
        }

        private bool ValidAccessToken(OneDriveAccessInfo oneDriveAccessInfo)
        {
            DateTime expirationDateTime = oneDriveAccessInfo.CreatedDateTimeUtc.AddSeconds(oneDriveAccessInfo.ExpiresInSeconds);
            return expirationDateTime > New<Abstractions.INow>().Utc;
        }

        private bool RefreshUserAccessToken()
        {
            return false;
        }

        public void RemoveExpiredOneDriveToken()
        {
            OneDriveAccessInfo oneDriveAccessInfo = New<FileProvidersUserAccessInfo>().OneDriveAccessInfo.SingleOrDefault(dbi => dbi.AccessToken == AccessToken)!;
            if (oneDriveAccessInfo == null)
            {
                return;
            }

            New<FileProvidersUserAccessInfo>().Remove(oneDriveAccessInfo);
        }

        private static void StoreUserAccessToken(OneDriveAccessInfo oneDriveAccessInfo)
        {
            New<FileProvidersUserAccessInfo>().Add(oneDriveAccessInfo);
        }
    }
}
