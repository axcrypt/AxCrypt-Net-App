using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.Core.Runtime;

namespace AxCrypt.App.Components.Services
{
    public class FilePickerService
    {
        public event EventHandler<FileSelectionEventArgs> SelectingFiles;

        public FilePickerService()
        {
            this.SelectingFiles += HandleSelectingFiles;
        }

        private async Task HandleSelectionInternal(FileSelectionEventArgs e)
        {
            switch (e.FileSelectionType)
            {
                case FileSelectionType.SaveAsEncrypted:
                case FileSelectionType.SaveAsDecrypted:
                    //await HandleSaveAsFileSelection(e);
                    break;

                case FileSelectionType.WipeConfirm:
                    await HandleWipeConfirm(e);
                    break;

                case FileSelectionType.Folder:
                    //await HandleFolderSelection(e);
                    break;

                default:
                    await HandleOpenFileSelection(e);
                    break;
            }
        }

        private async void HandleSelectingFiles(object sender, FileSelectionEventArgs fileSelectionEventArgs)
        {
            IEnumerable<FileResult> pickResult = await InternalFileSelectionAsync(fileSelectionEventArgs);

            if (pickResult.Any())
            {
                //fileSelectionEventArgs.SelectedFiles = pickResult.Select(file => file.FullPath).ToArray();

                if (!fileSelectionEventArgs.Cancel)
                {
                    switch (fileSelectionEventArgs.FileSelectionType)
                    {
                        case FileSelectionType.Open:
                            //New<MainHomeViewModel>().OpenSelectedFile(fileSelectionEventArgs);
                            break;

                        case FileSelectionType.Encrypt:
                            //New<MainHomeViewModel>().EncryptSelectedFiles(fileSelectionEventArgs);
                            break;

                        case FileSelectionType.Decrypt:
                            //New<MainHomeViewModel>().DecryptSelectedFiles(fileSelectionEventArgs);
                            break;

                            //case FileSelectionType.KeySharing:
                            //    await New<MainHomeViewModel>().KeyShareSelectedFiles(fileSelectionEventArgs);
                            //    break;
                    }
                }
            }
        }

        private static async Task<IEnumerable<FileResult>> InternalFileSelectionAsync(FileSelectionEventArgs e)
        {
            IDictionary<DevicePlatform, IEnumerable<string>> fileTypes = GetFileTypesForSelectionType(e.FileSelectionType);

            FilePickerFileType customFileType = new FilePickerFileType(fileTypes);
            IEnumerable<FileResult> pickResult = await FilePicker.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Please select files",
                FileTypes = customFileType,
            });

            if (!pickResult.Any())
            {
                e.Cancel = true;
            }

            return pickResult;
        }

        private static IDictionary<DevicePlatform, IEnumerable<string>> GetFileTypesForSelectionType(FileSelectionType selectionType)
        {
            Dictionary<DevicePlatform, IEnumerable<string>> fileTypes = new Dictionary<DevicePlatform, IEnumerable<string>>();
            IRuntimeEnvironment runtimeEnvironment = New<IRuntimeEnvironment>();

            switch (selectionType)
            {
                case FileSelectionType.Open:
                case FileSelectionType.Decrypt:
                case FileSelectionType.Rename:
                case FileSelectionType.KeySharing:
                case FileSelectionType.KeySharingEncrypt:
                    fileTypes.Add(DevicePlatform.WinUI, new[] { "." + runtimeEnvironment.AxCryptExtension });
                    fileTypes.Add(DevicePlatform.iOS, new string[] { });
                    fileTypes.Add(DevicePlatform.Android, new string[] { });
                    break;

                case FileSelectionType.Encrypt:
                case FileSelectionType.Wipe:
                    fileTypes.Add(DevicePlatform.WinUI, new string[] { });
                    fileTypes.Add(DevicePlatform.iOS, new string[] { });
                    fileTypes.Add(DevicePlatform.Android, new string[] { });
                    break;

                case FileSelectionType.ImportPublicKeys:
                case FileSelectionType.ImportPrivateKeys:
                    fileTypes.Add(DevicePlatform.WinUI, new[] { ".txt", "." + runtimeEnvironment.AxCryptExtension });
                    fileTypes.Add(DevicePlatform.iOS, new string[] { });
                    fileTypes.Add(DevicePlatform.Android, new string[] { });
                    break;

                default:
                    throw new NotImplementedException("File selection type not supported.");
            }

            return fileTypes;
        }

        public FileSelectionEventArgs SelectFiles(FileSelectionType fileSelectionType)
        {
            FileSelectionEventArgs fileSelectionArgs = new FileSelectionEventArgs(new string[0])
            {
                FileSelectionType = fileSelectionType,
            };
            OnSelectingFiles(fileSelectionArgs);
            if (fileSelectionArgs.Cancel)
            {
                return new FileSelectionEventArgs(new List<string>());
            }

            return fileSelectionArgs;
        }

        protected virtual void OnSelectingFiles(FileSelectionEventArgs e)
        {
            SelectingFiles?.Invoke(this, e);
        }

        private async Task HandleWipeConfirm(FileSelectionEventArgs e)
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert("Confirm Wipe", "Are you sure you want to permanently wipe the selected file?", "Yes", "No");
            if (confirm)
            {
                // Logic to delete or wipe the file
                File.Delete(e.SelectedFiles[0]);
            }
        }

        private async Task HandleOpenFileSelection(FileSelectionEventArgs e)
        {
            FileResult pickResult = await FilePicker.Default.PickAsync();

            if (pickResult != null)
            {
                e.SelectedFiles[0] = pickResult.FullPath;
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void DisableUI()
        {
            Application.Current.MainPage.IsEnabled = false;
        }

        private void RestoreUI()
        {
            Application.Current.MainPage.IsEnabled = true;
        }
    }
}