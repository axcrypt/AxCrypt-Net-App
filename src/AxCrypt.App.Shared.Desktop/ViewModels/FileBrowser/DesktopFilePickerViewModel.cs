using AxCrypt.App.Shared.CloudCore;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.UI.ViewModels;
using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.App.Shared.ViewModels.FileBrowser;
using AxCrypt.Content;
using AxCrypt.Core.IO;
using AxCrypt.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
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

    private IEnumerable<string> _selectedFiles { get; set; }

    public async Task AddFiles()
    {
        await HandleFileSelection();
        if (!_selectedFiles.Any())
        {
            return;
        }

        FolderItem CurrentCloudFolder = OpenedFoldersList.LastOrDefault()!;

        string currentFolderId = CurrentCloudFolder == null ? "/" : CurrentCloudFolder.FileID + "/";

        foreach (string file in _selectedFiles)
        {
            IDataStore fileContainer = New<IDataStore>(file);
            FilePickerItemViewModel _files = new FilePickerItemViewModel()
            {
                FileID = fileContainer.FullName,
                FileName = fileContainer.Name,
                IsFolder = fileContainer.IsFolder,
                FileExtension = System.IO.Path.GetExtension(fileContainer.FullName),
                DestinationPath = currentFolderId.ToLower(),
                Source = FileProvider.Local,
            };

            SelectedFiles.Add(_files);
        }

        SelectedFileOperation = FileOperationOption.Encrypt;
        await ProcessSelectedFileItemActionAsync();
    }

    private async Task HandleFileSelection()
    {
        FileSelectionEventArgs eventArgs = new FileSelectionEventArgs([])
        {
            FileSelectionType = FileSelectionType.Encrypt
        };

        _selectedFiles = Enumerable.Empty<string>();
        await New<IDataItemSelection>().HandleSelection(eventArgs);
        if (eventArgs.SelectedFiles == null || !eventArgs.SelectedFiles.Any())
        {
            return;
        }

        _selectedFiles = eventArgs.SelectedFiles;
    }

    public string GetFileTypeIcon(string fileName)
    {
        string fileExtension = UIHelperUtility.GetExtention(fileName);
        fileExtension = fileExtension?.ToLowerInvariant() ?? "";

        string axcryptFileType = ".axx";
        if (fileExtension == axcryptFileType)
        {
            return "axcrypt-icon";
        }

        return fileName.GetIcon();
    }
}