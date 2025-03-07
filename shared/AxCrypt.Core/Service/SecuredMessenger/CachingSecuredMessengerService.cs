using AxCrypt.Abstractions;
using AxCrypt.Api;
using AxCrypt.Api.Model.SecuredMessenger;
using AxCrypt.Api.SecuredMessenger;
using AxCrypt.Common;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Service.SecuredMessenger;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.Service.Secrets
{
    public class CachingSecuredMessengerService : ISecuredMessengerService
    {
        private ISecuredMessengerService _service;

        private CacheKey _key;

        public CachingSecuredMessengerService(ISecuredMessengerService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            _service = service;
            _key = CacheKey.RootKey.Subkey(nameof(CachingSecuredMessengerService)).Subkey(service.Identity.UserEmail.Address).Subkey(service.Identity.Tag.ToString());
        }

        public ISecuredMessengerService Refresh()
        {
            New<ICache>().RemoveItem(_key);
            return this;
        }

        public LogOnIdentity Identity
        {
            get
            {
                return _service.Identity;
            }
        }

        public async Task<IEnumerable<SecuredMessengerApiModel>> GetListAsync(RequestOptions requestOptions)
        {
            return await New<ICache>().UpdateItemAsync(async () => await _service.GetListAsync(requestOptions)).Free();
        }

        public async Task<IEnumerable<SecuredMessengerApiModel>> GetSentListAsync(RequestOptions requestOptions)
        {
            return await New<ICache>().UpdateItemAsync(async () => await _service.GetSentListAsync(requestOptions), _key).Free();
        }

        public async Task<IEnumerable<SecuredMessengerApiModel>> GetUnreadListAsync(RequestOptions requestOptions)
        {
            return await New<ICache>().UpdateItemAsync(async () => await _service.GetUnreadListAsync(requestOptions)).Free();
        }

        public async Task<bool> CreateAsync(SecuredMessengerApiModel messengerApiModel)
        {
            return await New<ICache>().UpdateItemAsync(async () => await _service.CreateAsync(messengerApiModel), _key).Free();
        }

        public async Task<SecuredMessengerRootApiModel> GetAsync(Guid id, string userEmail)
        {
            return await New<ICache>().UpdateItemAsync(async () => await _service.GetAsync(id, userEmail), _key).Free();
        }

        public async Task<bool> UpdateAsync(IEnumerable<Guid> ids, string userEmail, bool isUnread)
        {
            return await New<ICache>().UpdateItemAsync(async () => await _service.UpdateAsync(ids, userEmail, isUnread), _key).Free();
        }

        public async Task<bool> DeleteAsync(IEnumerable<Guid> ids, string user, SecureMsgrFilterTab securedMessengerFilter)
        {
            return await New<ICache>().UpdateItemAsync(async () => await _service.DeleteAsync(ids, user, securedMessengerFilter), _key).Free();
        }

        public async Task<bool> SaveMessagelist(SecuredMessengerApiModel model)
        {
            return await New<ICache>().UpdateItemAsync(async () => await _service.SaveMessagelist(model), _key).Free();
        }

        public async Task<bool> SavemessagesAsync(SecuredMessengerRootApiModel model)
        {
            return await New<ICache>().UpdateItemAsync(async () => await _service.SavemessagesAsync(model), _key).Free();
        }

        public async Task<IEnumerable<SecuredMessengerRootApiModel>> GetSecMsgWithSearchFiltersAsync(SecureMsgrFilterTab securedMessengerFilterTab, RequestOptions requestOptions)
        {
            return await New<ICache>().UpdateItemAsync(async () => await _service.GetSecMsgWithSearchFiltersAsync(securedMessengerFilterTab, requestOptions), _key).Free();
        }
    }
}