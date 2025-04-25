using AxCrypt.Api.Model;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Service;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System;
using AxCrypt.App.Shared.Helpers;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.App.Shared.ViewModels;

namespace AxCrypt.App.Desktop.ViewModels.Home;

public class ActionsViewModel : ViewModelBase
{
    private FileOperationViewModel _fileOperationViewModel;
    private MainViewModel? _mainViewModel;

    private IStatusAlertService _statusAlertService;
    private ShareKeyViewModel? _sharekeyViewModel;

    public ActionsViewModel(ShareKeyViewModel shareKeyViewModel)
    {
        LogOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
        _statusAlertService = AxCServiceProviderExtension.StatusAlertService!;

        _mainViewModel = LogOnViewModel.MainViewModel;
        _fileOperationViewModel = LogOnViewModel.FileOperationViewModel;

        _sharekeyViewModel = shareKeyViewModel;

        Initialized();
    }

    public void Initialized()
    {
        LogOnViewModel.BindPropertyChanged(nameof(LogOnViewModel.License), (LicenseCapabilities license) => { ConfigureMenusAccordingToPolicyAsync(license); });

        _mainViewModel!.BindPropertyChanged(nameof(_mainViewModel.EncryptFileEnabled), (bool enabled) => { EncryptButtonEnabled = enabled; });
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.FilesArePending), (bool areFilesPending) => { IsFilesPending = areFilesPending; UpdateViewState(); });
    }

    public bool IsFilesPending { get; set; }

    public bool EncryptButtonEnabled { get; set; }

    public bool KeyShareButtonEnabled { get; set; }

    public bool HasBusiness { get; set; }

    public bool HasPremium { get; set; }

    public bool HasNoSubscription { get; set; }

    public LogOnViewModel LogOnViewModel { get; set; }

    public SubscriptionLevel SubscriptionLevel
    {
        get
        {
            return LogOnViewModel.SubscriptionLevel;
        }
    }

    public void OpenFeedbackPopup()
    {
        LogOnViewModel.FeedbackDialog.Show();
    }

    public async Task OpenFile()
    {
        await _fileOperationViewModel.OpenFilesFromFolder.ExecuteAsync(string.Empty);
    }

    public async Task SecureFile()
    {
        await _fileOperationViewModel.EncryptFiles.ExecuteAsync(null);
    }

    public async Task StopSecuringFile()
    {
        await _fileOperationViewModel.DecryptFiles.ExecuteAsync(_mainViewModel!.SelectedRecentFiles.Any() ? _mainViewModel!.SelectedRecentFiles : null!);
    }

    public async void ShareKeysAsync(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, async (ss, ee) => { await ShareKeyService.ShareKeysWithFileSelectionAsync(_sharekeyViewModel!, _mainViewModel!.SelectedRecentFiles, _fileOperationViewModel); }, null!, e);
    }

    public async void CleanAndRemoveOpenFilesButton_Click(EventArgs e)
    {
        await EncryptPendingFiles();
    }

    private void ConfigureMenusAccordingToPolicyAsync(LicenseCapabilities license)
    {
        ConfigureKeyShareMenus(license);
        ConfigureMenus(license);
    }

    private void ConfigureKeyShareMenus(LicenseCapabilities license)
    {
        if (license.Has(LicenseCapability.KeySharing))
        {
            KeyShareButtonEnabled = true;
        }
        else
        {
            KeyShareButtonEnabled = false;
        }

        UpdateViewState();
    }

    private void ConfigureMenus(LicenseCapabilities license)
    {
        HasBusiness = license.Has(LicenseCapability.Business);
        HasPremium = license.Has(LicenseCapability.Premium);
        HasNoSubscription = license.CryptoPolicy.Name == "Free";

        UpdateViewState();
    }

    private async Task EncryptPendingFiles()
    {
        if (_mainViewModel != null)
        {
            new ApplicationManager().WaitForBackgroundToComplete();
            await _mainViewModel.EncryptPendingFiles.ExecuteAsync(null!);
            new ApplicationManager().WaitForBackgroundToComplete();
        }

        UpdateViewState();
    }

    public void UpgradeDialog()
    {
        LogOnViewModel.UpgradeDialog.Show();
    }

    public async void RedirectToAccountWebUrl()
    {
        LogOnIdentity identity = New<KnownIdentities>().DefaultEncryptionIdentity;
        string tag = string.Empty;
        if (New<KnownIdentities>().IsLoggedOn)
        {
            IAccountService accountService = New<LogOnIdentity, IAccountService>(identity);
            tag = (await accountService.AccountAsync()).Tag ?? string.Empty;
        }

        BrowseUtility.RedirectToPurchasePage(identity.UserEmail.Address, true, tag);
    }

    public void RedirectToAccountSite()
    {
        New<Abstractions.IBrowser>().OpenUri(new Uri("https://account.axcrypt.net/"));
    }

    //public void BuyForSomeoneElseLink()
    //{
    //    //New<Abstractions.IBrowser>().OpenUri(new Uri("https://account.axcrypt.net/en/Premium/CreateSubscription"));
    //}

    //public void ChangeSubscriptionToBusinessLink()
    //{
    //    New<Abstractions.IBrowser>().OpenUri(new Uri("https://account.axcrypt.net/en/HomeBusiness/CreateSubscription"));
    //}

    private async Task PremiumFeature_ClickAsync(LicenseCapability requiredCapability, Func<object, EventArgs, Task> realHandler, object sender, EventArgs e)
    {
        if (_mainViewModel!.License.Has(requiredCapability))
        {
            if (realHandler != null)
            {
                await realHandler(sender, e);
            }
            return;
        }

        LogOnViewModel.UpgradeDialog.Show();
    }
}