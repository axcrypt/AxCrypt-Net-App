using System.Threading.Tasks;
using AxCrypt.Core.IO;

namespace AxCrypt.App.Shared.Desktop.Services.Interface
{
    public interface IFilePicker
    {
        Task<IDataStore> ChooseFileAsync();

        Task<IDataStore> ChooseFileAsync(FilePickerParameters parameters);
    }

    public class FilePickerParameters
    {
        public FilePickerFilter Filter { get; set; }

        public object DisplayngAnchorView { get; set; }
    }

    public enum FilePickerFilter
    {
        AllFiles,
        AxCryptFiles,
    }
}