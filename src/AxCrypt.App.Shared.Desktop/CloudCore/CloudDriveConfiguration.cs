using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility;
using Microsoft.Maui.Devices;

namespace AxCrypt.App.Shared.Desktop.CloudCore
{
    public class CloudDriveConfiguration : ICloudDriveConfiguration
    {
        public static readonly string DesktopRedirectUrl = "http://127.0.0.1:5000/";

        public string RedirectUrl
        {
            get
            {
                if (DeviceInfo.Platform == DevicePlatform.WinUI || DeviceInfo.Platform == DevicePlatform.MacCatalyst || DeviceInfo.Platform == DevicePlatform.macOS)
                {
                    return DesktopRedirectUrl;
                }
                else
                {
                    return "";
                }
            }
        }

        public string ApplicationId
        {
            get
            {
                if (DeviceInfo.Platform == DevicePlatform.MacCatalyst || DeviceInfo.Platform == DevicePlatform.macOS)
                {
                    return "net.axcrypt.app.maui";
                }

                if (DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    return "axcrypt.net.app.windows";
                }

                return "AxCrypt.App.Linux";
            }
        }

        public DeviceCategory CurrentDeviceCategory
        {
            get
            {
                if (DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    return DeviceCategory.Windows;
                }
                else if (DeviceInfo.Platform == DevicePlatform.MacCatalyst || DeviceInfo.Platform == DevicePlatform.macOS)
                {
                    return DeviceCategory.Mac;
                }

                return DeviceCategory.None;
            }
        }
    }
}