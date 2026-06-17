using AxCrypt.Abstractions.Rest;
using AxCrypt.Api.Model.SyntheticWord;

namespace AxCrypt.Core.StoreApi
{
    public interface ISyntheticWordStoreApiService
    {
        Task<bool> Insert(SyntheticWordApiModel model);

        Task<SyntheticWordApiModel> GetAsync(string streamName);

        Task<bool> CopyAsync(string workCultureSpecificStreamname, string activeCultureSpecificStreamname);

        Task<bool> Delete(RestContent restContent);
    }
}