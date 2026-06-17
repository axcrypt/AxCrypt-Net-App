namespace AxCrypt.Core.StoreApi
{
    public interface IPlayStoreInAppPurchaseSettingsStoreApi
    {
        Task<bool> SaveAsync(AxCrypt.Abstractions.Rest.RestContent restContent);

        Task<bool> UpdateAsync(AxCrypt.Abstractions.Rest.RestContent restContent);

        Task<bool> EmailChangeAsync(string newUser, string oldUser);

        Task<AxCrypt.Abstractions.Rest.RestResponse> GetListAsync();
    }
}