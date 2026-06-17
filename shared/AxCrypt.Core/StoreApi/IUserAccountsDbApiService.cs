using AxCrypt.Api.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Core.StoreApi
{
    public interface IUserAccountsDbApiService
    {
        Task<bool> CreateAsync(UserAccounts userAccount);

        Task<bool> UpdateAsync(UserAccount userAccount);

        Task<UserAccounts> GetListAsync(string userEmail);

        Task<UserAccounts> GetAsync(string userEmail);

        Task<bool> MigrateUserAccountsAsync(UserAccounts userAccount);

        Task<bool> CopyAsync(string fromUser, string toUser);

        Task<bool> MoveAsync(string fromUser, string toUser);
    }
}
