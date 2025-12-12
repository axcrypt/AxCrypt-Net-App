using AxCrypt.Api.Model;
using AxCrypt.Api.Model.TextEncryption;
using AxCrypt.Common;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.Service.TextEncryption
{
    public class DeviceTextEncryptionService : ITextEncryptionService
    {
        private ITextEncryptionService _localService;
        private ITextEncryptionService _remoteService;

        public DeviceTextEncryptionService(ITextEncryptionService localService, ITextEncryptionService remoteService)
        {
            _localService = localService;
            _remoteService = remoteService;
        }

        public ITextEncryptionService Refresh()
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

        public async Task<Guid> ShareTextAsync(TextEncryptionApiModel textEncryptionApiModel)
        {
            if (New<AxCryptOnlineState>().IsOnline && Identity != LogOnIdentity.Empty)
            {
                try
                {
                    return await _remoteService.ShareTextAsync(textEncryptionApiModel).Free();
                }
                catch (ApiException aex)
                {
                    await aex.HandleApiExceptionAsync();
                }
            }

            return await _localService.ShareTextAsync(textEncryptionApiModel).Free();
        }

        public async Task<IEnumerable<UserPublicKey>> GetUserPublicKeyAsync(IEnumerable<EmailAddress> users)
        {
            if (New<AxCryptOnlineState>().IsOnline && Identity != LogOnIdentity.Empty)
            {
                try
                {
                    return await _remoteService.GetUserPublicKeyAsync(users).Free();
                }
                catch (ApiException aex)
                {
                    await aex.HandleApiExceptionAsync();
                }
            }

            return await _localService.GetUserPublicKeyAsync(users).Free();
        }
    }
}