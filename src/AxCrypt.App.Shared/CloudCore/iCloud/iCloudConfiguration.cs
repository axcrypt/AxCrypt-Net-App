using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.CloudCore.iCloud;

public static class iCloudConfiguration
{
    public const string ContainerId = "iCloud.net.axcrypt.app.maui";
 
    public static bool SupportsNativeiCloudIntegration => OperatingSystem.IsMacCatalyst() || OperatingSystem.IsMacOS();
  
    public const int RequestTimeoutSeconds = 60;
 
    public const int DownloadTimeoutSeconds = 60;

    public const string CloudKitAPIUrl = "";

    public const string CloudKitServiceToken = "";  
    
    private static DeviceCategory _deviceType => New<ICloudDriveConfiguration>().CurrentDeviceCategory;

    public static readonly string AuthorizeUrl = "";
    public static readonly string AccessTokenUrl = "";

    public static string RedirectUrl => New<ICloudDriveConfiguration>().RedirectUrl;
    public static string ApplicationId => New<ICloudDriveConfiguration>().ApplicationId;

    public static readonly string[] iCloudAPIScopes = { "" };

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
        _ when _deviceType == DeviceCategory.iOS => "",
        _ when _deviceType == DeviceCategory.Mac => "",
        _ => ""
    };

    public static string ClientSecret =>
    _deviceType switch
    {
        _ when _deviceType == DeviceCategory.Windows || _deviceType == DeviceCategory.Mac => "",
        _ => ""
    };
}
