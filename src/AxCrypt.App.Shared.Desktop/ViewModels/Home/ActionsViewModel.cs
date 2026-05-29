using AxCrypt.Api.Model;
using AxCrypt.App.Entitlement.Contracts;
using AxCrypt.App.Shared.Desktop.Services;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Core;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AxCrypt.App.Shared.Desktop.ViewModels.Home;

public class ActionsViewModel : ViewModelBase
{
    private FileOperationViewModel _fileOperationViewModel;
    private MainViewModel? _mainViewModel;

    private IStatusAlertService _statusAlertService;
    private ShareKeyViewModel? _sharekeyViewModel;
    private BatchFileOperationService _batchService;

    public ActionsViewModel(ShareKeyViewModel shareKeyViewModel, BatchFileOperationService batchService)
    {
        LogOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
        _statusAlertService = AxCServiceProviderExtension.StatusAlertService!;

        _mainViewModel = LogOnViewModel.MainViewModel;
        _fileOperationViewModel = LogOnViewModel.FileOperationViewModel;

        _sharekeyViewModel = shareKeyViewModel;
        _batchService = batchService;

        Initialized();
    }

    public void Initialized()
    {
        _mainViewModel!.BindPropertyChanged(nameof(_mainViewModel.License), (LicenseCapabilities license) => { ConfigureMenusAccordingToPolicyAsync(license); });
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.FilesArePending), (bool areFilesPending) => { AreFilesPending = areFilesPending; UpdateViewState(); });
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.FoldersArePending), (bool areFoldersPending) => { AreFoldersPending = areFoldersPending; UpdateViewState(); });
    }

    public bool AreFilesPending { get; set; }

    public bool AreFoldersPending { get; set; }

    public bool EncryptButtonEnabled
    {
        get
        {
            if (LogOnViewModel.UserHas(LicenseCapability.EncryptNewFiles))
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
        // Route through the batch service when there's a pre-selection so
        // failures on one file don't abort the rest, and so the
        // BatchOperationToast shows the same summary the other Quick
        // Actions produce. No selection → let Core open its picker.
        IEnumerable<string>? selected = _mainViewModel?.SelectedRecentFiles;
        if (selected != null && selected.Any())
        {
            await _batchService.RunAsync(
                selected,
                async (path) => await _fileOperationViewModel.OpenFiles.ExecuteAsync(new[] { path }),
                "Opened");
            return;
        }

        await _fileOperationViewModel.OpenFilesFromFolder.ExecuteAsync(string.Empty);
    }

    public async Task SecureFile(EventArgs e)
    {
        // Note: when invoked from the Quick Action tile with no pre-selection,
        // Core opens its own file picker (we pass null) and runs internally.
        // For the multi-select case (selection already exists), we batch each
        // file individually so one failure can't abort the rest.
        await PremiumFeature_ClickAsync(LicenseCapability.EncryptNewFiles, async (ss, ee) =>
        {
            await SecureFileAsync();
        }, null!, e);
    }

    public async Task SecureFileAsync()
    {
        IEnumerable<string>? selected = _mainViewModel?.SelectedRecentFiles;
        if (selected != null && selected.Any())
        {
            await _batchService.RunAsync(
                selected,
                async (path) => await _fileOperationViewModel.EncryptFiles.ExecuteAsync(new[] { path }),
                "Encrypted",
                FeatureKey.FileEncryption);
            return;
        }

        // No pre-selection: open the file picker ourselves so we know exactly
        // which files the user chose. This lets us route through RunAsync (same
        // as the pre-selection path) which meters only the files that actually
        // succeeded, and skips metering entirely on cancel.
        FileSelectionEventArgs args = new FileSelectionEventArgs(Enumerable.Empty<string>())
        {
            FileSelectionType = FileSelectionType.Encrypt,
        };
        await New<IDataItemSelection>().HandleSelection(args);

        if (args.Cancel || !args.SelectedFiles.Any())
        {
            return; // Picker cancelled — nothing encrypted, quota unchanged.
        }

        await _batchService.RunAsync(
            args.SelectedFiles,
            async (path) => await _fileOperationViewModel.EncryptFiles.ExecuteAsync(new[] { path }),
            "Encrypted",
            FeatureKey.FileEncryption);
    }

    /// <summary>
    /// Pull the latest entitlement counts from the API in the background.
    /// Used after encryption paths whose file count isn't known locally
    /// (Core's own file picker). Fire-and-forget — never blocks the UI.
    /// </summary>
    private static void ReconcileFreeTierUsage()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                IFeatureUsageProvider? usage = AxCServiceProviderExtension.GetService<IFeatureUsageProvider>();
                if (usage != null)
                {
                    await usage.RefreshAsync();
                }
            }
            catch
            {
                // Metering must never disrupt the encryption flow.
            }
        });
    }

    public async Task StopSecuringFile()
    {
        IEnumerable<string>? selected = _mainViewModel?.SelectedRecentFiles;
        if (selected != null && selected.Any())
        {
            await _batchService.RunAsync(
                selected,
                async (path) => await _fileOperationViewModel.DecryptFiles.ExecuteAsync(new[] { path }),
                "Decrypted");
            return;
        }

        // Fall-through: no selection → let Core ask the user via its picker.
        await _fileOperationViewModel.DecryptFiles.ExecuteAsync(null!);
    }

    public async Task ShareKeys(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, async (ss, ee) =>
        {
            await ShareKeysAsync(e);
        }, null!, e);
    }

    public async Task ShareKeysAsync(EventArgs e)
    {
        // Open the dialog shell immediately so the user sees it at once,
        // instead of waiting ~5 s for the service's pre-load API call.
        // The compiled service will fire OnDialogVisibilityChanged(true)
        // again when data is ready — that second call is idempotent.
        _sharekeyViewModel!.LogOnViewModel.ShareKeyDialog.Show();

        // One-shot share — the dialog and service handle the whole
        // selection internally and present their own success / failure UI.
        // No per-file iteration, no batch toast.
        bool shared = await ShareKeyService.ShareKeysWithFileSelectionAsync(
            _sharekeyViewModel!,
            _mainViewModel!.SelectedRecentFiles,
            _fileOperationViewModel);

        // Only record metered usage when key sharing actually completed —
        // cancelling the file picker or the share dialog must not consume quota.
        if (shared)
        {
            await _batchService.RecordMeteredUsageAsync(FeatureKey.KeyShare);
        }
        ReconcileFreeTierUsage();
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