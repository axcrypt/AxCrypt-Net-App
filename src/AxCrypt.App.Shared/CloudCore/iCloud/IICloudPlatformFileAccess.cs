using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AxCrypt.App.Shared.UI.ViewModels;

namespace AxCrypt.App.Shared.CloudCore.iCloud;

public interface IICloudPlatformFileAccess
{
    string GetDocumentsContainerPath();

    Task EnsureFileDownloadedAsync(string filePath);

    Task ShareFileAsync(string filePath, string message);

    bool IsAvailable { get; }

    Task<List<FilePickerItemViewModel>> GetItemsAsync(string folderPath);

    Task<List<FilePickerItemViewModel>> SearchAsync(string query, string rootPath);

    Task<bool> WaitForFileAvailabilityAsync(string filePath, int retries = 20);

    Task<string?> SafeReplaceFileAsync(string originalPath, string tempUploadedPath);

    Task<List<FilePickerItemViewModel>>GetSharedByMeItemsAsync();

    Task<List<FilePickerItemViewModel>>GetSharedWithMeItemsAsync();
}
