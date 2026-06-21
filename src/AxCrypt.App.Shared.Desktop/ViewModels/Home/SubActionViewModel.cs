using AxCrypt.Api.Model;
using AxCrypt.App.Entitlement.Services;
using AxCrypt.App.Shared.CloudCore.CloudFileProviderUtility;
using AxCrypt.App.Shared.Desktop.ViewModels.FileBrowser;
using AxCrypt.App.Shared.Desktop.ViewModels.Main;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.App.Shared.ViewModels.FileBrowser;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.ViewModels.Home
{
    public class SubActionViewModel : ViewModelBase
    {
        private FileOperationViewModel _fileOperationViewModel;
        private MainViewModel? _mainViewModel;
        private IStatusAlertService _statusAlertService;

        public FileProviderSelectionViewModel FileProviderViewModel;
        public bool HasEncryptionCapability { get; set; }

        public SubActionViewModel()
        {
            LogOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
            _mainViewModel = LogOnViewModel.MainViewModel;
            _fileOperationViewModel = LogOnViewModel.FileOperationViewModel;
            _statusAlertService = AxCServiceProviderExtension.StatusAlertService!;

            FileProviderViewModel = AxCServiceProvider.GetService<FileProviderSelectionViewModel>();
            Initialized();
        }

        public void Initialized()
        {
            _mainViewModel!.BindPropertyChanged(nameof(_mainViewModel.License), (LicenseCapabilities license) => { if (_mainViewModel.LoggedOn) ConfigureMenusAccordingToPolicy(license); });

            FileProviderViewModel.UpdateFileProviderSelection(FileOperationOption.None, InitializeProviderFileSelection);
        }

        private async Task InitializeProviderFileSelection()
        {
            if (FileProviderViewModel.SelectedFileProvider == null)
            {
                return;
            }

            FilePickerViewModel filePickerViewModel = AxCServiceProvider.GetService<DesktopFilePickerViewModel>();
            await CloudFileProviderHelper.Initialize(FileProviderViewModel.SelectedFileProvider.Value, filePickerViewModel, FileProviderViewModel.SelectedFileOperation, HasEncryptionCapability);
        }

        public LogOnViewModel LogOnViewModel { get; set; }

        public bool EnableCloudServices { get; set; }

        public bool EnableVault { get; set; }

        public bool EnableRandomRename { get; set; }

        public bool EnableSecureWipeFiles { get; set; }

        public bool EnableEncryptionUpgrade { get; set; }

        public bool EnableInviteUser { get; set; }

        public bool EnableAlwaysOffline { get; set; }

        public string? DisabledBackColor { get; set; }

        public SubscriptionLevel SubscriptionLevel
        {
            get
            {
                return LogOnViewModel.SubscriptionLevel;
            }
        }

        private void ConfigureMenusAccordingToPolicy(LicenseCapabilities license)
        {
            ConfigureCloudService(license);
            ConfigureVault(license);
            ConfigureAnonymousRename(license);
            ConfigureSecureWipe(license);
            ConfigureStrongEncryption(license);

            UpdateViewState();
        }

        private void ConfigureAnonymousRename(LicenseCapabilities license)
        {
            if (license.Has(LicenseCapability.RandomRename))
            {
                EnableRandomRename = true;
                EnableAlwaysOffline = false;
            }
            else
            {
                EnableRandomRename = false;
                EnableAlwaysOffline = false;
            }
        }

        private void ConfigureSecureWipe(LicenseCapabilities license)
        {
            if (license.Has(LicenseCapability.SecureWipe))
            {
                EnableSecureWipeFiles = true;
                EnableInviteUser = false;
            }
            else
            {
                EnableSecureWipeFiles = false;
                EnableInviteUser = true;
            }
        }

        private void ConfigureStrongEncryption(LicenseCapabilities license)
        {
            if (license.Has(LicenseCapability.StrongerEncryption))
            {
                EnableEncryptionUpgrade = true;
            }
            else
            {
                EnableEncryptionUpgrade = false;
            }
        }

        private void ConfigureCloudService(LicenseCapabilities license)
        {
            if (license.Has(LicenseCapability.CloudStorageAwareness))
            {
                EnableCloudServices = true;
            }
            else
            {
                EnableCloudServices = false;
            }
        }

        private void ConfigureVault(LicenseCapabilities license)
        {
            if (license.Has(LicenseCapability.Vault))
            {
                EnableVault = true;
            }
            else
            {
                EnableVault = false;
            }
        }

        public void OnCloudServiceButtonClick(KnownFolder knownFolder)
        {
            //await _fileOperationViewModel.OpenFilesFromFolder.ExecuteAsync(knownFolder.My.FullName);
            if (!Directory.Exists(knownFolder.My.FullName))
            {
                _statusAlertService.Error("Folder is not exist");
                return;
            }

            //We are opening the Explorer not the File picker with default folder for Cloud services
            _mainViewModel!.SelectedWatchedFolders = new List<string> { knownFolder.My.FullName };
            _mainViewModel.OpenSelectedFolder.Execute(_mainViewModel.SelectedWatchedFolders.First());
        }

        public async void RandomRenameAsync(EventArgs e)
        {
            await PremiumFeature_ClickAsync(LicenseCapability.RandomRename, async (ss, ee) =>
            {
                IEnumerable<string> preSelected = _mainViewModel?.SelectedRecentFiles ?? Enumerable.Empty<string>();

                IEnumerable<string> filesToRename;
                if (preSelected.Any())
                {
                    filesToRename = preSelected;
                }
                else
                {
                    FileSelectionEventArgs args = new FileSelectionEventArgs(Enumerable.Empty<string>())
                    {
                        FileSelectionType = FileSelectionType.Rename,
                    };
                    await New<IDataItemSelection>().HandleSelection(args);

                    if (args.Cancel || !args.SelectedFiles.Any())
                    {
                        return; // user cancelled — no toast
                    }
                    filesToRename = args.SelectedFiles;
                }

                try
                {
                    // ExecuteAsync fires the batch-completion notification itself —
                    // calling _statusAlertService.Success() here would produce a
                    // second confirmation message (the same bug fixed for Recent Files).
                    await _fileOperationViewModel.RandomRenameFiles.ExecuteAsync(filesToRename);
                }
                catch (Exception ex)
                {
                    _statusAlertService.Error($"Anonymous rename failed: {ex.Message}");
                }
            }, null!, e);
        }

        public async void RestoreOriginalNamesAsync(EventArgs e)
        {
            await PremiumFeature_ClickAsync(LicenseCapability.RandomRename, async (ss, ee) =>
            {
                IEnumerable<string> preSelected = _mainViewModel?.SelectedRecentFiles ?? Enumerable.Empty<string>();

                IEnumerable<string> filesToRestore;
                if (preSelected.Any())
                {
                    filesToRestore = preSelected;
                }
                else
                {
                    FileSelectionEventArgs args = new FileSelectionEventArgs(Enumerable.Empty<string>())
                    {
                        FileSelectionType = FileSelectionType.Rename,
                    };
                    await New<IDataItemSelection>().HandleSelection(args);

                    if (args.Cancel || !args.SelectedFiles.Any())
                    {
                        return; // user cancelled — no toast
                    }
                    filesToRestore = args.SelectedFiles;
                }

                try
                {
                    // ExecuteAsync fires the batch-completion notification itself —
                    // calling _statusAlertService.Success() here would produce a
                    // second confirmation message (the same bug fixed for Recent Files).
                    await _fileOperationViewModel.RestoreRandomRenameFiles.ExecuteAsync(filesToRestore);
                }
                catch (Exception ex)
                {
                    _statusAlertService.Error($"Restore original names failed: {ex.Message}");
                }
            }, null!, e);
        }

        public async void SecureWipeFiles(EventArgs e)
        {
            await PremiumFeature_ClickAsync(LicenseCapability.SecureWipe, async (ss, ee) =>
            {
                // Resolve the file list — either from a pre-existing selection or by
                // opening the file picker ourselves. Owning the picker call lets us
                // detect a cancel and skip both the success toast and usage recording.
                IEnumerable<string> preSelected = _mainViewModel?.SelectedRecentFiles ?? Enumerable.Empty<string>();

                IEnumerable<string> filesToWipe;
                if (preSelected.Any())
                {
                    filesToWipe = preSelected;
                }
                else
                {
                    FileSelectionEventArgs args = new FileSelectionEventArgs(Enumerable.Empty<string>())
                    {
                        FileSelectionType = FileSelectionType.Wipe,
                    };
                    await New<IDataItemSelection>().HandleSelection(args);

                    if (args.Cancel || !args.SelectedFiles.Any())
                    {
                        return; // user cancelled picker — no toast, no usage charge
                    }
                    filesToWipe = args.SelectedFiles;
                }

                int count = filesToWipe.Count();

                try
                {
                    await _fileOperationViewModel.WipeFiles.ExecuteAsync(filesToWipe);

                    // Guard the toast and usage recording behind the confirmation
                    // dialog result. WipeFiles.ExecuteAsync triggers the confirm
                    // dialog internally (HandleWipeConfirm) and returns normally
                    // regardless of the user's choice — we must read the VM to tell
                    // whether the user actually pressed Yes vs No / Cancel.
                    ConfirmWipeDialogViewModel confirmVm =
                        AxCServiceProvider.GetService<ConfirmWipeDialogViewModel>();

                    if (!confirmVm.OptedYes)
                    {
                        return; // user pressed No or Cancel — no toast, no usage charge
                    }

                    _statusAlertService.Success(count == 1
                        ? $"Securely deleted 1 file."
                        : $"Securely deleted {count} files.");

                    // Record usage only when the wipe actually completed.
                    await New<UserEntitlementService>().InsertUserUsageCount(
                        LimitedCapability.SecureWipe, LogOnViewModel.SubscriptionLevel);
                }
                catch (Exception ex)
                {
                    _statusAlertService.Error($"Secure delete failed: {ex.Message}");
                }
            }, null!, e);
        }

        public async void EncryptionUpgrade(EventArgs e)
        {
            await _fileOperationViewModel!.AsyncEncryptionUpgrade.ExecuteAsync(null!);
        }

        public void UpgradeDialog()
        {
            AxCServiceProviderExtension.UpgradeSubscriptionViewModel!.ShowUpgradeDialog();
        }

        public void AlwaysOfflineForFreeUser()
        {
            bool alwaysOnline = !New<UserSettings>().OfflineMode;
            New<UserSettings>().OfflineMode = alwaysOnline;
            New<AxCryptOnlineState>().IsOffline = alwaysOnline;

            string alert = alwaysOnline ? "Offline mode is enabled." : "Offline mode is disabled.";
            _statusAlertService.Success(alert);
            UpdateViewState();
        }

        public string SetInternetStateText()
        {
            return New<UserSettings>().OfflineMode ? "Switch to Online" : "Switch to Offline";
        }

        public async Task<bool> ValidateVaultPath()
        {
            string vaultfolder = New<UserSettings>().VaultEncryptDataPath;

            if (string.IsNullOrEmpty(vaultfolder))
            {
                AppSettingsViewModel appSettingsViewModel = AxCServiceProvider.GetService<AppSettingsViewModel>();
                appSettingsViewModel.ToggleVaultSettings();
                return false;
            }

            if (!New<IDataContainer>(vaultfolder).IsAvailable)
            {
                await New<IPopup>().ShowAsync(
                    PopupButtons.Ok,
                    Texts.WarningTitle,
                    Texts.VaultMisConfigured
                );
                return false;
            }

            return true;
        }

        public string GetIconClass(string displayName)
        {
            if (displayName.ToLower().Contains("onedrive"))
                return "onedrv-icon";
            if (displayName.ToLower().Contains("com~apple~clouddocs"))
                return "cld-icon";
            if (displayName.ToLower().Contains("google drive") || displayName.ToLower().Contains("my drive"))
                return "ggldrv-icon";
            if (displayName.ToLower().Contains("dropbox"))
                return "drpbx-icon";

            return "default-icon";
        }

        private async Task PremiumFeature_ClickAsync(LicenseCapability requiredCapability, Func<object, EventArgs, Task> realHandler, object sender, EventArgs e)
        {
            if (LogOnViewModel.UserHas(requiredCapability))
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
}