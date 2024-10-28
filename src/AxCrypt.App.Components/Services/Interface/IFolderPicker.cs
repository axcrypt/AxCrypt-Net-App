using AxCrypt.Core.UI;

namespace AxCrypt.App.Components.Services.Interface;

public interface IFolderPicker
{
    Task<string> PickFolderAsync();

    Task<IEnumerable<FileResult>> PickMultipleAsync(string folderPath, FileSelectionEventArgs e);
}