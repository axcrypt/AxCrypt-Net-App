namespace AxCrypt.Core.Authenticator.Service;

public class TwoFactorAuthService : ITwoFactorAuthenticateService
{
    public async Task<bool> VerifyTwoFactorAsync(string oneTimeCode, string activeTFAUniqueKey)
    {
        return new TwoFactorAuth().ValidateTwoFactorPIN(activeTFAUniqueKey, oneTimeCode, false);
    }
}