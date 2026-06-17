using AxCrypt.Api.Model.Migration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Core.StoreApi
{
    public interface INewsLetterStoreApiService
    {
        Task<bool> CreateAsync(NewsLetterApiModel model);
        Task<bool> DeleteAsync(string key);
        Task<NewsLetterApiModel> GetAsync(string id);
        Task<IEnumerable<NewsLetterApiModel>> ListAsync();
        Task<bool> UpdateAsync(NewsLetterApiModel newsmodel);
    }
}
