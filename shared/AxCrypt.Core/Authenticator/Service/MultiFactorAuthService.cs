using AxCrypt.Api.Model;
using AxCrypt.Api.Model.MFA;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Service;
using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.Authenticator.Service;

public class MultiFactorAuthService : IMultiFactorAuthService
{
    public async Task<bool> VerifyMultiFactorAuthAsync(string oneTimeCode, string activeTFAUniqueKey, MultiFactorAuthType multiFactorAuthType)
    {
        if (multiFactorAuthType.HasFlag(MultiFactorAuthType.Email) || multiFactorAuthType.HasFlag(MultiFactorAuthType.SMS))
        {
            return oneTimeCode == New<KnownIdentities>().DefaultEncryptionIdentity.ActiveMFAOneTimeCode;
        }

        return new MFAAuthApp().ValidateTwoFactorPIN(activeTFAUniqueKey, oneTimeCode, false);
    }

    public async Task<bool> SendMFAOTPAsync(LogOnIdentity logOnIdentity)
    {
        try
        {
            IAccountService accountService = New<LogOnIdentity, IAccountService>(logOnIdentity);
            MultiFactorAuthOTPApiModel authOTPApiModel = await accountService.SendMFAOtpAsync(logOnIdentity.UserEmail.Address);
            if (authOTPApiModel == null)
            {
                return false;
            }

            New<KnownIdentities>().DefaultEncryptionIdentity.SetMFAOnetimeCode(authOTPApiModel.Otp, authOTPApiModel.Expiration);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}