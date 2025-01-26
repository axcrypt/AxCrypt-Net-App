using AxCrypt.Core.UI;


namespace AxCrypt.App.Windows.Services.Interface;

public interface IFolderPicker
{
    Task<string> PickFolderAsync();

    Task<IEnumerable<FileResult>> PickMultipleAsync(string folderPath, FileSelectionEventArgs e);
}