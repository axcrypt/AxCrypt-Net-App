using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.CloudCore.GoogleDrive
{
    public class GoogleDriveConfiguration
    {
        private static DeviceCategory _deviceType => New<ICloudDriveConfiguration>().CurrentDeviceCategory;

        public static readonly string AuthorizeUrl = "https://accounts.google.com/o/oauth2/v2/auth";
        public static readonly string AccessTokenUrl = "https://oauth2.googleapis.com/token";

        public static string RedirectUrl => New<ICloudDriveConfiguration>().RedirectUrl;
        public static string ApplicationId => New<ICloudDriveConfiguration>().ApplicationId;

        public static readonly string[] GoogleAPIScopes =
        {
             "https://www.googleapis.com/auth/drive"
        };

        public static int ChunkFileSize =>
        _deviceType switch
        {
            _ when _deviceType == DeviceCategory.Android || _deviceType == DeviceCategory.iOS => 8 * 1024 * 102,
            _ when _deviceType == DeviceCategory.Windows || _deviceType == DeviceCategory.Mac => 16 * 1024 * 102,
            _ => 8 * 1024 * 102
        };

        // No secrets are committed to source. See CloudDriveSecrets for how official
        // builds and local developers supply these values.
        public static string ClientId =>
        _deviceType switch
        {
            _ when _deviceType == DeviceCategory.iOS => CloudDriveSecrets.Get(CloudDriveSecrets.GoogleClientIdIos),
            _ when _deviceType == DeviceCategory.Android => CloudDriveSecrets.Get(CloudDriveSecrets.GoogleClientIdAndroid),
            _ when _deviceType == DeviceCategory.Windows || _deviceType == DeviceCategory.Mac => CloudDriveSecrets.Get(CloudDriveSecrets.GoogleClientIdDesktop),
            _ => ""
        };

        public static string ClientSecret =>
        _deviceType switch
        {
            _ when _deviceType == DeviceCategory.Windows || _deviceType == DeviceCategory.Mac => CloudDriveSecrets.Get(CloudDriveSecrets.GoogleClientSecretDesktop),
            _ => ""
        };
    }
}