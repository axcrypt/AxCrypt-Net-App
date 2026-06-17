using AxCrypt.Api.Model.TextEncryption;

namespace AxCrypt.Core.StoreApi
{
    public interface ITextEncryptionStoreApiService
    {
        Task<Guid> ShareAsync(TextEncryptionApiModel textEncryptionApiModel);

        Task<TextEncryptionApiModel> GetByIdAsync(Guid textId);

        Task<IEnumerable<TextEncryptionApiModel>> GetListByEmailAsync(string userEmail);
    }
}