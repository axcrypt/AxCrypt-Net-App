using AxCrypt.Abstractions;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.UI.ViewModels;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.App.Shared.ViewModels.Authentication;
using AxCrypt.Content;
using AxCrypt.Core.IO;
using AxCrypt.Core.UI;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Upload;
using System.Diagnostics;
using static AxCrypt.Abstractions.TypeResolve;
using static Google.Apis.Drive.v3.FilesResource;

namespace AxCrypt.App.Shared.CloudCore.GoogleDrive
{
    internal class GoogleDriveServices : FileStorageProvider
    {
        private int chunkFileSize = GoogleDriveConfiguration.ChunkFileSize;
        private const int maxRetries = 3;
        private int retryCount = 0;

        public class FileInformation
        {
            public string? Name { get; set; }

            public string? Content { get; set; }
        }

        private DriveService? _driveService;
        private GoogleDriveAuthenticator _instance;

        private List<FilePickerItemViewModel> _files = new List<FilePickerItemViewModel>();

        public override List<FilePickerItemViewModel> Files
        {
            get => _files;
        }

        private OAuth2Auth? _oAuth2Authenticator;

        public override OAuth2Auth? OAuth2Authenticator
        {
            get => _oAuth2Authenticator;
        }

        public override string PageTitle { get; } = Texts.KnownFolderNameGoogleDrive;

        private Action<FileStorageProvider> initiateFilePickerAsync { get; set; } = _ => { };

        public GoogleDriveServices(Action<FileStorageProvider> initiateFilePicker)
            : this(new GoogleDriveAuthenticator(), initiateFilePicker) { }

        public GoogleDriveServices(
            GoogleDriveAuthenticator instance,
            Action<FileStorageProvider> initiateFilePicker
        )
        {
            if (!New<IInternetState>().Connected)
            {
                throw new InvalidOperationException(
                    "No Internet Acccess, please check your internet connection."
                );
            }

            _instance = instance;
            _oAuth2Authenticator = instance.Auth;
            if (_oAuth2Authenticator != null)
            {
                _oAuth2Authenticator.Completed += async (sender, e) =>
                    await Presenter_Completed(sender, e);
            }
            StartOAuthLoginPresenter();

            initiateFilePickerAsync = initiateFilePicker;
        }

        private async void StartOAuthLoginPresenter()
        {
            if (_instance.UserCredential != null)
            {
                await InitializeAsync();
                return;
            }

            await InitializeAuth();
        }

        private async Task InitializeAuth()
        {
            string message =
                $"AxCrypt needs your permission to access your {PageTitle} to open, encrypt, decrypt, and securely key share your files.\nYour privacy is our priority — we never store your files or share your data.\n\nWould you like to connect now?";

            PopupButtons popupResult = await New<IPopup>().ShowAsync(PopupButtons.OkCancel, $"Connect to {PageTitle}", message);

            if (popupResult == PopupButtons.Cancel)
            {
                return;
            }

            try
            {
                await New<ICloudPlatformService>().InitializeCloudAuth(OAuth2Authenticator!);
            }
            catch (TaskCanceledException ex)
            {
                return;
            }
        }

        private async Task Presenter_Completed(object sender, EventArgs e)
        {
            await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            if (!New<IInternetState>().Connected)
            {
                return;
            }

            if (_instance.UserCredential == null)
            {
                return;
            }

            using (ProcessIndicator processIndicator = new ProcessIndicator())
            {
                await LoadDriveFilesAsync();
            }
        }

        public async Task LoadDriveFilesAsync()
        {
            if (!New<IInternetState>().Connected)
            {
                return;
            }

            await LoadCloudDriveAsync();
            await ListFilesAsync();

            initiateFilePickerAsync(this);
        }

        private async Task LoadCloudDriveAsync()
        {
            try
            {
                _driveService = new DriveService(
                    new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = _instance.UserCredential,
                        ApplicationName = GoogleDriveConfiguration.ApplicationId,
                    }
                );
            }
            catch (Exception ex)
            {
                await New<IPopup>()
                    .ShowAsync(
                        PopupButtons.Ok,
                        Texts.WarningTitle,
                        $"Failed to create drive service, due to {ex.Message}! Try again."
                    );
                return;
            }
        }

