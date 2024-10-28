using AxCrypt.Api.Model.Migration;
using System;
using System.Collections.Generic;
using System.Text;

namespace AxCrypt.Core.StoreApi
{
    public interface IPersistentPropertiesStoreApi
    {
        Task<bool> CreatePersistentPropertiesAsync(PersistentPropertiesApiModel persistentProperties);

        Task<PersistentPropertiesApiModel> GetPersistentPropertiesAsync();
    }
}