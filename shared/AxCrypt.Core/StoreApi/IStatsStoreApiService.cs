using AxCrypt.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AxCrypt.Core.StoreApi
{
    public interface IStatsStoreApiService
    {
        Task<AxCrypt.Abstractions.Rest.RestResponse> GetBusSubsStatsAsync(string email);

        Task<AxCrypt.Abstractions.Rest.RestResponse> GetBusinessMRRStatsAsync(string userEmail);

        Task<bool> RefreshStatsDataAsync(string userEmail);

        Task<AxCrypt.Abstractions.Rest.RestResponse> GetUserPerDayStatsAsync(RequestOptions requestOptions);

        Task<AxCrypt.Abstractions.Rest.RestResponse> GetCurrentUsersListWithFiltersAsync(RequestOptions requestOptions);

        Task<AxCrypt.Abstractions.Rest.RestResponse> GetCurrentUserListAsync(RequestOptions requestOptions);

        Task<bool> RefreshUserPerDayStatsAsync(string userEmail);

        Task<AxCrypt.Abstractions.Rest.RestResponse> GetProductRegistrationStatsAsync(string userEmail);

        Task<bool> RefreshProductRegistrationStatsAsync(string userEmail);

        Task<AxCrypt.Abstractions.Rest.RestResponse> GetDownloadsStatsAsync(RequestOptions requestOptions);

        Task<bool> RefreshDownloadsStatsAsync(string userEmail);

        Task<bool> RefreshCurrentUserListViewAsync(string userEmail);

        Task<AxCrypt.Abstractions.Rest.RestResponse> GetUserTotalSubscriptionMetricsAsync(string userEmail);
    }
}