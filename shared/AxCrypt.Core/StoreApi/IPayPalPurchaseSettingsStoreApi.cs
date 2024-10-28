using AxCrypt.Api.Model.Migration;
using System;
using System.Collections.Generic;
using System.Text;

namespace AxCrypt.Core.StoreApi
{
    public interface IPayPalPurchaseSettingsStoreApi
    {
        Task<bool> CreatePayPalPurchaseAsync(PayPalPurchaseSettingsApiModel payPalPurchaseSettings);

        Task<IEnumerable<PayPalPurchaseSettingsApiModel>> GetPayPalPurchaseAsync();
    }
}