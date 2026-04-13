using AxCrypt.Abstractions;
using AxCrypt.App.Shared.Providers;
using AxCrypt.App.Shared.ViewModels.Authentication;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.CloudCore.iCloud
{
    internal class iCloudAuthenticator
    {
        public OAuth2Auth? Auth;

        public iCloudAccessInfo? CurrentAccessInfo;

        private const string iCloudAPIEndpoint = "";
        private const string iCloudAuthEndpoint = "";

        public iCloudAuthenticator()
        {
            InitializeAccessToken();
        }

        private void InitializeAccessToken()
        {
            IEnumerable<iCloudAccessInfo> iCloudAccessInfos = New<FileProvidersUserAccessInfo>().iCloudAccessInfo.ToList();

            if (!iCloudAccessInfos.Any())
            {
                //InitializeAuth();
                return;
            }

            iCloudAccessInfo? activeAccessToken = iCloudAccessInfos.FirstOrDefault(info => ValidAccessToken(info));

            if (activeAccessToken != null)
            {
                CurrentAccessInfo = activeAccessToken;
                return;
            }

            activeAccessToken = iCloudAccessInfos.LastOrDefault();
            if (activeAccessToken != null)
            {
                CurrentAccessInfo = activeAccessToken;
                if (RefreshiCloudAccessToken())
                {
                    return;
                }
            }
            CurrentAccessInfo = null;
            //InitializeAuth();
        }

        private void InitializeAuth()
        {
            string scope = Uri.EscapeDataString(string.Join(" ", iCloudConfiguration.iCloudAPIScopes));

            string authUrl = $"{iCloudConfiguration.AuthorizeUrl}" +
                $"?client_id={iCloudConfiguration.ClientId}" +
                $"&redirect_uri={iCloudConfiguration.RedirectUrl}" +
                $"&response_type=code" +
                $"&scope={scope}" +
                $"&response_mode=form_post";

            Auth = new OAuth2Auth(
                iCloudConfiguration.ClientId,
                string.Empty,
                scope,
                new Uri(iCloudConfiguration.AuthorizeUrl),
                new Uri(iCloudConfiguration.RedirectUrl),
                new Uri(iCloudConfiguration.AccessTokenUrl),
                authUrl,
                isUsingNativeUI: true
            );

            Auth.Authorized += async (sender, e) => await OnAuthenticationCompleted(sender, e);
        }

        private async Task OnAuthenticationCompleted(object? sender, string authCode)
        {
            if (string.IsNullOrEmpty(authCode))
            {
                return;
            }

            bool tokenGenerated = await GetAccessTokenAsync(authCode);
            if (tokenGenerated)
            {
                await Auth?.RaiseCompletedEventAsync()!;
            }
        }

        private async Task<bool> GetAccessTokenAsync(string authCode)
        {
            if (string.IsNullOrEmpty(authCode))
            {
                return false;
            }

            try
            {
                using HttpClient httpClient = new HttpClient();
                string tokenEndpoint = iCloudConfiguration.AccessTokenUrl;

                Dictionary<string, string> requestData = new Dictionary<string, string>
                {
                    { "code", authCode },
                    { "client_id", iCloudConfiguration.ClientId },
                    { "client_secret", iCloudConfiguration.ClientSecret },
                    { "redirect_uri", iCloudConfiguration.RedirectUrl },
                    { "grant_type", "authorization_code" }
                };

                using FormUrlEncodedContent requestContent = new FormUrlEncodedContent(requestData);
                using HttpResponseMessage response = await httpClient.PostAsync(tokenEndpoint, requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"iCloud auth error: {errorContent}");
                    return false;
                }

                string responseContent = await response.Content.ReadAsStringAsync();
                iCloudAccessInfo iCloudAccessInfo = Serializer.Deserialize<iCloudAccessInfo>(responseContent);

                CurrentAccessInfo = iCloudAccessInfo;
                StoreiCloudAccessToken(iCloudAccessInfo);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAccessTokenAsync error: {ex.Message}");
                return false;
            }
        }

        private bool ValidAccessToken(iCloudAccessInfo accessInfo)
        {
            DateTime expirationDateTime = accessInfo.CreatedDateTimeUtc.AddSeconds(accessInfo.ExpiresInSeconds);
            return expirationDateTime > New<INow>().Utc;
        }

        private bool RefreshiCloudAccessToken()
        {
            if (CurrentAccessInfo?.RefreshToken == null)
            {
                return false;
            }

            bool refreshed = false;
            Task refreshTokenTask = Task.Run(async () =>
            {
                try
                {
                    refreshed = await PerformTokenRefreshAsync(CurrentAccessInfo.RefreshToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Token refresh error: {ex.Message}");
                    refreshed = false;
                }
            });

            Task.WaitAll(refreshTokenTask);
            return refreshed;
        }

        private async Task<bool> PerformTokenRefreshAsync(string refreshToken)
        {
            try
            {
                using HttpClient httpClient = new HttpClient();
                string tokenEndpoint = iCloudConfiguration.AccessTokenUrl;

                Dictionary<string, string> requestData = new Dictionary<string, string>
                {
                    { "refresh_token", refreshToken },
                    { "client_id", iCloudConfiguration.ClientId },
                    { "client_secret", iCloudConfiguration.ClientSecret },
                    { "grant_type", "refresh_token" }
                };

                using FormUrlEncodedContent requestContent = new FormUrlEncodedContent(requestData);
                using HttpResponseMessage response = await httpClient.PostAsync(tokenEndpoint, requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                string responseContent = await response.Content.ReadAsStringAsync();
                iCloudAccessInfo refreshedAccessInfo = Serializer.Deserialize<iCloudAccessInfo>(responseContent);

                CurrentAccessInfo = refreshedAccessInfo;
                StoreiCloudAccessToken(refreshedAccessInfo);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PerformTokenRefreshAsync error: {ex.Message}");
                return false;
            }
        }

        private static void StoreiCloudAccessToken(iCloudAccessInfo accessInfo)
        {
            New<FileProvidersUserAccessInfo>().Add(accessInfo);
        }

        private static IStringSerializer Serializer
        {
            get => New<IStringSerializer>();
        }
    }
}