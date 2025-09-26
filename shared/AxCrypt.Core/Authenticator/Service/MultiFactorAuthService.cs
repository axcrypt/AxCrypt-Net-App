using AxCrypt.Abstractions;
using AxCrypt.Api.Model;
using AxCrypt.Api.Model.MFA;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Service;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System.Text;
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

    public async Task<bool> SaveDeviceAndExpiryInfo(LogOnEventArgs eventArgs, LogOnIdentity logOnIdentity)
    {
        try
        {
            string deviceInfo = Convert.ToBase64String(Encoding.UTF8.GetBytes(eventArgs.UserDevice));

            IAccountService accountService = New<LogOnIdentity, IAccountService>(logOnIdentity);
            MultiFactorAuthApiModel multiFactorAuthApi = new MultiFactorAuthApiModel
            {
                UserEmail = eventArgs.UserEmail,
                UserDevice = deviceInfo,
                RememberUntil = eventArgs.RememberUntil,
                UpdatedUtc = New<INow>().Utc,
            };

            return await accountService.UpdateRememberMeOnMFAInfoAsync(multiFactorAuthApi);
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}