        public override async Task ListFilesAsync(string fileId = "")
        {
            try
            {
                string queryString = String.Format("'root' in parents and trashed=false");
                if (fileId.Length > 0)
                {
                    queryString = String.Format("parents in '{0}'", fileId);
                }

                ListRequest fileListRequest = _driveService!.Files.List();
                fileListRequest.Q = queryString;
                fileListRequest.Fields = "nextPageToken, files(id,name,mimeType,size,modifiedTime,fileExtension)";

                try
                {
                    FileList driveFileList = await fileListRequest.ExecuteAsync();
                    GenerateFileItemLists(driveFileList.Files);
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, ex.Message);
            }
        }

        private void GenerateFileItemLists(IList<Google.Apis.Drive.v3.Data.File> files)
        {
            if (files == null)
            {
                return;
            }

            _files = files
                .Select(file => new FilePickerItemViewModel()
                {
                    FileID = file.Id,
                    FileName = file.Name,
                    IsFolder = file.MimeType == FilePickerItemViewModel.GOOGLEFOLDER_MIMETYPE,
                    MimeType = file.MimeType,
                    FileExtension = file.FileExtension,
                    FileSize = FormatSize(file.Size),
                    ModifiedTime = file.ModifiedTimeDateTimeOffset?.ToString("MM/dd/yyyy"),
                    Source = AxCrypt.Core.IO.FileProvider.GoogleDrive,
                })
                .OrderByDescending(file => file.IsFolder)
                .ToList();
        }

        public override async Task<MemoryStream> ReadFileStreamAsync(string fileId)
        {
            if (!New<IInternetState>().Connected)
            {
                return null!;
            }

            FileInformation data = new FileInformation();

            Google.Apis.Drive.v3.Data.File metadata = await _driveService!.Files.Get(fileId).ExecuteAsync();
            data.Name = metadata.Name;

            using MemoryStream ms = new MemoryStream();
            await _driveService.Files.Get(fileId).DownloadAsync(ms);

            return ms;
        }

