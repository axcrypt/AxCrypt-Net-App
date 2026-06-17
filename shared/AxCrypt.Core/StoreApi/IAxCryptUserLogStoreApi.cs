using AxCrypt.Abstractions.Rest;
using AxCrypt.Api.Model.Migration;
using System;
using System.Collections.Generic;
using System.Text;

namespace AxCrypt.Core.StoreApi
{
    public interface IAxCryptUserLogStoreApi
    {
        Task<bool> CreateKeyLookupLogAsync(KeyLookupLogApiModel keyLookUpLog);

        Task<bool> CreateSignInLogAsync(SignInLogApiModel signInLog);

        Task<bool> CreateDownloadLogAsync(DownloadLogApiModel downloadLog);

        Task<bool> CreateSubsEventLogAsync(SubsEventLogApiModel subsEvent);

        Task<bool> CreatePaymentTransactionDailyLogAsync(RestContent restContent);

        Task<RestResponse> GetPaymentTransactionLogAsync(AxCrypt.Abstractions.Rest.RestContent restContent);

        Task<RestResponse> GetCurrentUserListAsync();
    }
}