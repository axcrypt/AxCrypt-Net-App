using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.CloudCore.DropBox
{
    public class DropBoxConfiguration
    {
        private static DeviceCategory _deviceType => New<ICloudDriveConfiguration>().CurrentDeviceCategory;

        // No secrets are committed to source. See CloudDriveSecrets for how official
        // builds and local developers supply these values.
        public static string ClientIdOrAppKey => CloudDriveSecrets.Get(CloudDriveSecrets.DropBoxAppKey);

        public static string AppSecret => CloudDriveSecrets.Get(CloudDriveSecrets.DropBoxAppSecret);

        public static readonly string AuthorizeUrl = "https://www.dropbox.com/oauth2/authorize";
        public static readonly string AccessTokenUrl = "https://api.dropboxapi.com/oauth2/token";

        public static string RedirectUrl => New<ICloudDriveConfiguration>().RedirectUrl;

        public static int ChunkFileSize =>
        _deviceType switch
        {
            _ when _deviceType == DeviceCategory.Android || _deviceType == DeviceCategory.iOS => 8 * 1024 * 102,
            _ when _deviceType == DeviceCategory.Windows || _deviceType == DeviceCategory.Mac || _deviceType == DeviceCategory.Linux => 16 * 1024 * 102,
            _ => 8 * 1024 * 102
        };
    }
}