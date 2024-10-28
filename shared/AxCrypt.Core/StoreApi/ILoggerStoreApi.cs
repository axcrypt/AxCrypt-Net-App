using AxCrypt.Api.Model.Migration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AxCrypt.Core.StoreApi
{
    //internal class ILoggerStoreApi
    //{
    //}
    public interface ILoggerStoreApi
    {
        Task<bool> CreateAsync(LoggerApiModel loggerApiModel);

        Task<IEnumerable<LoggerApiModel>> GetListAsync();

        Task<LoggerApiModel> GetAsync(long id);

        Task<bool> CreateTableAsync();
    }
}