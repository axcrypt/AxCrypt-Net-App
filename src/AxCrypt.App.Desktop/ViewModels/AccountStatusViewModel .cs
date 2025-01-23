using AxCrypt.Abstractions;
using AxCrypt.Api.Model;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Service;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Desktop.ViewModels;

public class AccountStatusViewModel : ViewModelBase
{
    public AccountStatusViewModel()
    {
        RefreshAccountInfoCommand = new Command(async () => await LoadAccountStatusAsync());
        InitializeBuyPremiumCommand = new Command(InitiateBuyPremiumAction);
        //SignOutCommand = new Command(SignOut);
        OpenGetStartedPageCommand = new Command(OpenGetStartedPage);
        StartLoadAccountStatus();
    }

    public PlanState PlanState { get; private set; }
    private static bool CanTryPremiumSubscription { get; set; }
    public Command RefreshAccountInfoCommand { get; private set; }
    public Command InitializeBuyPremiumCommand { get; private set; }
    public ICommand SignOutCommand { get; private set; }
    public ICommand OpenGetStartedPageCommand { get; private set; }

    public SubscriptionLevel SubscriptionLevel
    {
        get
        {
            if (PlanState == PlanState.HasBusiness)
            {
                return SubscriptionLevel.Business;
            }
            if (PlanState == PlanState.HasPremium)
            {
                return SubscriptionLevel.Premium;
            }
            if (PlanState == PlanState.HasPasswordManager)
            {
                return SubscriptionLevel.PasswordManager;
            }
            return SubscriptionLevel.Free;
        }
    }

    private async void StartLoadAccountStatus()
    {
        await LoadAccountStatusAsync();
    }

    public string UserEmail { get; set; } = string.Empty;
    public int DaysLeft { get; set; } = 0;
    public string ValidFormatted
    {
        get
        {
            if (DaysLeft == 0)
            {
                return "0 days left";
            }
            DateTime validDate = New<INow>().Utc.AddDays(DaysLeft);
            return validDate.ToString("dd MMM yyyy");
        }
    }

    public async Task LoadAccountStatusAsync()
    {
        KnownIdentities identityStorage = New<KnownIdentities>();
        LogOnIdentity identity = identityStorage.DefaultEncryptionIdentity;
        if (!identityStorage.IsLoggedOn && string.IsNullOrEmpty(identity.UserEmail.Address))
        {
            return;
        }

        UserEmail = identity.UserEmail.Address;
        PlanInformation pi = await PlanInformation.CreateAsync(identity);
        PlanState = pi.PlanState;
        DaysLeft = pi.DaysLeft;
        CanTryPremiumSubscription = pi.CanTryPremiumSubscription;
    }

    private string GetStatusText(PlanState planState)
    {
        if (planState == PlanState.HasPasswordManager)
        {
            return Texts.PromptPasswordManager;
        }

        if (planState == PlanState.HasPremium)
        {
            return Texts.PremiumAccountLabel;
        }

        if (planState == PlanState.HasBusiness)
        {
            return Texts.BusinessAccountLabel;
        }

        if (planState == PlanState.NoPremium || planState == PlanState.OfflineNoPremium || planState == PlanState.CanTryPremium)
        {
            return Texts.FreeLabel;
        }

        return string.Empty;
    }

    public void UpdateFreeAccountPlanInfo(PlanInformation pi)
    {
        KnownIdentities identityStorage = New<KnownIdentities>();
        if (!identityStorage.IsLoggedOn)
        {
            return;
        }
        PlanState = pi.PlanState;
        CanTryPremiumSubscription = pi.CanTryPremiumSubscription;
    }

    public async void InitiateBuyPremiumAction()
    {
        LogOnIdentity identity = New<KnownIdentities>().DefaultEncryptionIdentity;
        if (DeviceInfo.Platform == DevicePlatform.iOS)
        {
            //ShowInAppPurchasePage(identity);
            return;
        }
        await New<PremiumManager>().BuyPremium(identity);
    }

