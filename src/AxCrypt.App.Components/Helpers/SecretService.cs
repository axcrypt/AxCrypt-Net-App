using AxCrypt.App.Components.Models.Secret;

namespace AxCrypt.App.Components.Helpers
{
    public class SecretService
    {
        public SecretViewModel CurrentSecret { get; set; }

        public void SetCurrentSecret(SecretViewModel secret)
        {
            CurrentSecret = secret;
        }

        public SecretViewModel GetCurrentSecret()
        {
            return CurrentSecret;
        }
    }
}