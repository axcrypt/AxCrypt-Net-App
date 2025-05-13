using AxCrypt.Api.Model.SecuredMessenger;
using AxCrypt.Api.SecuredMessenger;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.SecuredMessenger;
using AxCrypt.Core.Service;
using AxCrypt.Core.Service.SecuredMessenger;
using AxCrypt.Core.UI;
using AxCrypt.Cryptor;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Api.Shared.Helper
{
    public static class SecuredMessengerFacade
    {
        public static async Task<IEnumerable<SecuredMessage>> GetInboxMessagesAsync(int pageNumber)
        {
            IEnumerable<SecuredMessengerApiModel> messages = await New<LogOnIdentity, ISecuredMessengerService>(Identity()).GetListAsync(GetRequestOptions(SecureMsgrSearchFilters.None, pageNumber));

            return messages.Select(m => m.CovertToMessengerModel());
        }

        public static async Task<IEnumerable<SecuredMessage>> GetSentMessagesAsync(int pageNumber)
        {
            IEnumerable<SecuredMessengerApiModel> messages = await New<LogOnIdentity, ISecuredMessengerService>(Identity()).GetSentListAsync(GetRequestOptions(SecureMsgrSearchFilters.None, pageNumber));
            return messages.Select(m => m.CovertToMessengerModel());
        }

        public static async Task<IEnumerable<SecuredMessage>> GetUnreadMessagesAsync(int pageNumber)
        {
            IEnumerable<SecuredMessengerApiModel> messages = await New<LogOnIdentity, ISecuredMessengerService>(Identity()).GetUnreadListAsync(GetRequestOptions(SecureMsgrSearchFilters.None, pageNumber));
            return messages.Select(m => m.CovertToMessengerModel());
        }

        public static async Task<IEnumerable<SecuredMessage>> GetMessageRepliesAsync(Guid messageId, SecureMsgrFilterTab secMessengerFilterTab)
        {
            SecuredMessengerRootApiModel rootMessage = await New<LogOnIdentity, ISecuredMessengerService>(Identity()).GetAsync(messageId, Identity().UserEmail.Address);
            if (rootMessage.Message == null && rootMessage.Replies == null)
            {
                return new List<SecuredMessage>();
            }
            return await LoadMessageInfoAsync(rootMessage, false, null, secMessengerFilterTab);
        }

        public static async Task<SecuredMessage> GetMessageAsync(Guid messageId)
        {
            SecuredMessengerRootApiModel rootMessage = await New<LogOnIdentity, ISecuredMessengerService>(Identity()).GetAsync(messageId, Identity().UserEmail.Address);
            if (rootMessage.Message.MessageId == messageId)
            {
                return await LoadViewMessageInfoAsync(rootMessage.Message);
            }

            SecuredMessengerApiModel message = rootMessage.Replies.FirstOrDefault(sec => sec.MessageId == messageId);
            return await LoadViewMessageInfoAsync(message);
        }

        public static async Task<bool> PostMessage(SecuredMessengerApiModel messengerApiModel)
        {
            if (messengerApiModel == null)
            {
                return false;
            }

            if (messengerApiModel.Receiver == null || !messengerApiModel.Receiver.Any())
            {
                return false;
            }

            await PrepareSaveSecuredMessageAsync(messengerApiModel);

            return await New<LogOnIdentity, ISecuredMessengerService>(Identity()).CreateAsync(messengerApiModel);
        }

        public static async Task<IEnumerable<SecuredMessage>> GetSecMsgSearchFilterAsync(SecureMsgrFilterTab securedMessengerFilterTab, RequestOptions requestOptions)
        {
            bool isForSearch = true;
            if (requestOptions.QueryString == string.Empty)
            {
                isForSearch = false;
            }
            List<SecuredMessage> messengers = new List<SecuredMessage>();
            IEnumerable<SecuredMessengerRootApiModel> allSecuredUserMessages = await New<LogOnIdentity, ISecuredMessengerService>(Identity()).GetSecMsgWithSearchFiltersAsync(securedMessengerFilterTab, requestOptions);
            for (int i = 0; i < allSecuredUserMessages.Count(); i++)
            {
                SecuredMessengerRootApiModel message = allSecuredUserMessages.ElementAt(i);
                messengers.AddRange(await LoadMessageInfoAsync(message, isForSearch));
            }

            return messengers;
        }

        private static async Task PrepareSaveSecuredMessageAsync(SecuredMessengerApiModel messengerApiModel)
        {
            IList<UserPublicKey> usersPublicKeys = new List<UserPublicKey>();
            IList<MessengerReceiverApiModel> sharedWithUsers = new List<MessengerReceiverApiModel>();
            foreach (MessengerReceiverApiModel receiver in messengerApiModel.Receiver)
            {
                UserPublicKey receiverPublicKey = await UserPublicKey(receiver);
                if (receiverPublicKey == null)
                {
                    continue;
                }

                usersPublicKeys.Add(receiverPublicKey);
                sharedWithUsers.Add(receiver);
            }

            if (!usersPublicKeys.Any())
            {
                return;
            }

            messengerApiModel.EncryptedMessage = await TextCryptor.EncryptTextAsync(Identity(), messengerApiModel.EncryptedMessage, usersPublicKeys);
            messengerApiModel.Receiver = sharedWithUsers;
        }

        private static async Task<UserPublicKey> UserPublicKey(MessengerReceiverApiModel adminUser)
        {
            try
            {
                return await New<LogOnIdentity, ISecuredMessengerService>(Identity()).OtherPublicKeyAsync(EmailAddress.Parse(adminUser.User));//EmailAddress.parse
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private static async Task<string> GetDecryptedMessageAsync(string encryptedMessage)
        {
            if (encryptedMessage == null)
            {
                throw new ArgumentNullException(nameof(encryptedMessage));
            }

            AxCrypt.Core.Crypto.LogOnIdentity identity = New<KnownIdentities>().DefaultEncryptionIdentity;
            IEnumerable<UserKeyPair> currentKeyPairs = await New<LogOnIdentity, IAccountService>(identity).ListAsync();
            identity = new LogOnIdentity(currentKeyPairs, identity.Passphrase);

            return TextCryptor.DecryptTextAsync(identity, encryptedMessage);
        }

        private static RequestOptions GetRequestOptions(SecureMsgrSearchFilters secMsgSearchFilters, int pageNumber = 0)
        {
            RequestOptions options = new RequestOptions();

            options.UserName = Identity().UserEmail.Address;
            options.PageCount = SecMessengerUtility.SecuredMessagePageCount;
            options.PageNumber = pageNumber;

            return options;
        }

        private static async Task<IEnumerable<SecuredMessage>> LoadMessageInfoAsync(SecuredMessengerRootApiModel rootMessage, bool isForSearch = false, Guid? messageId = null, SecureMsgrFilterTab securedMessengerFilterTab = SecureMsgrFilterTab.None)
        {
            IList<SecuredMessage> messengers = new List<SecuredMessage>();
            if (rootMessage == null)
            {
                return messengers;
            }

            int msgIndex = 0;
            bool canDecryptMessage = false;

            if (securedMessengerFilterTab == SecureMsgrFilterTab.Inbox)
            {
                rootMessage.Replies = rootMessage.Replies.Where(rm => (rm.Visibility == nameof(SecureMsgrVisibility.Once) && rm.Receiver.First(ru => ru.User == New<UserSettings>().UserEmail).Read == DateTime.MinValue) || rm.Visibility != nameof(SecureMsgrVisibility.Once) && rm.VisibleUntil > New<Abstractions.INow>().Utc);
            }

            foreach (SecuredMessengerApiModel messenger in rootMessage.Replies)
            {
                canDecryptMessage = isForSearch || msgIndex == 0;
                SecuredMessage message = new SecuredMessage();

                if (isForSearch && messageId != null)
                {
                    canDecryptMessage = false;
                }

                if (messageId != null && messenger.MessageId == messageId)
                {
                    canDecryptMessage = true;
                    message = await InternalGetMessengerAsync(messenger, canDecryptMessage);
                    if (message == null)
                    {
                        msgIndex++;
                        continue;
                    }
                }
                else
                {
                    message = await InternalGetMessengerAsync(messenger, canDecryptMessage);
                    if (message == null)
                    {
                        msgIndex++;
                        continue;
                    }
                }

                messengers.Add(message);
                msgIndex++;
            }

            if (rootMessage.Message.MessageId != Guid.Empty)
            {
                canDecryptMessage = isForSearch || !messengers.Any();
                SecuredMessage parentMsg = await InternalGetMessengerAsync(rootMessage.Message, canDecryptMessage);
                messengers.Add(parentMsg);
            }

            return messengers;
        }

        private static async Task<SecuredMessage> InternalGetMessengerAsync(SecuredMessengerApiModel rootMessage, bool canDecryptMsg)
        {
            if (canDecryptMsg)
            {
                return await LoadViewMessageInfoAsync(rootMessage);
            }

            return rootMessage.CovertToMessengerModel(string.Empty);
        }

        private static async Task<SecuredMessage> LoadViewMessageInfoAsync(SecuredMessengerApiModel message)
        {
            string plainMessage = await GetDecryptedMessageAsync(message?.EncryptedMessage);
            if (string.IsNullOrEmpty(plainMessage))
            {
                return null;
            }

            return message.CovertToMessengerModel(plainMessage);
        }

        private static SecuredMessage CovertToMessengerModel(this SecuredMessengerApiModel messengerApiModel, string plainMessage = null)
        {
            SecureMsgrVisibility msgVisibility = SecureMsgrVisibility.Forever;
            if (!string.IsNullOrEmpty(messengerApiModel.Visibility))
            {
                msgVisibility = (SecureMsgrVisibility)Enum.Parse(typeof(SecureMsgrVisibility), messengerApiModel.Visibility);
            }
            return new SecuredMessage
            {
                Id = messengerApiModel.MessageId,
                ParentId = messengerApiModel.ParentId,
                Message = new AxCrypt.Core.SecuredMessenger.UserSecuredMessage()
                {
                    TheMessage = plainMessage ?? messengerApiModel.EncryptedMessage,
                    ReceiverList = messengerApiModel.Receiver,
                    Username = messengerApiModel.Sender,
                    Visibility = msgVisibility,
                    VisibleUntil = messengerApiModel.VisibleUntil,
                },
                CreatedUtc = messengerApiModel.CreatedUtc,
                DBId = messengerApiModel.Id,
                UpdatedUtc = messengerApiModel.UpdatedUtc,
            };
        }

        private static LogOnIdentity Identity()
        {
            return New<KnownIdentities>().DefaultEncryptionIdentity;
        }
    }
}