using AxCrypt.App.Shared.Providers;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using Microsoft.Graph.Models.CallRecords;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.ViewModels;

public class FileProviderSelectionViewModel : ViewModelBase
{
    private bool isVisible = false;

    public bool IsVisible
    {
        get { return isVisible; }
        set
        {
            isVisible = value;
            UpdateViewState();
        }
    }

    public FileOperationOption SelectedFileOperation { get; set; } = FileOperationOption.None;

    public FileProviderItem? SelectedFileProvider { get; set; }

    public IList<FileProviderItem> EncryptableFileProviders { get; set; } = [];

    private Func<Task> _selectedProviderAction;

    public void ShowFileProviderSelectionPopup(FileOperationOption fileOperation, Func<Task> selectedProviderAction)
    {
        _selectedProviderAction = selectedProviderAction;
        SelectedFileOperation = fileOperation;
        InitializeFileProviders();
        IsVisible = true;
    }

    public void UpdateFileProviderSelection(FileOperationOption fileOperation, Func<Task> selectedProviderAction)
    {
        _selectedProviderAction = selectedProviderAction;
        SelectedFileOperation = fileOperation;
        InitializeFileProviders();
    }

    private void InitializeFileProviders()
    {
        EncryptableFileProviders = [];
        if (SelectedFileOperation == FileOperationOption.OpenSecured)
        {
            EncryptableFileProviders.Add(
                new FileProviderItem(
                    "Your phone",
                    AxCrypt.Core.IO.FileProvider.PhoneBrowser,
                    "phn-icon"
                )
            );
        }
        if (
            New<ICloudDriveConfiguration>().CurrentDeviceCategory == DeviceCategory.Android
            && SelectedFileOperation != FileOperationOption.OpenSecured
        )
        {
            EncryptableFileProviders.Add(
                new FileProviderItem(
                    Texts.PhoneLabel,
                    AxCrypt.Core.IO.FileProvider.Local,
                    "phn-icon"
                )
            );
        }

        EncryptableFileProviders.Add(
            new FileProviderItem(
                Texts.KnownFolderNameGoogleDrive,
                AxCrypt.Core.IO.FileProvider.GoogleDrive,
                "ggldrv-icon"
            )
        );
        EncryptableFileProviders.Add(
            new FileProviderItem(
                Texts.KnownFolderNameDropbox,
                AxCrypt.Core.IO.FileProvider.DropBox,
                "drpbx-icon"
            )
        );
        EncryptableFileProviders.Add(
            new FileProviderItem(
                Texts.KnownFolderNameOneDrive,
                AxCrypt.Core.IO.FileProvider.OneDrive,
                "onedrv-icon"
            )
        );

        if (OperatingSystem.IsMacCatalyst() || OperatingSystem.IsMacOS())
        {
            EncryptableFileProviders.Add(
                new FileProviderItem(
                    Texts.KnownFolderNameICloud,
                    AxCrypt.Core.IO.FileProvider.iCloud,
                    "cld-icon",
                    New<FileProvidersUserAccessInfo>().iCloudAccessInfo?.Any() ?? false
                )
            );
        }
    }

    public async Task SubActionSelectProvider(FileProviderItem provider)
    {
        if (New<AxCryptOnlineState>().IsOffline)
        {
            await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.AlertText, Texts.OfflineInternetRequiredText);
            return;
        }

        SelectedFileOperation = FileOperationOption.None;
        SelectedFileProvider = provider;
        IsVisible = false;

        await _selectedProviderAction();
    }

    public async Task SelectProvider(FileProviderItem provider)
    {
        SelectedFileProvider = provider;
        IsVisible = false;

        await _selectedProviderAction();
    }
}