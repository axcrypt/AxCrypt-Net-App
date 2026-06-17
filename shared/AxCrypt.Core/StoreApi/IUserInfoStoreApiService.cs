using AxCrypt.Api.Model.User;


namespace AxCrypt.Core.StoreApi
{
    public interface IUserInfoStoreApiService
    {
        Task<UserInfoApiModel> GetUserInfoAsync(string userEmail);

        Task<UserKeyValuePairApiModel> GetUserInfoByKeyAsync(string userEmail, string memPropertyName);

        Task<bool> UpdateUserInfoByKeyAsync(UserKeyValuePairApiModel userKeyValuePairApiModel);
    }
}
