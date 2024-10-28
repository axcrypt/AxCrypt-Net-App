using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.App.Components.Services.Interface
{
    public interface IExportKeyManagementFile
    {
        Task<string> ShowSaveFileDialogAsync(string title, string defaultExt, string filter, string fileName);

        Task ExportToFileAsync(string filePath, string data);
    }
}