        public override async Task CopyFileToImportedFiles(FilePickerItemViewModel fileItem, Stream destinationFileStream)
        {
            if (!New<IInternetState>().Connected)
            {
                throw new InvalidOperationException(
                    "No Internet Acccess, please check your internet connection."
                );
            }

            if (fileItem == null)
            {
                throw new ArgumentNullException("request");
            }

            if (destinationFileStream == null)
            {
                throw new ArgumentNullException("request");
            }

            //string fileName - filename already we are reading for the list - if possibel try to pass that value here
            Google.Apis.Drive.v3.FilesResource.GetRequest getFileRequest = _driveService.Files.Get(fileItem.FileID);
            getFileRequest.MediaDownloader.ChunkSize = chunkFileSize;
            getFileRequest.Alt = FilesResource.GetRequest.AltEnum.Media;
            getFileRequest.SupportsAllDrives = true;

            getFileRequest.MediaDownloader.ProgressChanged += (
                Google.Apis.Download.IDownloadProgress progress
            ) =>
            {
                switch (progress.Status)
                {
                    case DownloadStatus.Downloading:
                        {
                            Debug.WriteLine($"Downloading");
                            break;
                        }
                    case DownloadStatus.Completed:
                        {
                            Debug.WriteLine($"Downloading Completed");
                            break;
                        }
                    case DownloadStatus.Failed:
                        {
                            Debug.WriteLine($"Downloading Failed");
                            throw progress.Exception;
                        }
                }
            };

            try
            {
                await getFileRequest.DownloadAsync(destinationFileStream);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public override async Task<bool> UpdateFile(FilePickerItemViewModel fileItem, AxCrypt.Core.Session.ActiveFile encryptedFile)
        {
            if (!New<IInternetState>().Connected)
            {
                return false;
            }

            try
            {
                Google.Apis.Drive.v3.Data.File file = _driveService!.Files.Get(fileItem.FileID).Execute();

                string contentType = "application/octet-stream";

                file.Name = encryptedFile.EncryptedFileInfo.Name;
                file.MimeType = contentType;

                using (Stream stream = encryptedFile.EncryptedFileInfo.OpenRead())
                {
                    FilesResource.UpdateMediaUpload request = _driveService.Files.Update(
                        file,
                        fileItem.FileID,
                        stream,
                        contentType
                    );

                    await request.UploadAsync();
                    return request.ResponseBody != null;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<string> UploadFileAsync(FilePickerItemViewModel actualFileItem, string fileName, IDataStore fileInfo, CancellationToken ct = default)
        {
            if (actualFileItem == null)
                throw new ArgumentNullException(nameof(actualFileItem));

            if (fileInfo == null)
                throw new ArgumentNullException(nameof(fileInfo));

            retryCount = 0;

            Google.Apis.Drive.v3.Data.File fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileInfo.Name
            };

            if (_files.Any(f => f.FileID == actualFileItem.FileID))
            {
                GetRequest getRequest = _driveService!.Files.Get(actualFileItem.FileID);
                getRequest.Fields = "parents";
                getRequest.SupportsAllDrives = true;

                Google.Apis.Drive.v3.Data.File existingFile = await getRequest.ExecuteAsync(ct);
                fileMetadata.Parents = existingFile.Parents;
            }

            const string contentType = "application/octet-stream";

            using Stream fileStream = fileInfo.OpenRead();
            fileStream.Position = 0;

            CreateMediaUpload createRequest = _driveService.Files.Create(fileMetadata, fileStream, contentType);

            createRequest.ChunkSize = ResumableUpload.MinimumChunkSize * 32;
            createRequest.Fields = "id";
            createRequest.SupportsAllDrives = true;

            createRequest.ProgressChanged += progress =>
            {
                switch (progress.Status)
                {
                    case UploadStatus.Uploading:
                        Debug.WriteLine($"Uploading");
                        break;

                    case UploadStatus.Completed:
                        Debug.WriteLine("Upload completed");
                        break;

                    case UploadStatus.Failed:
                        Debug.WriteLine($"Upload failed: {progress.Exception}");
                        break;
                }
            };

            try
            {
                while (retryCount < maxRetries)
                {
                    try
                    {
                        IUploadProgress progress = await createRequest.UploadAsync(ct);

                        if (progress.Status == UploadStatus.Completed)
                        {
                            return createRequest.ResponseBody.Id;
                        }
                    }
                    catch (Exception ex) when (IsNetworkError(ex))
                    {
                        retryCount++;
                        await Task.Delay(2000, ct);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Upload failed :" + ex.Message);
            }

            return string.Empty;
        }

        public override async Task<string> MoveFile(FilePickerItemViewModel actualFileItem, string fileName, IDataStore fileInfo)
        {
            if (!New<IInternetState>().Connected)
            {
                return string.Empty;
            }

            if (fileName == null)
            {
                return string.Empty;
            }

            if (fileInfo == null || !fileInfo.IsAvailable)
            {
                return string.Empty;
            }

            try
            {
                return await UploadFileAsync(actualFileItem, fileName, fileInfo);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public override async Task<bool> DeleteFileAsync(string fullFileName, FilePickerItemViewModel fileItem, string encryptedFilePathForOverWrite,
            string newFileId = "", bool rename = false)
        {
            if (!New<IInternetState>().Connected)
            {
                return false;
            }

            try
            {
                await _driveService!.Files.Delete(fileItem.FileID).ExecuteAsync();

                if (string.IsNullOrEmpty(fullFileName))
                {
                    return true;
                }

                IDataStore file = New<IDataStore>(fullFileName);
                if (file != null && file.IsAvailable)
                {
                    WipeLocalFile(fullFileName);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static readonly string[] SUFFIXES = { "Bytes", "KB", "MB", "GB", "TB", "PB" };

        private string FormatSize(Int64? bytes)
        {
            int counter = 0;
            if (bytes != null)
            {
                decimal number = (decimal)bytes;
                while (Math.Round(number / 1024) >= 1)
                {
                    number = number / 1024;
                    counter++;
                }
                return string.Format("{0:n1}{1}", number, SUFFIXES[counter]);
            }
            return null!;
        }
    }
}