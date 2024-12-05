using AxCrypt.App.Components.Models;
using AxCrypt.App.Windows.ViewModels;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.IO;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.Services;

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

        if (fileSelectionArgs.Cancel)
        {
            return;
        }

        await ShareKeysAsync(fileSelectionArgs.SelectedFiles, sharekeyViewModel, fileOperationViewModel);
    }

    public static async Task ShareKeysAsync(IEnumerable<string> fileNames, ShareKeyViewModel sharekeyViewModel, FileOperationViewModel fileOperationViewModel)
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
        sharekeyViewModel!.SetSelectedFilesOrFolders(encryptedFileNames, viewModel);

        if (encryptableFileNames != null && encryptableFileNames.Any())
        {
            fileOperationViewModel.Recipients = viewModel.SharedWith;
            await fileOperationViewModel.EncryptFiles.ExecuteAsync(encryptableFileNames);
            fileOperationViewModel.Recipients = null;
        }

        await viewModel.ShareFiles.ExecuteAsync(null);
    }
}