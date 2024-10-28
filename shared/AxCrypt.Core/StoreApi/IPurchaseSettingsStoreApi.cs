using AxCrypt.Api.Model;
using AxCrypt.Api.Model.Migration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.Core.StoreApi
{
    public interface IPurchaseSettingsStoreApi
    {
        Task<bool> CreatePurchaseSettingsAsync(PurchaseSettings purchaseSettings);

        Task<IEnumerable<PurchaseSettings>> GetPurchaseSettingsListAsync();

    }
}
