using AxCrypt.Api.Model;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.Core;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI.ViewModel;
using System;
using AxCrypt.App.Shared.Helpers;
using System.Linq;
using System.Threading.Tasks;
using AxCrypt.App.Shared.ViewModels;

namespace AxCrypt.App.Shared.Desktop.ViewModels.Home;

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
        _mainViewModel!.BindPropertyChanged(nameof(_mainViewModel.License), (LicenseCapabilities license) => { ConfigureMenusAccordingToPolicyAsync(license); });
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.FilesArePending), (bool areFilesPending) => { IsFilesPending = areFilesPending; UpdateViewState(); });
    }

    public bool IsFilesPending { get; set; }

    public bool EncryptButtonEnabled
    {
        get
        {
            if (LogOnViewModel.License.Has(LicenseCapability.EncryptNewFiles))
            {
                return true;
            }

            return _mainViewModel?.EncryptFileEnabled ?? false;
        }
    }

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

    public async Task SecureFile(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.EncryptNewFiles, async (ss, ee) => { await _fileOperationViewModel.EncryptFiles.ExecuteAsync(null); }, null!, e);
    }

    public async Task StopSecuringFile()
    {
        await _fileOperationViewModel.DecryptFiles.ExecuteAsync(_mainViewModel!.SelectedRecentFiles.Any() ? _mainViewModel!.SelectedRecentFiles : null!);
    }

    public async Task ShareKeysAsync(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, async (ss, ee) => { await ShareKeyService.ShareKeysWithFileSelectionAsync(_sharekeyViewModel!, _mainViewModel!.SelectedRecentFiles, _fileOperationViewModel); }, null!, e);
    }

    public async Task CleanAndRemoveOpenFilesButton_Click(EventArgs e)
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
            await new ApplicationManager().WaitForBackgroundToCompleteAsync();
            await _mainViewModel.EncryptPendingFiles.ExecuteAsync(null!);
            await new ApplicationManager().WaitForBackgroundToCompleteAsync();
        }

        UpdateViewState();
    }

    public void UpgradeDialog()
    {
        AxCServiceProviderExtension.UpgradeSubscriptionViewModel!.ShowUpgradeDialog();
    }

    public void NavigateToBusinessRenewalPage()
    {
        BrowseUtility.RedirectToAccountWebUrl("{0}Business/SubscriptionDetails#renew-bus-section");
    }

    public void NavigateToBusinessTopupPage()
    {
        BrowseUtility.RedirectToAccountWebUrl("{0}Business/SubscriptionDetails#addmorelicns-bus-section");
    }

    public void NavigateToMasterKeyPage()
    {
        BrowseUtility.RedirectToAccountWebUrl("{0}MasterKey/Index");
    }

    public void NavigateToCreateGroupsPage()
    {
        BrowseUtility.RedirectToAccountWebUrl("{0}Group/Index");
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

        UpgradeDialog();
    }
}