using AxCrypt.Api.Model.TextEncryption;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AxCrypt.Core.Service.TextEncryption
{
    public class NullTextEncryptionService : ITextEncryptionService
    {
        private static readonly Task<Guid> _completedTask = Task.FromResult(Guid.Empty);

        public NullTextEncryptionService(LogOnIdentity identity)
        {
            Identity = identity;
        }

        public ITextEncryptionService Refresh()
        {
            return this;
        }

        public LogOnIdentity Identity
        {
            get; private set;
        }

        public Task<Guid> ShareTextAsync(TextEncryptionApiModel textEncryptionApiModel)
        {
            return _completedTask;
        }

        public Task<IEnumerable<UserPublicKey>> GetUserPublicKeyAsync(IEnumerable<EmailAddress> users)
        {
            return Task.FromResult((IEnumerable<UserPublicKey>?)null)!;
        }
    }
}