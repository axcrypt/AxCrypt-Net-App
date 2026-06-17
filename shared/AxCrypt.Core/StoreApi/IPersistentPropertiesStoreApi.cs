using AxCrypt.Abstractions.Rest;
using AxCrypt.Api.Model.Migration;
using System;
using System.Collections.Generic;
using System.Text;

namespace AxCrypt.Core.StoreApi
{
    public interface IPersistentPropertiesStoreApi
    {
        Task<bool> CreatePersistentPropertiesAsync(AxCrypt.Abstractions.Rest.RestContent restContent);

        Task<AxCrypt.Abstractions.Rest.RestResponse> GetPersistentPropertiesAsync();
    }
}