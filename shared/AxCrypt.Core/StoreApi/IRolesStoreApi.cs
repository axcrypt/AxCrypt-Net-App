using AxCrypt.Api.Model.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace AxCrypt.Core.StoreApi
{
    public interface IRolesStoreApi
    {
        Task<IEnumerable<RoleApiModel>> GetRolesByEmailAsync(string email);

        Task<RoleApiModel> GetRolesAsync(string email);

        Task<bool> IsSiteAdminAsync(string email);
    }
}