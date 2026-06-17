using AxCrypt.Api.Model.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Core.StoreApi
{
    public interface IUserAccountActivityStoreApiService
    {
        Task<bool> UpdateUserActivityByKeyAsync(UserKeyValuePairApiModel userKeyValuePairApiModel);

        Task<UserApiModel> GetInfoForPreparePwdResetAsync(string userEmail);

        Task<bool> UpdatePwdRstRequestedStatusAsync(UserApiModel userApiModel);

        Task<bool> UpdateUserAfterPwdChangeAsync(UserApiModel userApiModel);

        Task<UserApiModel> GetUserInfoForPwdResetAsync(string userName);

        Task<bool> UpdatePasswordResetAsync(UserApiModel userApiModel);

        Task<UserApiModel> GetUserInfoForValidateUserAsync(string userName);

        Task<UserApiModel> GetUserForEmailParametersAsync(string userName);

        Task<bool> UpdateValidatedUserActivityAsync(UserApiModel userApiModel);
    }
}
