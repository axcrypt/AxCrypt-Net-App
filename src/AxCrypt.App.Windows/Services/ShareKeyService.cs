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
    private static FileOperationViewModel FileOperationViewModel
    {
        get
        {
            return New<FileOperationViewModel>();
        }
    }

    public static async Task ShareKeysWithFileSelectionAsync(IEnumerable<string> selectedRecentFileNames)
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

        await ShareKeysAsync(fileSelectionArgs.SelectedFiles);
    }

    private static async Task ShareKeysAsync(IEnumerable<string> fileNames)
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
        ShareKeyViewModel shareKeyViewModel = new ShareKeyViewModel(new AxCrypt.App.Components.Models.LogOnViewModel(new AxCrypt.App.Components.Services.ProcessIndicatorService())); ;
        shareKeyViewModel.SetSelectedFilesOrFolders(fileNames, viewModel);

        if (encryptableFileNames != null && encryptableFileNames.Any())
        {
            FileOperationViewModel.Recipients = viewModel.SharedWith;
            await FileOperationViewModel.EncryptFiles.ExecuteAsync(encryptableFileNames);
            FileOperationViewModel.Recipients = null;
        }

        await viewModel.ShareFiles.ExecuteAsync(null);
    }
}