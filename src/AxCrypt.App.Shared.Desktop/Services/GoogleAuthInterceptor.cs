using AxCrypt.App.Shared.CloudCore.CloudFileProviderUtility;
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AxCrypt.App.Shared.Desktop.Services
{
    public static class GoogleAuthInterceptor
    {
        private static HttpListener? _httpListener;
        private static Task<HttpListenerContext>? _listenerTask;
        private static CancellationTokenSource? _cts;

        public static async Task ListenForOAuthRedirectAsync(string redirectUri)
        {
            await StopListeningAsync();

            _cts = new CancellationTokenSource();
            _httpListener = new HttpListener();

            try
            {
                _httpListener.Prefixes.Add(redirectUri);
                _httpListener.Start();

                _listenerTask = _httpListener.GetContextAsync();

                HttpListenerContext context = await WaitForContextWithCancellationAsync(_listenerTask, _cts.Token);

                if (context == null)
                {
                    return;
                }

                string oAuthCode = context.Request.QueryString["code"];

                await SendSuccessResponse(context.Response);

                if (oAuthCode != null)
                {
                    await CloudFileProviderHelper.CompleteOAuthSelection(oAuthCode);
                }

                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine($"HttpListener error: {ex.Message}");
                return;
            }
            finally
            {
                await StopListeningAsync();
            }
        }

        private static async Task<HttpListenerContext?> WaitForContextWithCancellationAsync(Task<HttpListenerContext> task,
            CancellationToken cancellationToken)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => tcs.TrySetResult(true)))
            {
                if (task != await Task.WhenAny(task, tcs.Task))
                {
                    return null;
                }
            }

            return await task;
        }

        public static async Task StopListeningAsync()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            if (_httpListener != null)
            {
                try
                {
                    if (_httpListener.IsListening)
                        _httpListener.Stop();

                    _httpListener.Close();
                }
                catch (ObjectDisposedException)
                {
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error stopping listener: {ex.Message}");
                }
                finally
                {
                    _httpListener = null;
                    _listenerTask = null;
                }
            }
        }

        private static async Task SendSuccessResponse(HttpListenerResponse response)
        {
            string responseString = "<html><body>Authentication successful. You can close this window.</body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);

            response.ContentType = "text/html";
            response.ContentLength64 = buffer.Length;

            try
            {
                await response.OutputStream.WriteAsync(buffer);
            }
            finally
            {
                response.OutputStream.Close();
            }
        }
    }
}