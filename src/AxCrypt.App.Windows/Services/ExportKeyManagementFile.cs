using AxCrypt.App.Components.Services.Interface;
using Windows.Storage.Pickers;
using Windows.Storage;
using AxCrypt.App.Windows.Services;

[assembly: Dependency(typeof(ExportKeyManagementFile))]
namespace AxCrypt.App.Windows.Services;

public class ExportKeyManagementFile : IExportKeyManagementFile
{
    public async Task<string> ShowSaveFileDialogAsync(string title, string defaultExt, string filter, string fileName)
    {
        FileSavePicker picker = new FileSavePicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeChoices.Add("AxCrypt Files", new List<string>() { defaultExt });
        picker.SuggestedFileName = fileName;

        nint hwnd = ((MauiWinUIWindow)Application.Current.Windows[0].Handler.PlatformView).WindowHandle;
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        StorageFile file = await picker.PickSaveFileAsync();
        return file?.Path;
    }

    public async Task ExportToFileAsync(string filePath, string data)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            await writer.WriteAsync(data);
        }
    }
}
