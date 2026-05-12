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
            catch (TaskCanceledException)
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

        #region SearchFile

        public override async Task SearchFileFolderAsync(string query, string path)
        {
            string searchText = query.Trim();
            if (string.IsNullOrEmpty(searchText))
                return;

            string escapedQuery = searchText.Replace("'", "\\'");

            try
            {
                List<Google.Apis.Drive.v3.Data.File> allFiles = new();

                List<string> folderIds = await GetAllSubFolderIdsAsync(path);
                folderIds.Add(path);

                foreach (string folderId in folderIds)
                {
                    List<Google.Apis.Drive.v3.Data.File> folderFiles =
                        await SearchFilesInFolderAsync(escapedQuery, folderId);

                    allFiles.AddRange(folderFiles);
                }

                _files = allFiles.Select(f => new FilePickerItemViewModel()
                {
                    FileID = f.Id,
                    FileName = f.Name,
                    IsFolder = f.MimeType == "application/vnd.google-apps.folder",
                    FileExtension = string.IsNullOrEmpty(f.FileExtension)
                            ? Path.GetExtension(f.Name ?? "")
                            : f.FileExtension,
                    Source = FileProvider.GoogleDrive,
                }).ToList();
            }
            catch (Exception e)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, e.Message);
            }
        }

        /// <summary>
        /// Search files matching query inside a specific folder (or entire drive if folderId is null).
        /// Excludes folders, trashed files, and hidden/app-generated files (matches Normal UI).
        /// </summary>
        private async Task<List<Google.Apis.Drive.v3.Data.File>> SearchFilesInFolderAsync(
            string escapedQuery, string folderId)
        {
            List<Google.Apis.Drive.v3.Data.File> results = new();

            string q = $"name contains '{escapedQuery}' " +
                       $"and trashed = false " +
                       $"and mimeType != 'application/vnd.google-apps.folder' " +
                       $"and mimeType != 'application/vnd.google-apps.script' " +
                       $"and mimeType != 'application/vnd.google-apps.form' " +
                       $"and 'me' in owners " +
                       $"and '{folderId}' in parents ";

            ListRequest request = _driveService!.Files.List();
            request.Q = q;
            request.Fields = "nextPageToken, files(id, name, mimeType, fileExtension, parents, size, modifiedTime)";
            request.PageSize = 100;
            request.Spaces = "drive";
            request.Corpora = "user";

            while (true)
            {
                FileList searchResult = await request.ExecuteAsync();
                if (searchResult.Files != null)
                    results.AddRange(searchResult.Files);

                if (string.IsNullOrEmpty(searchResult.NextPageToken)) break;
                request.PageToken = searchResult.NextPageToken;
            }

            return results;
        }

        /// <summary>
        /// Recursively fetches all subfolder IDs under a given folder.
        /// </summary>
        private async Task<List<string>> GetAllSubFolderIdsAsync(string folderId)
        {
            List<string> allSubFolderIds = new();
            Queue<string> queue = new();
            queue.Enqueue(folderId);

            while (queue.Count > 0)
            {
                string currentFolderId = queue.Dequeue();

                ListRequest request = _driveService!.Files.List();
                request.Q = $"'{currentFolderId}' in parents " +
                            $"and mimeType = 'application/vnd.google-apps.folder' " +
                            $"and trashed = false ";
                request.Fields = "nextPageToken, files(id, name)";
                request.PageSize = 100;

                while (true)
                {
                    FileList result = await request.ExecuteAsync();
                    if (result.Files != null)
                    {
                        foreach (Google.Apis.Drive.v3.Data.File? folder in result.Files)
                        {
                            allSubFolderIds.Add(folder.Id);
                            queue.Enqueue(folder.Id);
                        }
                    }

                    if (string.IsNullOrEmpty(result.NextPageToken)) break;
                    request.PageToken = result.NextPageToken;
                }
            }

            return allSubFolderIds;
        }

        #endregion SearchFile

        public override async Task ListSharedFilesAsync()
        {
            try
            {
                List<Google.Apis.Drive.v3.Data.File> allItems = new();
                ListRequest request = _driveService!.Files.List();

                request.Q = "'me' in owners and trashed = false";
                request.Fields = "nextPageToken, files(id, name, mimeType, shared, sharingUser)";
                request.PageSize = 100;

                do
                {
                    FileList result = await request.ExecuteAsync();
                    if (result.Files != null)
                    {
                        List<Google.Apis.Drive.v3.Data.File> sharedByMe = result.Files
                            .Where(f => f.Shared == true && IsAxCryptFile(f.Name)).ToList();

                        allItems.AddRange(sharedByMe);
                    }
                    request.PageToken = result.NextPageToken;
                } while (!string.IsNullOrEmpty(request.PageToken));

                GenerateFileItemLists(allItems);
            }
            catch (Exception e)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, e.Message);
            }
        }

        public override async Task ListSharedWithFilesAsync()
        {
            try
            {
                List<Google.Apis.Drive.v3.Data.File> allItems = new List<Google.Apis.Drive.v3.Data.File>();

                ListRequest request = _driveService!.Files.List();
                request.Q = "sharedWithMe = true";
                request.Fields = "nextPageToken, files(id, name, mimeType, size, modifiedTime, owners)";
                request.PageSize = 100;

                do
                {
                    FileList result = await request.ExecuteAsync();

                    if (result.Files != null)
                        allItems.AddRange(result.Files.Where(f => IsAxCryptFile(f.Name)));

                    request.PageToken = result.NextPageToken;
                } while (!string.IsNullOrEmpty(request.PageToken));

                GenerateFileItemLists(allItems);
            }
            catch (Exception e)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, e.Message);
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
                catch (Exception)
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

            _files = files.Select(file => new FilePickerItemViewModel()
            {
                FileID = file.Id,
                FileName = file.Name,
                IsFolder = file.MimeType == FilePickerItemViewModel.GOOGLEFOLDER_MIMETYPE,
                MimeType = file.MimeType,
                FileExtension = file.FileExtension,
                FileSize = FormatSize(file.Size),
                ModifiedTime = file.ModifiedTimeDateTimeOffset?.ToString("MM/dd/yyyy")!,
                Source = FileProvider.GoogleDrive,
            }).OrderByDescending(file => file.IsFolder).ToList();
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
            GetRequest getFileRequest = _driveService!.Files.Get(fileItem.FileID);
            getFileRequest.MediaDownloader.ChunkSize = chunkFileSize;
            getFileRequest.Alt = GetRequest.AltEnum.Media;
            getFileRequest.SupportsAllDrives = true;

            getFileRequest.MediaDownloader.ProgressChanged += (
                IDownloadProgress progress
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

        public override async Task<bool> UpdateFile(FilePickerItemViewModel cloudFileItem, IDataStore fileInfo, CancellationToken ct = default)
        {
            if (!New<IInternetState>().Connected)
            {
                return false;
            }

            try
            {
                IList<string> actualParentPath = await GetParentFolderPath(cloudFileItem.FileID);

                cloudFileItem.ParentPath = CreateCloudFolder();
                string newFileId = await UploadFileAsync(cloudFileItem, fileInfo.Name, fileInfo);

                if (string.IsNullOrEmpty(newFileId))
                {
                    await New<IPopup>().ShowAsync(
                            PopupButtons.Ok,
                            Texts.WarningTitle,
                            "Your file was successfully encrypted, however there was a problem when moving the encrypted file. The encrypted left is not updated and try again.",
                            Common.DoNotShowAgainOptions.None
                        );

                    return false;
                }

                if (!await DeleteFileAsync(fileInfo.FullName, cloudFileItem, fileInfo.FullName))
                {
                    await New<IPopup>().ShowAsync(
                            PopupButtons.Ok,
                            Texts.WarningTitle,
                            "Your file was successfully encrypted, however there was a problem when deleting the original file. The original left is left untouched and needs to be removed manually.",
                            Common.DoNotShowAgainOptions.None
                        );

                    return false;
                }

                if (!await MoveFile(newFileId, cloudFileItem.ParentPath, actualParentPath.FirstOrDefault()!))
                {
                    await New<IPopup>().ShowAsync(
                        PopupButtons.Ok,
                        Texts.WarningTitle,
                        "The file was uploaded to the temporary folder successfully. However, it could not be moved to the destination folder. The file remains in the temporary folder and must be moved to the destination folder manually.",
                        Common.DoNotShowAgainOptions.None
                    );

                    return false;
                }

                try
                {
                    FilesResource.DeleteRequest deleteRequest = _driveService!.Files.Delete(cloudFileItem.ParentPath);
                    deleteRequest.SupportsAllDrives = true;
                    await deleteRequest.ExecuteAsync(ct);
                }
                catch (Exception)
                {
                    await New<IPopup>().ShowAsync(
                        PopupButtons.Ok,
                        Texts.WarningTitle,
                        "Your file was uploaded successfully. However, there was a problem deleting the temporary folder created for the secure upload. Please remove the temporary folder manually.",
                        Common.DoNotShowAgainOptions.None
                    );

                    return false;
                }

                cloudFileItem.FileID = newFileId;
                return true;
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
                fileMetadata.Parents = await GetParentFolderPath(actualFileItem.FileID);
            }

            if (!string.IsNullOrEmpty(actualFileItem.ParentPath))
            {
                fileMetadata.Parents = new List<string> { actualFileItem.ParentPath };
            }

            const string contentType = "application/octet-stream";

            using Stream fileStream = fileInfo.OpenRead();
            fileStream.Position = 0;

            CreateMediaUpload createRequest = _driveService!.Files.Create(fileMetadata, fileStream, contentType);

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

                        if (progress.Status == UploadStatus.Failed)
                        {
                            retryCount++;

                            Console.WriteLine($"Upload failed: {progress.Exception?.Message}");

                            await Task.Delay(2000, ct);
                            continue;
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

        private string CreateCloudFolder()
        {
            Google.Apis.Drive.v3.Data.File folderMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = "/MyAxcryptTempFile_" + GenerateRandomFolderName(),
                MimeType = "application/vnd.google-apps.folder"
            };

            CreateRequest folderRequest = _driveService!.Files.Create(folderMetadata);
            folderRequest.Fields = "id";

            Google.Apis.Drive.v3.Data.File folder = folderRequest.Execute();
            return folder.Id;
        }

        public async Task<bool> MoveFile(string fileId, string folderId, string parentFolderPath)
        {
            try
            {
                UpdateRequest moveRequest = _driveService!.Files.Update(new Google.Apis.Drive.v3.Data.File(), fileId);
                moveRequest.AddParents = parentFolderPath;
                moveRequest.RemoveParents = folderId;
                moveRequest.Fields = "id, parents";
                moveRequest.SupportsAllDrives = true;

                Google.Apis.Drive.v3.Data.File movedFile = await moveRequest.ExecuteAsync();
                return movedFile != null;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Move failed: {ex.Message}");
                return false;
            }
        }

        public override async Task<ShareResult> ShareFileAsync(string fileId, ShareRequest request)
        {
            Permission linkPermission = new Permission
            {
                Type = request.LinkType == ShareLinkType.TeamOnly ? "domain" : "anyone",
                Role = "reader"
            };
            await _driveService!.Permissions.Create(linkPermission, fileId).ExecuteAsync();

            FilesResource.GetRequest fileReq = _driveService.Files.Get(fileId);
            fileReq.Fields = "webViewLink";
            Google.Apis.Drive.v3.Data.File file = await fileReq.ExecuteAsync();

            _ = Task.Run(async () =>
            {
                foreach (string email in request.RecipientEmailList)
                {
                    try
                    {
                        PermissionsResource.CreateRequest permReq = _driveService.Permissions.Create(new Permission
                        {
                            Type = "user",
                            Role = request.Permission == SharePermission.Editor ? "writer" : "reader",
                            EmailAddress = email
                        }, fileId);

                        permReq.SendNotificationEmail = true;
                        permReq.EmailMessage = request.Message;

                        await permReq.ExecuteAsync();
                        await Task.Delay(1000);
                    }
                    catch (Exception ex) { Debug.WriteLine($"Background Share Error: {ex.Message}"); }
                }
            });

            return new ShareResult
            {
                ShareableLink = file.WebViewLink,
                PermissionSet = true,
                RecipientEmailList = request.RecipientEmailList
            };
        }

        private async Task<IList<string>> GetParentFolderPath(string fileId)
        {
            GetRequest getRequest = _driveService!.Files.Get(fileId);
            getRequest.Fields = "parents";
            getRequest.SupportsAllDrives = true;

            Google.Apis.Drive.v3.Data.File existingFile = await getRequest.ExecuteAsync();

            return existingFile.Parents;
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