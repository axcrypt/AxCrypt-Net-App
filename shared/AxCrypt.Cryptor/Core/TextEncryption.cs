using AxCrypt.Abstractions;
using AxCrypt.Api.Extension;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Extensions;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Cryptor
{
    public static class TextEncryption
    {
        public static async Task<byte[]> EncryptAsync(EncryptionParameters encryptionParameters, string plainText)
        {
            if (encryptionParameters is null)
            {
                throw new ArgumentNullException(nameof(encryptionParameters));
            }

            if (plainText is null)
            {
                throw new ArgumentNullException(nameof(plainText));
            }
            // Avoid working with files in UI thread.
            return await Task.Run(() =>
            {
                return InternalEncryptTextAsync(encryptionParameters, plainText);
            });
        }

        private static byte[] InternalEncryptTextAsync(EncryptionParameters encryptionParameters, string plainText)
        {
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
                            // document.FileName = $"{userEmail}.secrets";
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

            return encryptedText;
        }

        public static async Task<string> DecryptAsync(IEnumerable<DecryptionParameter> decryptionParameters, byte[] encryptedData)
        {
            if (decryptionParameters is null)
            {
                throw new ArgumentNullException(nameof(decryptionParameters));
            }

            // Avoid working with files in UI thread.
            return await Task.Run(() =>
            {
                return InternalDecryptText(decryptionParameters, encryptedData);
            });
        }

        private static string InternalDecryptText(IEnumerable<DecryptionParameter> decryptionParameters, byte[] encryptedData)
        {
            string decryptedText = "";
            using (MemoryStream sourceStream = new MemoryStream(encryptedData))
            {
                IAxCryptDocument document = Document(sourceStream, decryptionParameters);
                if (!document.PassphraseIsValid)
                {
                    return decryptedText;
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

        private static IAxCryptDocument Document(Stream source, IEnumerable<DecryptionParameter> decryptionParameters)
        {
            if (decryptionParameters == null)
            {
                throw new ArgumentNullException(nameof(decryptionParameters));
            }

            IAxCryptDocument document = New<AxCryptFactory>().CreateDocument(decryptionParameters, source);
            return document;
        }

        private static async Task AddSharingParameters(EncryptionParameters parameters, IEnumerable<UserPublicKey> sharedKeyHolders)
        {
            if (sharedKeyHolders == null || !sharedKeyHolders.Any())
            {
                return;
            }

            await parameters.AddAsync(sharedKeyHolders);
        }

        public static IEnumerable<DecryptionParameter> TextDecryptionParameters(this LogOnIdentity identity)
        {
            IEnumerable<DecryptionParameter> decryptionParameters = DecryptionParameter.CreateAll(new Passphrase[] { identity.Passphrase }, identity.GetPrivateKeys(), Resolve.CryptoFactory.OrderedIds.Where(id => id != new V1Aes128CryptoFactory().CryptoId));
            return decryptionParameters;
        }

        public static async Task<string> EncryptAsync(LogOnIdentity identity, string messageJson, IEnumerable<UserPublicKey> sharedKeyHolders)
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

        private static async Task<string> InternalEncryptTextAsync(LogOnIdentity identity, string plainText, IEnumerable<UserPublicKey> sharedKeyHolders = null)
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

        public static string DecryptTextAsync(LogOnIdentity identity, string encryptedText, AxCrypt.Core.Service.UserKeyPair userKeyPair = null)
        {
            if (identity is null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            return InternalDecryptTextAsync(identity, encryptedText, userKeyPair);
        }

        private static string InternalDecryptTextAsync(LogOnIdentity identity, string encryptedText, AxCrypt.Core.Service.UserKeyPair userKeyPair)
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