    //private async void SignOut()
    //{
    //    KnownIdentities identityStorage = New<KnownIdentities>();
    //    if (!identityStorage.IsLoggedOn)
    //    {
    //        return;
    //    }
    //    PlanState = PlanState.Unknown;
    //    await identityStorage.SetDefaultEncryptionIdentity(LogOnIdentity.Empty);
    //    New<IInternetState>().Clear();
    //    New<ICache>().RemoveItem(CacheKey.RootKey);
    //    RecentFilesProvider recentFilesProvider = new RecentFilesProvider();
    //    await recentFilesProvider.PurgeActiveFilesAsync();
    //    INavigationService navigationService = New<INavigationService>();
    //    await navigationService.PushAsync(typeof(RootAuthenticationViewModel));
    //    while (navigationService.NavigationStack.Count > 1)
    //    {
    //        navigationService.RemoveFromStack(navigationService.NavigationStack[0]);
    //    }
    //}

    //public async Task<bool> RedirectToPurchasePageAsync(LogOnIdentity logOnIdentity)
    //{
    //    PlanInformation pi = await PlanInformation.CreateAsync(logOnIdentity);
    //    PlanState = pi.PlanState;
    //    CanTryPremiumSubscription = pi.CanTryPremiumSubscription;
    //    if (PlanState == PlanState.HasPremium)
    //    {
    //        return true;
    //    }
    //    if (PlanState == PlanState.HasBusiness)
    //    {
    //        await AddMasterKeyInfo(logOnIdentity);
    //        return true;
    //    }
    //    ShowInAppPurchasePage(logOnIdentity);
    //    return false;
    //}

    public async Task AddMasterKeyInfo(LogOnIdentity identity)
    {
        IAccountService accountService = New<LogOnIdentity, IAccountService>(identity);
        UserAccount userAccount = await accountService.AccountAsync().Free();
        if (userAccount.MasterKeyPair != null)
        {
            identity.GroupMasterKeyPairs = userAccount.GroupMasterKeyPairs;
        }
    }

    //private async void ShowInAppPurchasePage(LogOnIdentity identity)
    //{
    //    if (New<AxCryptOnlineState>().IsOffline)
    //    {
    //        await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, Texts.NoInternetErrorMessage);
    //        return;
    //    }
    //    PurchaseSettings inAppPurchaseSettings;
    //    Dictionary<string, object> purchasePageData = new Dictionary<string, object>();
    //    using (await New<IProgressDialog>().Show(Texts.ProgressIndicatorWaitMessage, Texts.ProgressIndicatorWaitMessage))
    //    {
    //        IAccountService service = New<LogOnIdentity, IAccountService>(identity);
    //        AccountStorage accountStorage = new AccountStorage(service);
    //        inAppPurchaseSettings = await accountStorage.GetInAppPurchaseSettingsAsync();
    //        purchasePageData.Add(nameof(LogOnIdentity), identity);
    //        purchasePageData.Add(nameof(PurchaseSettings), inAppPurchaseSettings);
    //        purchasePageData.Add(nameof(PlanState), PlanState);
    //        purchasePageData.Add(nameof(PlanInformation.CanTryPremiumSubscription), CanTryPremiumSubscription);
    //    }
    //    if (!inAppPurchaseSettings.PremiumProductIdList.Any())
    //    {
    //        await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, "There is no product(s) found!");
    //        return;
    //    }
    //    await New<INavigationService>().PushAndRemoveCurrentEntry(typeof(PurchaseViewModel), purchasePageData);
    //}

    //public async void RedirectToSignInAsync()
    //{
    //    if (New<KnownIdentities>().IsLoggedOn)
    //    {
    //        SignOut();
    //        return;
    //    }
    //    if (New<INavigationService>().CurrentEntry.Key != typeof(RootAuthenticationViewModel))
    //    {
    //        await New<INavigationService>().PushAndRemoveCurrentEntry(typeof(RootAuthenticationViewModel));
    //    }
    //}

    private void OpenGetStartedPage()
    {
        New<Abstractions.IBrowser>().OpenUri(new Uri("https://axcrypt.net/information/guides/get-started/mobile"));
    }
}