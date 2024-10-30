using AxCrypt.Api.Model.Secret;

namespace AxCrypt.App.Components.Services
{
    public class SecretServiceUtility
    {
        public SecretType CurrentSecretType { get; set; }

        public bool SaveSuccess { get; set; }

        public void SetSuccess(bool success)
        {
            SaveSuccess = success;
        }
    }
}