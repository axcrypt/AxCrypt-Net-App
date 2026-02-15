using AxCrypt.App.Shared.Utility;

namespace AxCrypt.App.Shared.Services.Interface
{
    public interface ICloudDriveConfiguration
    {
        string RedirectUrl { get; }
        string ApplicationId { get; }

        DeviceCategory CurrentDeviceCategory { get; }
    }
}