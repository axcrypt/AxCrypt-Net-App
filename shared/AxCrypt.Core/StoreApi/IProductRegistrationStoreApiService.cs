using AxCrypt.Api.Model;
using AxCrypt.Api.Model.Migration;

namespace AxCrypt.Core.StoreApi
{
    public interface IProductRegistrationStoreApiService
    {
        Task<bool> CreateAsync(ProductRegistrationApiModel productRegistrationApiModel);

        Task<ProductRegistrationApiModel> GetAsync(string email, string productName);

        Task<IEnumerable<ProductRegistrationApiModel>> GetListForStatsAsync(ListFilterOptions listFilterOptions);

        Task<bool> CopyAsync(string fromUser, string toUser);

        Task<bool> MoveAsync(string fromUser, string toUser);
    }
}