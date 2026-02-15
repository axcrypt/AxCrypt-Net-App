using AxCrypt.Abstractions;
using AxCrypt.App.Shared.Providers;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.ViewModels.Authentication;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.CloudCore.GoogleDrive
{
    internal class GoogleDriveAuthenticator
    {
        public OAuth2Auth? Auth;

        public UserCredential? UserCredential;

        public GoogleDriveAuthenticator()
        {
            InitializeUserAccessToken();
        }

        private void InitializeUserAccessToken()
        {
            IEnumerable<GoogleDriveAccessInfo> googleDriveAccessInfos = New<FileProvidersUserAccessInfo>().GoogleDriveAccessInfo.ToList();
            if (!googleDriveAccessInfos.Any())
            {
                InitializeAuth();
                return;
            }

            GoogleDriveAccessInfo activeAccessToken = googleDriveAccessInfos.SingleOrDefault(gdi => ValidAccessToken(gdi))!;
            if (activeAccessToken != null)
            {
                InitializeUserCredential(activeAccessToken);
                return;
            }

            activeAccessToken = googleDriveAccessInfos.LastOrDefault()!;
            InitializeUserCredential(activeAccessToken);

            if (RefreshUserAccessToken())
            {
                return;
            }

            UserCredential = null;

            InitializeAuth();
        }

        private void InitializeAuth()
        {
            string scope = Uri.EscapeDataString(string.Join(" ", GoogleDriveConfiguration.GoogleAPIScopes));

            string authUrl = $"{GoogleDriveConfiguration.AuthorizeUrl}" +
                                $"?client_id={GoogleDriveConfiguration.ClientId}" +
                                $"&redirect_uri={GoogleDriveConfiguration.RedirectUrl}" +
                                $"&response_type=code" +
                                $"&scope={scope}" +
                                $"&access_type=offline" +
                                $"&prompt=consent";

            Auth = new OAuth2Auth(GoogleDriveConfiguration.ClientId,
                string.Empty,
                scope,
                new Uri(GoogleDriveConfiguration.AuthorizeUrl),
                new Uri(GoogleDriveConfiguration.RedirectUrl),
                new Uri(GoogleDriveConfiguration.AccessTokenUrl),
                authUrl,
                isUsingNativeUI: true);
            Auth.Authorized += async (sender, e)=> await OnAuthenticationCompleted(sender, e);
        }

        private async Task OnAuthenticationCompleted(object? sender, string authCode)
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
                string tokenEndpoint = GoogleDriveConfiguration.AccessTokenUrl;

                Dictionary<string, string> requestData = new Dictionary<string, string>
                {
                    { "code", authCode },
                    { "client_id", GoogleDriveConfiguration.ClientId },
                    { "client_secret", GoogleDriveConfiguration.ClientSecret },
                    { "redirect_uri", GoogleDriveConfiguration.RedirectUrl },
                    { "grant_type", "authorization_code" }
                };

                FormUrlEncodedContent requestContent = new FormUrlEncodedContent(requestData);

                HttpResponseMessage response = await httpClient.PostAsync(tokenEndpoint, requestContent);
                string responseContent1 = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    GoogleDriveAccessInfo googleDeviceInfo = Serializer.Deserialize<GoogleDriveAccessInfo>(responseContent);
                    InitializeUserCredential(googleDeviceInfo);
                    StoreUserAccessToken(googleDeviceInfo);
                    return true;
                }
                else
                {
                    // Handle error
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error: {errorContent}");
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

        private bool ValidAccessToken(GoogleDriveAccessInfo googleDriveAccessInfo)
        {
            DateTime expirationDateTime = googleDriveAccessInfo.CreatedDateTimeUtc.AddSeconds(googleDriveAccessInfo.ExpiresInSeconds);
            return expirationDateTime > New<INow>().Utc;
        }

        public void InitializeUserCredential(GoogleDriveAccessInfo googleDriveAccessInfo)
        {
            if (googleDriveAccessInfo == null)
            {
                return;
            }

            GoogleAuthorizationCodeFlow.Initializer gAuthCodeFlowInitializer = new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets()
                {
                    ClientId = GoogleDriveConfiguration.ClientId,
                    ClientSecret = GoogleDriveConfiguration.ClientSecret
                },
                Scopes = GoogleDriveConfiguration.GoogleAPIScopes,
                //DataStore = new FileDataStore("Google.Apis.Auth"),
            };

            GoogleAuthorizationCodeFlow googleAuthCodeFlow = new GoogleAuthorizationCodeFlow(gAuthCodeFlowInitializer);
            string user = "AxCrypt User";

            TokenResponse token = new TokenResponse()
            {
                AccessToken = googleDriveAccessInfo.AccessToken,
                ExpiresInSeconds = googleDriveAccessInfo.ExpiresInSeconds,
                RefreshToken = googleDriveAccessInfo.RefreshToken,
                Scope = googleDriveAccessInfo.Scope,
                TokenType = googleDriveAccessInfo.TokenType
            };

            UserCredential = new UserCredential(googleAuthCodeFlow, user, token);
        }

        private bool RefreshUserAccessToken()
        {
            bool refreshed = false;
            Task refreshTokenTask = Task.Run(async () =>
            {
                try
                {
                    refreshed = await UserCredential.RefreshTokenAsync(System.Threading.CancellationToken.None);
                }
                catch
                {
                    refreshed = false;
                }
            });

            Task.WaitAll(refreshTokenTask);

            if (refreshed)
            {
                GoogleDriveAccessInfo googleDriveAccessInfo = new GoogleDriveAccessInfo(UserCredential.Token.AccessToken, Convert.ToInt64(UserCredential.Token.ExpiresInSeconds ?? 0),
                    UserCredential.Token.RefreshToken, UserCredential.Token.Scope, UserCredential.Token.TokenType);
                StoreUserAccessToken(googleDriveAccessInfo);
            }

            return refreshed;
        }

        private static void StoreUserAccessToken(GoogleDriveAccessInfo googleDriveAccessInfo)
        {
            New<FileProvidersUserAccessInfo>().Add(googleDriveAccessInfo);
        }

        //public async Task<string> GetEmailAsync(string tokenType, string accessToken)
        //{
        //    var httpClient = new HttpClient();
        //    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(tokenType, accessToken);
        //    //var json = await httpClient.GetStringAsync("https://www.googleapis.com/userinfo/email?alt=json");
        //    //var json = await httpClient.GetStringAsync("https://www.googleapis.com/oauth2/v1/userinfo?alt=json&access_token='" + accessToken +"'");
        //    var json = await httpClient.GetStringAsync("https://www.googleapis.com/oauth2/v3/userinfo");
        //    var email = JsonConvert.DeserializeObject<GoogleEmail>(json);
        //    return email.Email;
        //}
    }
}
