using AxCrypt.Api.Model;
using AxCrypt.Core.Crypto;

namespace AxCrypt.Core.Authenticator.Service;

public interface IMultiFactorAuthService
{
    Task<bool> SendMFAOTPAsync(LogOnIdentity logOnIdentity);

    Task<bool> VerifyMultiFactorAuthAsync(string oneTimeCode, string activeTFAUniqueKey, MultiFactorAuthType multiFactorAuthType);
}