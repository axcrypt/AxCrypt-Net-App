using AxCrypt.Api;
using AxCrypt.Api.Model.SecuredMessenger;
using AxCrypt.Api.SecuredMessenger;
using AxCrypt.Common;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Extensions;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.Service.SecuredMessenger
{
    /// <summary>
    /// Implement secrets service functionality for a device, using a local and a remote service instance. This
    /// class determines the interaction and behavior when the services cooperate to provide a robust service
    /// despite possible remote outages.
    /// An instance operates on behalf an identity, or an anonymous one.
    /// </summary>
    public class DeviceSecuredMessengerService : ISecuredMessengerService
    {
        private ISecuredMessengerService _localService;

        private ISecuredMessengerService _remoteService;

        public DeviceSecuredMessengerService(ISecuredMessengerService localService, ISecuredMessengerService remoteService)
        {
            _localService = localService;
            _remoteService = remoteService;
        }

        public ISecuredMessengerService Refresh()
        {
            return this;
        }

        public LogOnIdentity Identity
        {
            get
            {
                return _remoteService.Identity;
            }
        }

        public async Task<IEnumerable<SecuredMessengerApiModel>> GetListAsync(RequestOptions requestOptions)
        {
            IEnumerable<SecuredMessengerApiModel> localUserMessages = await _localService.GetListAsync(requestOptions).Free();
            if (!New<AxCryptOnlineState>().IsOnline || Identity == LogOnIdentity.Empty)
            {
                return localUserMessages;
            }

            try
            {
                IEnumerable<SecuredMessengerApiModel> remoteMessages = await _remoteService.GetListAsync(requestOptions).Free();
                if (remoteMessages == null)
                {
                    return localUserMessages;
                }

                if (localUserMessages != remoteMessages)
                {
                    await _localService.SaveMessagelist(remoteMessages.LastOrDefault()).Free();
                }

                return remoteMessages;
            }
            catch (ApiException aex)
            {
                await aex.HandleApiExceptionAsync();
            }

            return localUserMessages;
        }

        public async Task<IEnumerable<SecuredMessengerApiModel>> GetSentListAsync(RequestOptions requestOptions)
        {
            IEnumerable<SecuredMessengerApiModel> localUserMessages = await _localService.GetSentListAsync(requestOptions).Free();
            if (!New<AxCryptOnlineState>().IsOnline || Identity == LogOnIdentity.Empty)
            {
                return localUserMessages;
            }

            try
            {
                IEnumerable<SecuredMessengerApiModel> remoteMessages = await _remoteService.GetSentListAsync(requestOptions).Free();
                if (remoteMessages == null)
                {
                    return localUserMessages;
                }

                if (localUserMessages != remoteMessages)
                {
                    await _localService.SaveMessagelist(remoteMessages.LastOrDefault()).Free();
                }

                return remoteMessages;
            }
            catch (ApiException aex)
            {
                await aex.HandleApiExceptionAsync();
            }

            return localUserMessages;
        }

        public async Task<IEnumerable<SecuredMessengerApiModel>> GetUnreadListAsync(RequestOptions requestOptions)
        {
            IEnumerable<SecuredMessengerApiModel> localUserMessages = await _localService.GetUnreadListAsync(requestOptions).Free();
            if (!New<AxCryptOnlineState>().IsOnline || Identity == LogOnIdentity.Empty)
            {
                return localUserMessages;
            }

            try
            {
                IEnumerable<SecuredMessengerApiModel> remoteMessages = await _remoteService.GetUnreadListAsync(requestOptions).Free();
                if (remoteMessages == null)
                {
                    return localUserMessages;
                }

                if (localUserMessages != remoteMessages)
                {
                    await _localService.SaveMessagelist(remoteMessages.LastOrDefault()).Free();
                }

                return remoteMessages;
            }
            catch (ApiException aex)
            {
                await aex.HandleApiExceptionAsync();
            }

            return localUserMessages;
        }

        public async Task<SecuredMessengerRootApiModel> GetAsync(Guid id, string userEmail)
        {
            SecuredMessengerRootApiModel localUserMessages = await _localService.GetAsync(id, userEmail).Free();
            if (!New<AxCryptOnlineState>().IsOnline || Identity == LogOnIdentity.Empty)
            {
                return localUserMessages;
            }

            try
            {
                SecuredMessengerRootApiModel remoteMessages = await _remoteService.GetAsync(id, userEmail).Free();
                if (remoteMessages == null)
                {
                    return localUserMessages;
                }

                if (localUserMessages != remoteMessages)
                {
                    await _localService.SavemessagesAsync(remoteMessages).Free();
                }

                return remoteMessages;
            }
            catch (ApiException aex)
            {
                await aex.HandleApiExceptionAsync();
            }

            return localUserMessages;
        }

        public async Task<bool> UpdateAsync(IEnumerable<Guid> ids, string userEmail, bool isUnread)
        {
            if (New<AxCryptOnlineState>().IsOnline)
            {
                try
                {
                    return await _remoteService.UpdateAsync(ids, userEmail, isUnread).Free();
                }
                catch (ApiException aex)
                {
                    await aex.HandleApiExceptionAsync();
                }
            }

            return await _localService.UpdateAsync(ids, userEmail, isUnread).Free();
        }

        public async Task<bool> DeleteAsync(IEnumerable<Guid> ids, string user, SecureMsgrFilterTab securedMessengerFilter)
        {
            if (New<AxCryptOnlineState>().IsOnline)
            {
                try
                {
                    return await _remoteService.DeleteAsync(ids, user, securedMessengerFilter).Free();
                }
                catch (ApiException aex)
                {
                    await aex.HandleApiExceptionAsync();
                }
            }

            return await _localService.DeleteAsync(ids, user, securedMessengerFilter).Free();
        }

        public async Task<bool> SaveMessagelist(SecuredMessengerApiModel model)
        {
            if (New<AxCryptOnlineState>().IsOnline)
            {
                try
                {
                    return await _remoteService.SaveMessagelist(model).Free();
                }
                catch (ApiException aex)
                {
                    await aex.HandleApiExceptionAsync();
                }
            }

            return await _localService.SaveMessagelist(model).Free();
        }

        public async Task<bool> CreateAsync(SecuredMessengerApiModel model)
        {
            if (New<AxCryptOnlineState>().IsOnline)
            {
                try
                {
                    return await _remoteService.CreateAsync(model).Free();
                }
                catch (ApiException aex)
                {
                    await aex.HandleApiExceptionAsync();
                }
            }

            return await _localService.CreateAsync(model).Free();
        }

        public async Task<bool> SavemessagesAsync(SecuredMessengerRootApiModel model)
        {
            if (New<AxCryptOnlineState>().IsOnline)
            {
                try
                {
                    return await _remoteService.SavemessagesAsync(model).Free();
                }
                catch (ApiException aex)
                {
                    await aex.HandleApiExceptionAsync();
                }
            }

            return await _localService.SavemessagesAsync(model).Free();
        }

        public async Task<IEnumerable<SecuredMessengerRootApiModel>> GetSecMsgWithSearchFiltersAsync(SecureMsgrFilterTab securedMessengerFilterTab, RequestOptions requestOptions)
        {
            if (New<AxCryptOnlineState>().IsOnline)
            {
                try
                {
                    return await _remoteService.GetSecMsgWithSearchFiltersAsync(securedMessengerFilterTab, requestOptions).Free();
                }
                catch (ApiException aex)
                {
                    await aex.HandleApiExceptionAsync();
                }
            }

            return await _localService.GetSecMsgWithSearchFiltersAsync(securedMessengerFilterTab, requestOptions).Free();
        }
    }
}