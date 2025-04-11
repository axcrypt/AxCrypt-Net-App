using AxCrypt.Core.Crypto;
using AxCrypt.Core.Service.Secrets;
using AxCrypt.Core.Service.SecuredMessenger;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.UI.ViewModel
{
    public class AdditionalUserSettings
    {
        public AdditionalUserSettings(LogOnIdentity identity)
        {
            Identity = identity;
        }

        public LogOnIdentity Identity
        {
            get; private set;
        }

        private string UserEmail
        {
            get
            {
                return Identity?.UserEmail.Address ?? string.Empty;
            }
        }

        public long FreeUserSecretsCount
        {
            get
            {
                if (!string.IsNullOrEmpty(Identity.UserEmail.Address) && !string.IsNullOrEmpty(UserEmail))
                {
                    return Task.Run(async () => await New<LogOnIdentity, ISecretsService>(Identity).GetFreeUserSecretsCount(UserEmail)).Result;
                }
                return 0;
            }
        }

        public bool UpdateFreeUserSecretsCount()
        {
            return Task.Run(async () => await New<LogOnIdentity, ISecretsService>(Identity).InsertFreeUserSecretsAsync(UserEmail)).Result;
        }

        public long FreeUserSendMessageCount
        {
            get
            {
                if (!string.IsNullOrEmpty(Identity.UserEmail.Address) && !string.IsNullOrEmpty(UserEmail))
                {
                    return Task.Run(async () => await New<LogOnIdentity, ISecuredMessengerService>(Identity).GetFreeUserSecuredMessengerLimit(UserEmail)).Result;
                }
                return 0;
            }
        }

        public bool UpdateFreeUserSecuredMessengerLimit()
        {
            return Task.Run(async () => await New<LogOnIdentity, ISecuredMessengerService>(Identity).UpdateFreeUserSecuredMessengerLimit(UserEmail)).Result;
        }
    }
}