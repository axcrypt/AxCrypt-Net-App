using AxCrypt.Abstractions;
using AxCrypt.Abstractions.Rest;
using AxCrypt.Api.Response;
using AxCrypt.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Api
{
    public class ApiCaller
    {
        public ApiCaller()
        {
        }

        public async Task<RestResponse> RestAsync(RestIdentity identity, RestRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            try
            {
                RestResponse response = await RestCaller.SendAsync(identity, request).Free();
                return response;
            }
            catch (OperationCanceledException ex)
            {
                throw new OfflineApiException(ApiCallMessage(request, ex), ex);
            }
            catch (TimeoutException ex)
            {
                throw new OfflineApiException(ApiCallMessage(request, ex), ex);
            }
            catch (Exception ex) when (!(ex is OfflineApiException))
            {
                throw new ApiException(ApiCallMessage(request, ex), ex);
            }
        }

        public static void EnsureStatusOk(RestResponse restResponse)
        {
            if (restResponse == null)
            {
                throw new ArgumentNullException(nameof(restResponse));
            }

            if (restResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedException(restResponse.Content, ErrorStatus.ApiHttpResponseError);
            }
            if (restResponse.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                throw new OfflineApiException("Service unavailable.");
            }
            if (restResponse.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new BadRequestApiException(string.IsNullOrEmpty(restResponse.Content) ? "Malformed API request." : restResponse.Content, restResponse.StatusCode);
            }
            if (restResponse.StatusCode != HttpStatusCode.OK && restResponse.StatusCode != HttpStatusCode.Created && restResponse.StatusCode != HttpStatusCode.NoContent)
            {
                throw new ApiException(restResponse.Content, restResponse.StatusCode);
            }
        }

        private static string ApiCallMessage(RestRequest request, Exception exception)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} failed: {2}", request.Method, request.Url, exception.Message);
        }

        public static void EnsureStatusOk(ResponseBase apiResponse)
        {
            if (apiResponse == null)
            {
                throw new ArgumentNullException(nameof(apiResponse));
            }

            if (apiResponse.Status != 0)
            {
                throw new ApiException(apiResponse.Message, ErrorStatus.ApiError);
            }
        }

        private static IRestCaller RestCaller
        {
            get
            {
                return New<IRestCaller>();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1055:UriReturnValuesShouldNotBeStrings")]
        public static string UrlEncode(string value)
        {
            return RestCaller.UrlEncode(value);
        }

        public static string PathSegmentEncode(string value)
        {
            return UrlEncode(value).Replace("%2B", "+").Replace("%40", "@");
        }

        public static string EncodePathParams(string pathParams)
        {
            return pathParams.Replace("+", "%2B");
        }
    }
}
