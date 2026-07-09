using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.CloudCore.OneDrive
{
    public class OneDriveConfiguration
    {
        private static string _redirectUrl => New<ICloudDriveConfiguration>().RedirectUrl;
        private static DeviceCategory _deviceType => New<ICloudDriveConfiguration>().CurrentDeviceCategory;

        public static readonly string AUTHORIZE_URL = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";

        public static readonly string ACCESSTOKEN_URL = "https://login.microsoftonline.com/common/oauth2/v2.0/token";

        // No secrets are committed to source. See CloudDriveSecrets for how official
        // builds and local developers supply this value.
        public static string CLIENT_ID => CloudDriveSecrets.Get(CloudDriveSecrets.OneDriveClientId);

        public static readonly string[] SCOPES = new string[] { "Files.ReadWrite.All" };

        public new static string RedirectUrl
        {
            get
            {
                return _redirectUrl.Replace(":/oauth2redirect", "://oauth2redirect");
            }
        }

        public static int ChunkFileSize =>
        _deviceType switch
        {
            _ when _deviceType == DeviceCategory.Android || _deviceType == DeviceCategory.iOS => 8 * 1024 * 102,
            _ when _deviceType == DeviceCategory.Windows || _deviceType == DeviceCategory.Mac || _deviceType == DeviceCategory.Linux => 16 * 1024 * 102,
            _ => 8 * 1024 * 102
        };
    }
}