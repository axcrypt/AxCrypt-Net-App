using AxCrypt.Api.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Core.StoreApi
{
    public interface IBusinessManualInvoiceStoreApiService
    {
        Task<IEnumerable<BusinessManualInvoiceApiModel>> GetAsync();

        Task<bool> SaveAsync(BusinessManualInvoiceApiModel manualInvoiceInfoApiModel);
    }
}