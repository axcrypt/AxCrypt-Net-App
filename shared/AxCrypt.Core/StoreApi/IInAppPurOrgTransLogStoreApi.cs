using AxCrypt.Api.Model.Migration;
using System;
using System.Collections.Generic;
using System.Text;

namespace AxCrypt.Core.StoreApi
{
    public interface IInAppPurOrgTransLogStoreApi
    {
        Task<bool> CreateAsync(InAppPurOrgTransLogApiModel appPurOrgTransLogApiModel);

        Task<bool> UpdateAsync(InAppPurOrgTransLogApiModel appPurOrgTransLogApiModel);

        //Task<InAppPurOrgTransLogApiModel> GetAsync();

        Task<IEnumerable<InAppPurOrgTransLogApiModel>> GetListAsync();
    }
}