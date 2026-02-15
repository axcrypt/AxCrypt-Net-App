using AxCrypt.App.Shared.ViewModels.Authentication;

namespace AxCrypt.App.Shared.Services.Interface
{
    public interface ICloudPlatformService
    {
        Task InitializeCloudAuth(OAuth2Auth OAuth2Authenticator);
    }
}