using AxCrypt.Api.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Core.StoreApi
{
    public interface INIS2StatsStoreApiService
    {
        Task<IEnumerable<NIS2ApiModel>> GetNIS2ListAsync(ListFilterOptions listFilterOpt);
    }
}
