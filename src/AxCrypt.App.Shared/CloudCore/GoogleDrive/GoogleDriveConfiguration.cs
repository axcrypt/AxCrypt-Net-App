using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility;

namespace AxCrypt.App.Shared.CloudCore.GoogleDrive
{
    public class GoogleDriveConfiguration
    {
        private static string _redirectUrl;
        private static DeviceCategory _deviceType;
        private static string _applicationId;

        private static ICloudDriveConfiguration _config;

        public GoogleDriveConfiguration(ICloudDriveConfiguration config)
        {
            _config = config;
            Initialize();
            InitializeConfiguration();
        }

        private static void Initialize()
        {
            _redirectUrl = _config.RedirectUrl;
            _deviceType = _config.CurrentDeviceCategory;
            _applicationId = _config.ApplicationId;

            _clientId = "";
        }

        private static string _clientId;

        public static string ClientId
        {
            get
            {
                return _clientId;
            }
        }

        private static string _clientSecret;

        public static string ClientSecret
        {
            get
            {
                return _clientSecret;
            }
        }

        public static readonly string AuthorizeUrl = "https://accounts.google.com/o/oauth2/v2/auth";
        public static readonly string AccessTokenUrl = "https://oauth2.googleapis.com/token";

        public static string RedirectUrl => _redirectUrl;
        public static string ApplicationId => _applicationId;

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

        private void InitializeConfiguration()
        {
            if (_deviceType == DeviceCategory.iOS)
            {
                _clientId = "131011153195-22fcmc5j7gbpijl9atbfkl0lulcf8vl2.apps.googleusercontent.com";
            }
            else if (_deviceType == DeviceCategory.Android)
            {
                _clientId = "131011153195-hqc84rbfq31ht7237pjia2ofm0q3d2mc.apps.googleusercontent.com";
            }
            else if (_deviceType == DeviceCategory.Windows)
            {
                _clientId = "131011153195-r9l8gv5cu2828di1o6d4a5gion4354ao.apps.googleusercontent.com";
                _clientSecret = "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";
            }
            else
            {
                _clientId = "";
            }
        }
    }
}