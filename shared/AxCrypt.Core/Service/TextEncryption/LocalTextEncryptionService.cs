using AxCrypt.Api.Model.TextEncryption;
using AxCrypt.Common;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.Service.TextEncryption
{
    public class LocalTextEncryptionService : ITextEncryptionService
    {
        public LocalTextEncryptionService()
        {
        }

        public ITextEncryptionService Refresh()
        {
            return this;
        }

        public LogOnIdentity Identity
        {
            get;
        }

        public Task<Guid> ShareTextAsync(TextEncryptionApiModel textEncryptionApiModel)
        {
            return Task.FromResult(Guid.Empty);
        }

        public async Task<IEnumerable<UserPublicKey>> GetUserPublicKeyAsync(IEnumerable<EmailAddress> sharedUsers)
        {
            return await Task.Run(() =>
            {
                using (KnownPublicKeys knowPublicKeys = New<KnownPublicKeys>())
                {
                    IEnumerable<UserPublicKey> publicKeys = knowPublicKeys.PublicKeys.Where(pk => sharedUsers.Contains(pk.Email));
                    return publicKeys;
                }
            }).Free();
        }
    }
}