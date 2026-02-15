using AxCrypt.App.Shared.CloudCore;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.UI.ViewModels;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.App.Shared.ViewModels.FileBrowser;
using AxCrypt.Content;
using AxCrypt.Core.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.ViewModels.FileBrowser;

public class DesktopFilePickerViewModel(
    ShareKeyViewModel shareKeyViewModel,
    FileProviderSelectionViewModel fileProviderSelectionViewModel,
    ICustomNavigationService navigationManager
) : FilePickerViewModel(shareKeyViewModel, fileProviderSelectionViewModel, navigationManager)
{
    protected override async Task RedirectToMainScreen(IEnumerable<FilePickerItemViewModel> fileItems)
    {
        try
        {
            IsVisible = false;

            IDictionary<string, object> selectedFileDictionary = new Dictionary<string, object>
                {
                    { nameof(FilePickerItemViewModel), fileItems },
                    { nameof(FileStorageProvider), _fileProviderService }
                };

            SecureFilesViewModel secretFilesViewModel = new SecureFilesViewModel(
                _hasPaidSubscription,
                selectedFileDictionary,
                _navigationManager
            );

            await secretFilesViewModel.TriggerFileOperationProcess();
        }
        catch (Exception ex)
        {
            await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, ex.Message);
            UpdateViewState();
        }
    }
}