using AxCrypt.App.Shared.CloudCore.CloudFileProviderUtility;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.App.Shared.Desktop.Services
{
    public static class GoogleAuthInterceptor
    {
        private static HttpListener? _httpListener;

        public static async Task ListenForOAuthRedirectAsync(string redirectUri)
        {
            string? oAuthCode = null;

            try
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add(redirectUri);
                _httpListener.Start();

                HttpListenerContext context = await _httpListener.GetContextAsync();
                oAuthCode = context.Request.QueryString["code"]!;

                string responseString = @"
                <html>
                    <body>
                        Authentication successful. You can close this window.
                        <script>window.close();</script>
                    </body>
                </html>";

                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                context.Response.ContentLength64 = buffer.Length;
                await context.Response.OutputStream.WriteAsync(buffer);
                context.Response.OutputStream.Close();
            }
            finally
            {
                if (_httpListener != null && _httpListener.IsListening)
                {
                    _httpListener.Stop();
                    _httpListener.Close();
                }
            }
;

            if (oAuthCode != null)
            {
                await CloudFileProviderHelper.CompleteOAuthSelection(oAuthCode);
            }
        }
    }
}