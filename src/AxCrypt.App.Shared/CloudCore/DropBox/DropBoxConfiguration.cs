using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility;

namespace AxCrypt.App.Shared.CloudCore.DropBox
{
    public class DropBoxConfiguration
    {
        private static string _redirectUrl;
        private static DeviceCategory _deviceType;

        public static void Initialize(ICloudDriveConfiguration config)
        {
            _redirectUrl = config.RedirectUrl;
            _deviceType = config.CurrentDeviceCategory;
        }

        public static readonly string ClientIdOrAppKey = "omrx7hccdskf45r";

        public static readonly string AppSecret = "enma8m952i35ojh";

        public static readonly string AuthorizeUrl = "https://www.dropbox.com/oauth2/authorize";
        public static readonly string AccessTokenUrl = "https://api.dropboxapi.com/oauth2/token";

        public static string RedirectUrl => _redirectUrl;

        public static int ChunkFileSize =>
        _deviceType switch
        {
            _ when _deviceType == DeviceCategory.Android || _deviceType == DeviceCategory.iOS => 8 * 1024 * 102,
            _ when _deviceType == DeviceCategory.Windows || _deviceType == DeviceCategory.Mac => 16 * 1024 * 102,
            _ => 8 * 1024 * 102
        };
    }
}