using AxCrypt.Abstractions;
using AxCrypt.Api.Extension;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Extensions;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Cryptor
{
    public static class TextCryptor
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "displayContext")]
        private static IAxCryptDocument Document(Stream source, LogOnIdentity identity)
        {
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            IAxCryptDocument document = New<AxCryptFactory>().CreateDocument(identity.DecryptionParameters(), source);
            return document;
        }

        public static async Task<string> EncryptTextAsync(LogOnIdentity identity, string messageJson, IEnumerable<UserPublicKey>? sharedKeyHolders)
        {
            if (identity is null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            if (messageJson is null)
            {
                throw new ArgumentNullException(nameof(messageJson));
            }

            return await InternalEncryptTextAsync(identity, messageJson, sharedKeyHolders);
        }

        private static async Task<string> InternalEncryptTextAsync(LogOnIdentity identity, string plainText, IEnumerable<UserPublicKey>? sharedKeyHolders = null)
        {
            Guid cryptoId = Resolve.CryptoFactory.Default(New<ICryptoPolicy>()).CryptoId;
            EncryptionParameters encryptionParameters = new EncryptionParameters(cryptoId, identity);
            if (sharedKeyHolders != null)
            {
                await AddSharingParameters(encryptionParameters, sharedKeyHolders);
            }

            string userEmail = identity.UserEmail.Address;
            byte[] encryptedText;
            try
            {
                byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(plainText);

                using (MemoryStream destinationStream = new MemoryStream())
                {
                    using (MemoryStream sourceStream = new MemoryStream(byteArray))
                    {
                        using (IAxCryptDocument document = New<AxCryptFactory>().CreateDocument(encryptionParameters))
                        {
                            document.FileName = $"{userEmail}.secrets";
                            document.CreationTimeUtc = New<INow>().Utc;
                            document.LastWriteTimeUtc = New<INow>().Utc;
                            document.EncryptTo(sourceStream, destinationStream, AxCryptOptions.EncryptWithCompression);
                        }
                    }
                    encryptedText = destinationStream.ToArray();
                }
            }
            finally
            {
            }

            return encryptedText.GetCipherString();
        }

        private static async Task AddSharingParameters(EncryptionParameters parameters, IEnumerable<UserPublicKey> sharedKeyHolders)
        {
            if (sharedKeyHolders == null || !sharedKeyHolders.Any())
            {
                return;
            }

            await parameters.AddAsync(sharedKeyHolders);
        }

        public static string? DecryptText(LogOnIdentity identity, string encryptedText, AxCrypt.Core.Service.UserKeyPair? userKeyPair = null)
        {
            if (identity is null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            return InternalDecryptTextAsync(identity, encryptedText, userKeyPair);
        }

        private static string? InternalDecryptTextAsync(LogOnIdentity identity, string encryptedText, AxCrypt.Core.Service.UserKeyPair? userKeyPair)
        {
            string decryptedText = "";
            using (MemoryStream sourceStream = new MemoryStream(encryptedText.GetCipherBytes()))
            {
                IAxCryptDocument document = Document(sourceStream, identity);
                if (!document.PassphraseIsValid)
                {
                    return null;
                }

                using (MemoryStream destinationStream = new MemoryStream())
                {
                    document.DecryptTo(destinationStream);

                    byte[] destinationBytes = destinationStream.ToArray();
                    decryptedText = System.Text.Encoding.UTF8.GetString(destinationBytes, 0, destinationBytes.Length);
                }
            }

            return decryptedText;
        }
    }
}