#region Coypright and License

/*
 * AxCrypt - Copyright 2023, All Rights Reserved
 *
 * This file is part of AxCrypt.
 *
 * AxCrypt is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * AxCrypt is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with AxCrypt.  If not, see <http://www.gnu.org/licenses/>.
 *
 * The source is maintained at http://bitbucket.org/AxCrypt-net please visit for
 * updates, contributions and contact with the author. You may also visit
 * http://www.axcrypt.net for more information about the author.
*/

#endregion Coypright and License

using AxCrypt.Abstractions;
using AxCrypt.Api;
using AxCrypt.Api.Model;
using AxCrypt.Api.Model.SecuredMessenger;
using AxCrypt.Api.SecuredMessenger;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.IO;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Common;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.Service.SecuredMessenger
{
    public class LocalSecuredMessengerService : ISecuredMessengerService
    {
        private IDataContainer _workContainer;

        public static readonly string InboxMessageFileName = "AxInboxMessages.txt";

        public static readonly string SentMessageFileName = "AxSentMessages.txt";

        public LocalSecuredMessengerService(LogOnIdentity identity, IDataContainer workContainer)
        {
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }
            if (workContainer == null)
            {
                throw new ArgumentNullException(nameof(workContainer));
            }

            Identity = identity;
            _workContainer = workContainer;
        }

        public ISecuredMessengerService Refresh()
        {
            return this;
        }

        private IDataStore AxInboxMessagesStore
        {
            get
            {
                return _workContainer.FileItemInfo(InboxMessageFileName);
            }
        }

        private IDataStore AxSentMessagesStore
        {
            get
            {
                return _workContainer.FileItemInfo(SentMessageFileName);
            }
        }

        public LogOnIdentity Identity
        {
            get;
        }

        /// <summary>
        /// Fetches the encrypted user secrets.
        /// </summary>
        /// <returns>
        /// The encrypted user secrets.
        /// </returns>
        public async Task<IEnumerable<SecuredMessengerApiModel>> GetListAsync(RequestOptions requestOptions)
        {
            if (Identity.UserEmail == EmailAddress.Empty)
            {
                throw new InvalidOperationException("The account service requires a user.");
            }

            IEnumerable<SecuredMessengerApiModel> userSecrets = await Task.Run(() => LoadMessages());
            return userSecrets;
        }

        public async Task<IEnumerable<SecuredMessengerApiModel>> GetSentListAsync(RequestOptions requestOptions)
        {
            if (Identity.UserEmail == EmailAddress.Empty)
            {
                throw new InvalidOperationException("The account service requires a user.");
            }

            IEnumerable<SecuredMessengerApiModel> userSecrets = await Task.Run(() => LoadSentMessages());
            return userSecrets;
        }

        public async Task<IEnumerable<SecuredMessengerApiModel>> GetUnreadListAsync(RequestOptions requestOptions)
        {
            if (Identity.UserEmail == EmailAddress.Empty)
            {
                throw new InvalidOperationException("The account service requires a user.");
            }

            IEnumerable<SecuredMessengerApiModel> userSecrets = await Task.Run(() => LoadMessages());
            return userSecrets;
        }

        public Task<bool> CreateAsync(SecuredMessengerApiModel model)
        {
            return Task.FromResult(false);
        }

        public Task<bool> UpdateAsync(IEnumerable<Guid> ids, string userEmail, bool isUnread = false)
        {
            throw new NotImplementedException();
        }

        public Task<SecuredMessengerRootApiModel> GetAsync(Guid id, string userEmail)
        {
            return Task.FromResult(new SecuredMessengerRootApiModel());
        }

        public Task<bool> DeleteAsync(IEnumerable<Guid> ids, string user, SecureMsgrFilterTab securedMessengerFilter)
        {
            return Task.FromResult(false);
        }

        public Task<IEnumerable<SecuredMessengerRootApiModel>> GetSecMsgWithSearchFiltersAsync(SecureMsgrFilterTab securedMessengerFilterTab, RequestOptions requestOptions)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SaveMessagelist(SecuredMessengerApiModel messenger)
        {
            return Task.FromResult(false);
        }

        public async Task<bool> SavemessagesAsync(SecuredMessengerRootApiModel rootApiModel)
        {
            if (Identity.UserEmail == EmailAddress.Empty)
            {
                throw new InvalidOperationException("The account service requies a user.");
            }

            return await Task.Run(() => InternalSaveMessages(rootApiModel));
        }

        public async Task<UserPublicKey> OtherPublicKeyAsync(EmailAddress email)
        {
            if (Identity.UserEmail == EmailAddress.Empty)
            {
                throw new InvalidOperationException("The account service requires a user.");
            }

            return await Task.Run(() =>
            {
                using (KnownPublicKeys knowPublicKeys = New<KnownPublicKeys>())
                {
                    UserPublicKey publicKey = knowPublicKeys.PublicKeys.Where(pk => pk.Email == email).FirstOrDefault();
                    return publicKey;
                }
            }).Free();
        }

        private readonly long _maxAllowedSecretsCount = 10;

        public Task<long> GetFreeUserSecuredMessengerLimit(string userEmail)
        {
            return Task.FromResult(_maxAllowedSecretsCount);
        }

        public Task<bool> UpdateFreeUserSecuredMessengerLimit(string userEmail)
        {
            return Task.FromResult(false);
        }

        #region internal helper methods

        private IEnumerable<SecuredMessengerApiModel> LoadMessages()
        {
            if (!AxInboxMessagesStore.IsAvailable)
            {
                return Enumerable.Empty<SecuredMessengerApiModel>();
            }

            using (StreamReader reader = new StreamReader(AxInboxMessagesStore.OpenRead()))
            {
                EncryptedTextApiModel messengerModel = DeserializeFrom(reader);
                if (messengerModel == null)
                {
                    messengerModel = EncryptedTextApiModel.Empty;
                }
                SecuredMessengerApiModel convertedModel = ConvertToMessengerApiModel(messengerModel);
                return new List<SecuredMessengerApiModel> { convertedModel };
            }
        }

        private SecuredMessengerApiModel ConvertToMessengerApiModel(EncryptedTextApiModel messengerModel)
        {
            if (messengerModel == null)
            {
                throw new ArgumentNullException(nameof(messengerModel), "Messenger model cannot be null.");
            }

            return new SecuredMessengerApiModel
            {
                Sender = messengerModel.Sender,
                CreatedUtc = DateTime.UtcNow,
                EncryptedMessage = messengerModel.Cipher?.ToString() ?? string.Empty, // Avoid NullReferenceException
            };
        }

        private IEnumerable<SecuredMessengerApiModel> LoadSentMessages()
        {
            if (!AxInboxMessagesStore.IsAvailable)
            {
                return Enumerable.Empty<SecuredMessengerApiModel>();
            }

            using (StreamReader reader = new StreamReader(AxInboxMessagesStore.OpenRead()))
            {
                EncryptedTextApiModel messengerModel = DeserializeFrom(reader);
                if (messengerModel == null)
                {
                    messengerModel = EncryptedTextApiModel.Empty;
                }
                SecuredMessengerApiModel convertedModel = ConvertToMessengerApiModel(messengerModel);
                return new List<SecuredMessengerApiModel> { convertedModel };
            }
        }

        private bool InternalSaveMessages(SecuredMessengerRootApiModel rootApiModel)
        {
            using (StreamWriter writer = new StreamWriter(AxInboxMessagesStore.OpenWrite()))
            {
                SerializeTo(writer, rootApiModel);
            }

            return true;
        }

        #endregion internal helper methods

        #region Text Serializers

        public static EncryptedTextApiModel DeserializeFrom(TextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            string serialized = reader.ReadToEnd();
            return New<IStringSerializer>().Deserialize<EncryptedTextApiModel>(serialized);
        }

        public static IEnumerable<SecuredMessengerApiModel> DeserializeSharedSecretsFrom(TextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            string serialized = reader.ReadToEnd();
            return New<IStringSerializer>().Deserialize<IEnumerable<SecuredMessengerApiModel>>(serialized);
        }

        public void SerializeTo(TextWriter writer, SecuredMessengerRootApiModel rootApiModel)
        {
            string serializedString = New<IStringSerializer>().Serialize(rootApiModel);
            WriteToFile(writer, serializedString);
        }

        private static void WriteToFile(TextWriter writer, string serializedString)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            writer.Write(serializedString);
        }

        #endregion Text Serializers
    }
}