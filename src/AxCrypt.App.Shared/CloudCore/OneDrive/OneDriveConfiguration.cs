using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility;

namespace AxCrypt.App.Shared.CloudCore.OneDrive
{
    public class OneDriveConfiguration
    {
        private static string _redirectUrl;
        private static DeviceCategory _deviceType;

        public static void Initialize(ICloudDriveConfiguration config)
        {
            _redirectUrl = config.RedirectUrl;
            _deviceType = config.CurrentDeviceCategory;
        }

        public static readonly string AUTHORIZE_URL = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";

        public static readonly string ACCESSTOKEN_URL = "https://login.microsoftonline.com/common/oauth2/v2.0/token";

        public static readonly string CLIENT_ID = "ce1f3212-6b9d-4008-8013-c0e0c6cafc38";

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
            _ when _deviceType == DeviceCategory.Windows || _deviceType == DeviceCategory.Mac => 16 * 1024 * 102,
            _ => 8 * 1024 * 102
        };

        public new static DeviceCategory DeviceType => _deviceType;
    }
}