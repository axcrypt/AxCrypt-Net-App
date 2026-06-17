using AxCrypt.Api;
using AxCrypt.Api.Model.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Core.StoreApi
{
    public interface IUserActivityStoreApiService
    {
        Task<bool> SaveAsync(UserActivityApiModel userSignUpActivityApiModel);

        Task<IEnumerable<UserActivityApiModel>> GetListAsync(RequestOptions requestOptions);
    }
}
