using AxCrypt.Api.Model.MFA;

namespace AxCrypt.Core.StoreApi
{
    public interface IMultiFactorStoreApiService
    {
        Task<MultiFactorAuthApiModel> GetMultiFactorAuthStatusAsync(string userEmail);

        Task<bool> UpdateMultiFactorStatusAsync(MultiFactorAuthApiModel userApiModel);

        Task<bool> SaveMultiFactorAuthenticationInfoAsync(MultiFactorAuthApiModel multiFactorAuthApiModel);

        Task<MultiFactorAuthApiModel> GetMultiFactorAuthenticationInfoAsync(string userEmail);

        Task<bool> UpdateRememberMeOnMFAInfoAsync(MultiFactorAuthApiModel multiFactorAuthApi);
    }
}