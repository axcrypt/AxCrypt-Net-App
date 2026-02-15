using AxCrypt.Core.UI.ViewModel;

namespace AxCrypt.App.Shared.UI.ViewModels
{
    public class FolderItem : ViewModelBase
    {
        public string DirectoryName { get; set; }

        public string FileID { get; set; }

        public string ParentDirectory { get; set; }

        public string ParentDirectoryID { get; set; }
    }
}