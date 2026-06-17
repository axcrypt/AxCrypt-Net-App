using AxCrypt.Api.Model.Migration;

namespace AxCrypt.Core.StoreApi
{
    public interface IWebSettingsStoreApiSerrvice
    {
        Task<bool> CreateAsync(WebUserSettingsApiModel webUserSettingsApiModel);

        Task<WebUserSettingsApiModel> GetAsync(string userEmail);

        Task<bool> DeleteAsync(string userEmail);
    }
}