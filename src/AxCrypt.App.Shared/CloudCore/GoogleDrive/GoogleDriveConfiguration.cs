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

        public static string ClientId =>
        _deviceType switch
        {
            _ when _deviceType == DeviceCategory.iOS => "131011153195-22fcmc5j7gbpijl9atbfkl0lulcf8vl2.apps.googleusercontent.com",
            _ when _deviceType == DeviceCategory.Android => "131011153195-hqc84rbfq31ht7237pjia2ofm0q3d2mc.apps.googleusercontent.com",
            _ when _deviceType == DeviceCategory.Windows || _deviceType == DeviceCategory.Mac => "131011153195-r9l8gv5cu2828di1o6d4a5gion4354ao.apps.googleusercontent.com",
            _ => ""
        };

        public static string ClientSecret =>
        _deviceType switch
        {
            _ when _deviceType == DeviceCategory.Windows || _deviceType == DeviceCategory.Mac => "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",
            _ => ""
        };
    }
}