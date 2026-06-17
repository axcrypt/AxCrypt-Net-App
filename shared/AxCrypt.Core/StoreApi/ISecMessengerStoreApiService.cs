using AxCrypt.Api;
using AxCrypt.Api.Model.SecuredMessenger;
using AxCrypt.Core.UI;

namespace AxCrypt.Core.StoreApi
{
    public interface ISecMessengerStoreApiService
    {
        Task<IEnumerable<SecuredMessengerApiModel>> GetListAsync(RequestOptions requestOptions);

        Task<IEnumerable<SecuredMessengerApiModel>> GetSentListAsync(RequestOptions requestOptions);

        Task<IEnumerable<SecuredMessengerApiModel>> GetUnreadListAsync(RequestOptions requestOptions);

        Task<bool> CreateAsync(SecuredMessengerApiModel model);

        Task<bool> UpdateAsync(IEnumerable<Guid> ids, string userEmail, bool isUnread = false);

        Task<SecuredMessengerRootApiModel> GetAsync(Guid id, string userEmail);

        Task<bool> DeleteAsync(IEnumerable<Guid> ids, string user, SecuredMessengerFilterTab securedMessengerFilter);

        Task<IEnumerable<SecuredMessengerRootApiModel>> GetSecMsgWithSearchFiltersAsync(SecuredMessengerFilterTab securedMessengerFilterTab, RequestOptions requestOptions);

        Task<long> GetFreeUserSecMsgCountAsync(string userEmail);

        Task<bool> UpdateFreeUserSecMsgCountAsync(string userEmail);
    }
}