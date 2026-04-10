using AxCrypt.App.Entitlement.Contracts;
using AxCrypt.App.Entitlement.Services;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Services;

public static class ShareKeyService
{
    public static async Task ShareKeysWithFileSelectionAsync(ShareKeyViewModel sharekeyViewModel, IEnumerable<string> selectedRecentFileNames, FileOperationViewModel fileOperationViewModel)
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
        if (availableCount <= 0)
        {
            return;
        }

        if (fileSelectionArgs.Cancel)
        {
            return;
        }

        await ShareKeysAsync(fileSelectionArgs.SelectedFiles, sharekeyViewModel, fileOperationViewModel);
    }

    public static async Task ShareKeysAsync(IEnumerable<string> fileNames, ShareKeyViewModel sharekeyViewModel, FileOperationViewModel fileOperationViewModel)
    {
        try
        {
            IEnumerable<string> encryptableFileNames = fileNames.Where(f => New<IDataStore>(f).IsEncryptable());
            if (encryptableFileNames != null && encryptableFileNames.Any())
            {
                PopupButtons click = await New<IPopup>().ShowAsync(PopupButtons.OkCancel, Texts.InformationTitle, "There are some unencrypted files also selected for key sharing. AxCrypt will encrypt and then key share the selected files. Would you like to continue to proceed?");
                if (click != PopupButtons.Ok)
                {
                    return;
                }
            }

            IEnumerable<string> encryptedFileNames = fileNames.Where(f => New<IDataStore>(f).IsEncrypted());
            SharingListViewModel viewModel = await SharingListViewModel.CreateForFilesAsync(encryptedFileNames, Resolve.KnownIdentities.DefaultEncryptionIdentity);
            await sharekeyViewModel!.SetSelectedFilesOrFolders(fileNames, viewModel);

            if (sharekeyViewModel.PageResult == DialogResult.Cancel)
            {
                return;
            }

            if (encryptableFileNames != null && encryptableFileNames.Any())
            {
                fileOperationViewModel.Recipients = viewModel.SharedWith;

                int availableCount = await New<UserEntitlementService>().GetRemainingCount(LimitedCapability.SecureFolders, New<AccountStatusViewModel>().SubscriptionLevel, encryptableFileNames.Count());
                if (availableCount <= 0)
                {
                    return;
                }

                await fileOperationViewModel.EncryptFiles.ExecuteAsync(encryptableFileNames.Take(availableCount));
                fileOperationViewModel.Recipients = null;
            }

            await viewModel.ShareFiles.ExecuteAsync(null);
        }

        catch (Exception ex)
        {
            await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, ex.Message);
            return;
        }
    }
}