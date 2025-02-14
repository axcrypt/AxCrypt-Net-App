using AxCrypt.Core.UI;
using Microsoft.Maui.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AxCrypt.App.Desktop.Services.Interface;

public interface IFolderPicker
{
    Task<string> PickFolderAsync();

    Task<IEnumerable<FileResult>> PickMultipleAsync(string folderPath, FileSelectionEventArgs e);
}