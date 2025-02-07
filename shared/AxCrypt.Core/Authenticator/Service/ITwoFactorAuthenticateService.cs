namespace AxCrypt.Core.Authenticator.Service;

public interface ITwoFactorAuthenticateService
{
    Task<bool> VerifyTwoFactorAsync(string oneTimeCode, string activeTFAUniqueKey);
}