using AxCrypt.Api.Model.Migration;
using System;
using System.Collections.Generic;
using System.Text;

namespace AxCrypt.Core.StoreApi
{
    public interface ISiteConfigurationStoreApi
    {
        Task<bool> CreateSiteConfigurationAsync(SiteConfigSettingsApiModel siteConfigurationSettings);

        Task<IEnumerable<SiteConfigSettingsApiModel>> GetSiteConfigurationAsync();
    }
}