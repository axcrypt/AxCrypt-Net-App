using AxCrypt.App.Entitlement.Contracts;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.IO;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Services;

public static class ShareKeyService
{
    public static async Task<bool> ShareKeysWithFileSelectionAsync(ShareKeyViewModel sharekeyViewModel, IEnumerable<string> selectedRecentFileNames, FileOperationViewModel fileOperationViewModel)
    {
        FileSelectionEventArgs fileSelectionArgs = new FileSelectionEventArgs(selectedRecentFileNames)
        {
            FileSelectionType = FileSelectionType.KeySharingEncrypt,
        };

        if (!fileSelectionArgs.SelectedFiles.Any())
        {
            await New<IDataItemSelection>().HandleSelection(fileSelectionArgs);
        }

        IFeatureUsageProvider? usage = AxCServiceProviderExtension.GetService<IFeatureUsageProvider>();
        int availableCount = usage.GetUsage(FeatureKey.KeyShare).Remaining;
        if (availableCount == 0)
        {
            CloseShareKeyDialogIfOpen(sharekeyViewModel);
            return false;
        }

        // Cancelled picker OR empty selection — the caller (ActionsViewModel)
        // opened the ShareKey dialog optimistically before the picker, so the
        // shell is showing right now with no files. Close it so the user
        // isn't left staring at an empty popup that does nothing.
        if (fileSelectionArgs.Cancel || !fileSelectionArgs.SelectedFiles.Any())
        {
            CloseShareKeyDialogIfOpen(sharekeyViewModel);
            return false;
        }

        return await ShareKeysAsync(fileSelectionArgs.SelectedFiles, sharekeyViewModel, fileOperationViewModel);
    }

    /// <summary>
    /// Idempotent dialog-close used by every early-exit path. The optimistic
    /// Show() in ActionsViewModel / RecentFilesViewModel means the dialog
    /// shell can be on screen with no data; this helper makes sure it goes
    /// away on every abort.
    /// </summary>
    private static void CloseShareKeyDialogIfOpen(ShareKeyViewModel sharekeyViewModel)
    {
        try
        {
            sharekeyViewModel?.LogOnViewModel?.ShareKeyDialog?.Close();
            if (sharekeyViewModel != null)
            {
                sharekeyViewModel.SelectedFilesOrFolders = new List<string>();
                sharekeyViewModel.PageResult = DialogResult.Cancel;
            }
        }
        catch
        {
            // Closing a not-open dialog must never throw.
        }
    }

    public static async Task<bool> ShareKeysAsync(IEnumerable<string> fileNames, ShareKeyViewModel sharekeyViewModel, FileOperationViewModel fileOperationViewModel)
    {
        try
        {
            IEnumerable<string> encryptableFileNames = fileNames.Where(f => New<IDataStore>(f).IsEncryptable());
            if (encryptableFileNames != null && encryptableFileNames.Any())
            {
                PopupButtons click = await New<IPopup>().ShowAsync(PopupButtons.OkCancel, Texts.InformationTitle, "There are some unencrypted files also selected for key sharing. AxCrypt will encrypt and then key share the selected files. Would you like to continue to proceed?");
                if (click != PopupButtons.Ok)
                {
                    return false;
                }
            }

            IEnumerable<string> encryptedFileNames = fileNames.Where(f => New<IDataStore>(f).IsEncrypted());
            SharingListViewModel viewModel = await SharingListViewModel.CreateForFilesAsync(encryptedFileNames, Resolve.KnownIdentities.DefaultEncryptionIdentity);
            await sharekeyViewModel!.SetSelectedFilesOrFolders(fileNames, viewModel);

            if (sharekeyViewModel.PageResult == DialogResult.Cancel)
            {
                return false;
            }

            if (encryptableFileNames != null && encryptableFileNames.Any())
            {
                IFeatureUsageProvider? usage = AxCServiceProviderExtension.GetService<IFeatureUsageProvider>();
                fileOperationViewModel.Recipients = viewModel.SharedWith;
                FeatureUsage featureUsage = usage.GetUsage(FeatureKey.KeyShare);
                int availableCount = featureUsage.Remaining;
                if (availableCount == 0)
                {
                    return false;
                }

                if (featureUsage.Limit > 0)
                {
                    encryptableFileNames = encryptableFileNames.Take(availableCount);
                    fileOperationViewModel.Recipients = viewModel.SharedWith.Take(availableCount);
                }

                await fileOperationViewModel.EncryptFiles.ExecuteAsync(encryptableFileNames);
                fileOperationViewModel.Recipients = null;
                return true;
            }

            await viewModel.ShareFiles.ExecuteAsync(null);
            return true;
        }
        catch (Exception ex)
        {
            await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, ex.Message);
            return false;
        }
    }
}