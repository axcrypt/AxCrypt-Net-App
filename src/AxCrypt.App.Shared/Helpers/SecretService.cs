using AxCrypt.Api.Model.Secret;
using AxCrypt.App.Shared.Models.Secret;

namespace AxCrypt.App.Shared.Helpers;

public class SecretService
{
    public SecretViewModel CurrentSecret { get; set; }

    public SecretType SecretType { get; set; }

    public void SetCurrentSecret(SecretViewModel secret)
    {
        CurrentSecret = secret;
    }

    public SecretViewModel GetCurrentSecret()
    {
        return CurrentSecret;
    }
}