using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AxCrypt.App.Shared.CloudCore.DropBox;

public class DropboxHandler : HttpClientHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri.AbsoluteUri.Contains("files/download"))
        {
            request.Content = new StringContent(string.Empty);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        }

        if (request.RequestUri.AbsoluteUri.Contains("files/upload"))
        {
            if (request.Content != null)
            {
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}