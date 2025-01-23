namespace AxCrypt.App.Shared.Services.Interface;

public interface IExportKeyManagementFile
{
    Task<string> ShowSaveFileDialogAsync(string title, string defaultExt, string filter, string fileName);

    Task ExportToFileAsync(string filePath, string data);
}