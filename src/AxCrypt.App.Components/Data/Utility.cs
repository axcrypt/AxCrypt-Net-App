using AxCrypt.App.Components.Models.Notification;
using Microsoft.JSInterop;

namespace AxCrypt.App.Components.Data;

public static class Utility
{
    public static async Task<bool> IsWideScreenAsync(IJSRuntime jsRuntime)
    {
        int screenWidth = await jsRuntime.InvokeAsync<int>("getWidth");
        double wideScreenThreshold = 768; // Adjust as needed
        return screenWidth > wideScreenThreshold;
    }

    public static async Task<bool> IsLargeScreenAsync(IJSRuntime jsRuntime)
    {
        int screenWidth = await jsRuntime.InvokeAsync<int>("getWidth");
        double largeScreenThreshold = 1140; // Adjust as needed
        return screenWidth > largeScreenThreshold;
    }

    private static bool _isMainMenuHidden;

    public static bool IsMainMenuHidden
    {
        get => _isMainMenuHidden;
        set
        {
            _isMainMenuHidden = value;
            OnIsMainMenuHiddenChanged?.Invoke();
        }
    }

    public static event Action? OnIsMainMenuHiddenChanged;

    public static void ToggleMainMenu()
    {
        IsMainMenuHidden = !IsMainMenuHidden;
    }

    public static NotificationViewModel GetNotification()
    {
        //Services.Notification.INotificationService notificationService = new Services.Notification.NotificationService();
        NotificationViewModel notification = null;
        //System.Threading.Tasks.Task.Run(async () => { notification = await notificationService.GetNotificationList(); }).Wait();

        return notification;
    }

    public static bool IsBusinessUser
    {
        get
        {
            //    if (IsUserAuthorized())
            //    {
            //        return New<IXecretsUserGateway>(UserContext.Name).UserSubscriptionLevel == SubscriptionLevel.Business;
            //    }
            return false;
        }
    }

    public static bool IsPremiumUser
    {
        get
        {
            //if (IsUserAuthorized())
            {
                //return New<IXecretsUserGateway>(UserContext.Name).UserSubscriptionLevel == SubscriptionLevel.Premium;
            }
            return false;
        }
    }

    public static bool IsPasswordManager
    {
        get
        {
            //if (IsUserAuthorized())
            //{
            //    return New<IXecretsUserGateway>(UserContext.Name).UserSubscriptionLevel == SubscriptionLevel.PasswordManager;
            //}
            return false;
        }
    }

    private static bool IsUserAuthorized()
    {
        throw new NotImplementedException();
    }

    public static DeviceIdiom GetCurrentDeviceIdiom()
    {
        return DeviceInfo.Idiom;
    } 
    
    public static DevicePlatform GetCurrentPlatform()
    {
        return DeviceInfo.Platform;
    }

    public static bool ToggleUpgradePopup(bool currentState)
    {
        return !currentState;
    }
}