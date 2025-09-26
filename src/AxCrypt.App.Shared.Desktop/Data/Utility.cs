using AxCrypt.Abstractions;
using AxCrypt.App.Shared.ViewModels.Notification;
using Microsoft.JSInterop;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using System;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.Data;

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

    public static int MaxReceiversToDisplay = 2;

    public static string ToDateString(DateTime dateTime)
    {
        DateTime utcNow = New<INow>().Utc;
        if (dateTime.Year != utcNow.Year)
        {
            return dateTime.ToString("dd/MMM/yyyy");
        }

        if (dateTime.Month != utcNow.Month)
        {
            return dateTime.ToString("dd/MMM");
        }

        if (dateTime.AddDays(7) < utcNow.AddDays(-7))
        {
            return dateTime.ToString("dd/MMM");
        }

        return dateTime.ToString("ddd hh:mm tt");
    }

    public static string ToLocalFullDateString(DateTime dateTime)
    {
        return dateTime.ToLocalTime().ToString("dd/MMM/yyyy ddd hh:mm tt");
    }

    public static string GetCurrentDevice()
    {
        string os = $"{DeviceInfo.Platform} {DeviceInfo.VersionString}";
        string appVersion = AppInfo.VersionString;
        string deviceModel = $"{DeviceInfo.Manufacturer} {DeviceInfo.Model}";

        return $"{os}|{appVersion}|{deviceModel}";
    }
}