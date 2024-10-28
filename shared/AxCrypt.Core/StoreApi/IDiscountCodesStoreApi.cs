using AxCrypt.Api.Model.Migration;
using System;
using System.Collections.Generic;
using System.Text;

namespace AxCrypt.Core.StoreApi
{
    public interface IDiscountCodesStoreApi
    {
        Task<bool> CreateAsync(DiscountCodesApiModel discountCodes);

        Task<IEnumerable<DiscountCodesApiModel>> GetListAsync();
    }
}