using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AxCrypt.App.Shared.ViewModels.Authentication
{
    public class OAuth2Auth(
        string clientId,
        string clientSecret,
        string scope,
        Uri authorizeUrl,
        Uri redirectUrl,
        Uri accessTokenUrl,
        string authUrl,
        bool isUsingNativeUI = false
    )
    {
        public string ClientId { get; set; } = clientId;

        public string Scope { get; set; } = scope;

        public string ClientSecret { get; set; } = clientSecret;

        public Uri AuthorizeUrl { get; set; } = authorizeUrl;

        public Uri RedirectUrl { get; set; } = redirectUrl;

        public Uri AccessTokenUrl { get; set; } = accessTokenUrl;

        public bool IsUsingNativeUI { get; set; } = isUsingNativeUI;

        public string AuthUrl { get; set; } = authUrl;

        public event EventHandler<string> Authorized;

        public event EventHandler Completed;

        public async Task RaiseAuthorizedEventAsync(string arg)
        {
            EventHandler<string> handlers = Authorized;
            if (handlers != null)
            {
                foreach (Delegate handler in handlers.GetInvocationList())
                {
                    object? result = handler.DynamicInvoke(this, arg);
                    if (result is Task task)
                    {
                        await task;
                    }
                }
            }
        }

        public async Task RaiseCompletedEventAsync()
        {
            EventHandler handlers = Completed;
            if (handlers != null)
            {
                foreach (Delegate handler in handlers.GetInvocationList())
                {
                    object? result = handler.DynamicInvoke(this, EventArgs.Empty);
                    if (result is Task task)
                    {
                        await task;
                    }
                }
            }
        }
    }
}
