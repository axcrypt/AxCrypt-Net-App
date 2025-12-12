using AxCrypt.Api.Model;
using AxCrypt.Api.Model.TextEncryption;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.UI;

namespace AxCrypt.Core.Service.TextEncryption
{
    public interface ITextEncryptionService
    {
        ITextEncryptionService Refresh();

        LogOnIdentity Identity { get; }

        Task<Guid> ShareTextAsync(TextEncryptionApiModel textEncryptionApiModel);

        Task<IEnumerable<UserPublicKey>> GetUserPublicKeyAsync(IEnumerable<EmailAddress> sharedUsers);
    }
}