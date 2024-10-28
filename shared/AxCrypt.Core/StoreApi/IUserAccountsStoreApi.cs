using AxCrypt.Api.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Core.StoreApi
{
    public interface IUserAccountsStoreApi
    {
        Task<bool> CreateUserAccountsAsync(string email, UserAccounts userAccounts);

        Task<UserAccounts> GetUserAccountsAsync(string email);
    }
}
