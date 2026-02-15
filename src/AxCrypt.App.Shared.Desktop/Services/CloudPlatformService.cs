using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.ViewModels.Authentication;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Threading.Tasks;

namespace AxCrypt.App.Shared.Desktop.Services
{
    public class CloudPlatformService : ICloudPlatformService
    {
        public async Task InitializeCloudAuth(OAuth2Auth OAuth2Authenticator)
        {
            Task OAuthListenerTask = GoogleAuthInterceptor.ListenForOAuthRedirectAsync(OAuth2Authenticator!.RedirectUrl.ToString());
            Uri authUrl = new Uri(OAuth2Authenticator!.AuthUrl);
            await Browser.OpenAsync(OAuth2Authenticator!.AuthUrl, BrowserLaunchMode.External);
            await OAuthListenerTask;
        }
    }
}