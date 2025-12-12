using AxCrypt.Api.Model;
using AxCrypt.Api.Model.TextEncryption;
using AxCrypt.Common;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.UI;

namespace AxCrypt.Core.Service.TextEncryption
{
    public class CachingTextEncryptionService : ITextEncryptionService
    {
        private ITextEncryptionService _service;

        public CachingTextEncryptionService(ITextEncryptionService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            _service = service;
        }

        public LogOnIdentity Identity => throw new NotImplementedException();

        public async Task<Guid> ShareTextAsync(TextEncryptionApiModel textEncryptionApiModel)
        {
            return await _service.ShareTextAsync(textEncryptionApiModel).Free();
        }

        public async Task<IEnumerable<UserPublicKey>> GetUserPublicKeyAsync(IEnumerable<EmailAddress> sharedUsers)
        {
            return await _service.GetUserPublicKeyAsync(sharedUsers).Free();
        }

        public ITextEncryptionService Refresh()
        {
            return this;
        }
    }
}