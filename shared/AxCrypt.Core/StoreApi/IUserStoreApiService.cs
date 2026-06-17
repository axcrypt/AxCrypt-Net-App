using AxCrypt.Api.Model.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Core.StoreApi
{
    public interface IUserStoreApiService
    {
        Task<UserApiModel> GetUserAsync(string userEmail);

        //Task<bool> CreateUserAsync(UserApiModel user);

        Task<bool> UpdateUserAsync(UserApiModel user);

        Task<bool> DeleteAsync(string userEmail);

        Task<bool> UpdateArchiveUserAsync(string fromUser, string toUser);

        //Task<bool> UpdateIsApprovedAsync(UserApiModel userApiModel);


        Task<UserApiModel> CreateAsync(UserApiModel userApiModel);

        //Task<bool> UpdateAsync(string userEmail, UserApiModel secret);

        Task<UserApiModel> GetUserDetailsAsync(string userEmail);

        // Task<bool> DeleteAsync(string userEmail);

        //Task<bool> UpdateArchiveUserAsync(string oldUserEmail, string newUserEmail);

        //Task<bool> MigrateUserAsync(UserApiModel userApiModel);

        //Task<UserApiModel> GetUserAsync(string userEmail);

        Task<bool> UpdateUserActivateIfPendingEmailChangeAsync(UserApiModel userApiModel);

        Task<UserKeyValuePairApiModel> GetUserActivityKeyPairsAsync(string userEmail, string memPropertyName);

        Task<bool> UpdateUserActivityAsync(UserKeyValuePairApiModel userKeyValuePairApiModel);

        Task<UserApiModel> GetCurrentUserAsync(string userEmail);
    }
}