using AxCrypt.App.Components.Services.Interface;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using System.Runtime.InteropServices;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.Services;

public class FolderPickerWindows : IFolderPicker
{
    [DllImport("Microsoft.UI.Xaml.dll", ExactSpelling = true, PreserveSig = false)]
    private static extern IntPtr GetWindowHandle(IntPtr hwnd);

    public async Task<string> PickFolderAsync()
    {
        try
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");
            folderPicker.ViewMode = PickerViewMode.Thumbnail;

            nint hwnd = ((MauiWinUIWindow)MauiWinUIApplication.Current.Application.Windows[0].Handler.PlatformView).WindowHandle;
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            StorageFolder folders = await folderPicker.PickSingleFolderAsync();
            return folders?.Path;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task<IEnumerable<FileResult>> PickMultipleAsync(string initialDirectory, FileSelectionEventArgs e)
    {
        try
        {
            nint hwnd = ((MauiWinUIWindow)Application.Current.Windows[0].Handler.PlatformView).WindowHandle;
            FolderPicker folderPicker = new FolderPicker();
            InitializeWithWindow.Initialize(folderPicker, hwnd);

            folderPicker.FileTypeFilter.Add(".axx");

            StorageFolder initialFolder = await StorageFolder.GetFolderFromPathAsync(initialDirectory);
            folderPicker.SettingsIdentifier = initialFolder.Path;

            StorageFolder pickedFolder = await folderPicker.PickSingleFolderAsync();
            if (pickedFolder == null)
            {
                return Enumerable.Empty<FileResult>();
            }

            FileOpenPicker filePicker = new FileOpenPicker();
            InitializeWithWindow.Initialize(filePicker, hwnd);
            filePicker.SuggestedStartLocation = PickerLocationId.Desktop;

            IDictionary<DevicePlatform, IEnumerable<string>> fileTypes = GetFileTypesForSelectionType(e.FileSelectionType);
            foreach (string fileType in fileTypes[DevicePlatform.WinUI])
            {
                filePicker.FileTypeFilter.Add(fileType);
            }

            string folderToken = StorageApplicationPermissions.FutureAccessList.Add(pickedFolder);
            filePicker.SettingsIdentifier = folderToken;

            IReadOnlyList<StorageFile> files = await filePicker.PickMultipleFilesAsync();

            return files?.Select(file => new FileResult(file.Path)) ?? Enumerable.Empty<FileResult>();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public static IDictionary<DevicePlatform, IEnumerable<string>> GetFileTypesForSelectionType(FileSelectionType selectionType)
    {
        Dictionary<DevicePlatform, IEnumerable<string>> fileTypes = new Dictionary<DevicePlatform, IEnumerable<string>>();
        IRuntimeEnvironment runtimeEnvironment = New<IRuntimeEnvironment>();

        switch (selectionType)
        {
            case FileSelectionType.Open:
            case FileSelectionType.Decrypt:
            case FileSelectionType.KeySharing:
            case FileSelectionType.KeySharingEncrypt:
                fileTypes.Add(DevicePlatform.WinUI, new[] { runtimeEnvironment.AxCryptExtension });
                fileTypes.Add(DevicePlatform.MacCatalyst, new string[] { });
                fileTypes.Add(DevicePlatform.macOS, new string[] { });
                fileTypes.Add(DevicePlatform.iOS, new string[] { });
                fileTypes.Add(DevicePlatform.Android, new string[] { });
                break;

            case FileSelectionType.Encrypt:
            case FileSelectionType.Rename:
            case FileSelectionType.Wipe:
                fileTypes.Add(DevicePlatform.WinUI, new string[] { });
                fileTypes.Add(DevicePlatform.MacCatalyst, new string[] { });
                fileTypes.Add(DevicePlatform.macOS, new string[] { });
                fileTypes.Add(DevicePlatform.iOS, new string[] { });
                fileTypes.Add(DevicePlatform.Android, new string[] { });
                break;

            case FileSelectionType.ImportPublicKeys:
            case FileSelectionType.ImportPrivateKeys:
                fileTypes.Add(DevicePlatform.WinUI, new[] { ".txt", "." + runtimeEnvironment.AxCryptExtension });
                fileTypes.Add(DevicePlatform.MacCatalyst, new string[] { });
                fileTypes.Add(DevicePlatform.macOS, new string[] { });
                fileTypes.Add(DevicePlatform.iOS, new string[] { });
                fileTypes.Add(DevicePlatform.Android, new string[] { });
                break;

            default:
                throw new NotImplementedException("File selection type not supported.");
        }

        return fileTypes;
    }
}
