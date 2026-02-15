using AxCrypt.Abstractions;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.UI.ViewModels;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.App.Shared.ViewModels.Authentication;
using AxCrypt.Content;
using AxCrypt.Core.IO;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using Azure.Core;
using Microsoft.Graph;
using Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;
using Microsoft.Graph.Models;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.CloudCore.OneDrive
{
    public class OneDriveServices : FileStorageProvider
    {
        private OneDriveAuthenticator _instance;
        private GraphServiceClient _graphClient;

        private List<FilePickerItemViewModel> _files = new List<FilePickerItemViewModel>();

        private readonly string OneDriveRootFolderPath = "/drive/root:";

        private readonly string RootFolderName = "root";

        private string? _userDriveId;

        private int chunkFileSize = OneDriveConfiguration.ChunkFileSize;
        private const int maxRetries = 3;
        private int retryCount = 0;

        public override List<FilePickerItemViewModel> Files
        {
            get => _files;
        }

        private OAuth2Auth? _oAuth2Authenticator;

        public override OAuth2Auth OAuth2Authenticator
        {
            get => _oAuth2Authenticator;
        }

        public override string PageTitle => AxCrypt.Content.Texts.KnownFolderNameOneDrive;

        private Action<FileStorageProvider> initiateFilePickerAsync { get; set; } = _ => { };

        public OneDriveServices(Action<FileStorageProvider> initiateFilePicker)
            : this(new OneDriveAuthenticator(), initiateFilePicker) { }

        public OneDriveServices(
            OneDriveAuthenticator instance,
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
            if (_instance.AccessToken != null)
            {
                await InitializeFilesAsync();
                return;
            }

            await SignInAsync();
        }

        private async Task SignInAsync()
        {
            string message =
                $"AxCrypt needs your permission to access your {PageTitle} to open, encrypt, decrypt, and securely share your files.\nYour privacy is our priority — we never store your files or share your data.\n\nWould you like to connect now?";

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
            await InitializeFilesAsync();
        }

        private async Task InitializeFilesAsync()
        {
            if (!New<IInternetState>().Connected)
            {
                return;
            }

            if (_instance.AccessToken == null)
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
            _graphClient = GetAuthenticatedClient();
            Microsoft.Graph.Models.Drive? driveInfo = await _graphClient.Me.Drive.GetAsync();
            _userDriveId = driveInfo!.Id;
        }

        public override async Task ListFilesAsync(string folderId = "")
        {
            try
            {
                if (!New<IInternetState>().Connected)
                {
                    return;
                }

                IList<DriveItem> files = await GetDriveFiles(folderId);
                if (files == null)
                {
                    return;
                }

                _files = files.Select(file => new FilePickerItemViewModel()
                {
                    FileID = file.Id,
                    FileName = file.Name,
                    IsFolder = file.Folder != null,
                    MimeType = file.File?.MimeType,
                    FileExtension = file.Folder != null ? "" : System.IO.Path.GetExtension(file.Name),
                    ParentPath = file.ParentReference!.Path!,
                    Source = AxCrypt.Core.IO.FileProvider.OneDrive,
                })
                    .OrderByDescending(file => file.IsFolder)
                    .ToList();
            }
            catch (Exception ex)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, $"Failed to create drive service, due to {ex.Message}! Try again.");
                return;
            }
        }

        private async Task<IList<DriveItem>> GetDriveFiles(string folder)
        {
            if (string.IsNullOrEmpty(folder))
            {
                folder = RootFolderName;
            }

            if (_userDriveId == null)
            {
                return null!;
            }

            return (await _graphClient.Drives[_userDriveId].Items[folder].Children.GetAsync())?.Value!;
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

            try
            {
                DriveItem? item = await _graphClient.Drives[_userDriveId].Items[fileItem.FileID].GetAsync();

                if (item == null)
                    throw new Exception("File not found in OneDrive.");

                if (!item.AdditionalData.TryGetValue("@microsoft.graph.downloadUrl", out object? downloadUrlObj) || downloadUrlObj is null)
                    throw new Exception("Download URL missing.");

                string downloadUrl = downloadUrlObj.ToString();

                long fileSize = item.Size ?? throw new Exception("File size missing.");
                long offset = 0;

                using HttpClient httpClient = new HttpClient();

                while (offset < fileSize)
                {
                    long end = Math.Min(offset + chunkFileSize - 1, fileSize - 1);
                    retryCount = 0;

                    while (true)
                    {
                        try
                        {
                            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
                            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(offset, end);

                            using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                            response.EnsureSuccessStatusCode();

                            using Stream stream = await response.Content.ReadAsStreamAsync();
                            destinationFileStream.Position = offset;
                            await stream.CopyToAsync(destinationFileStream);

                            offset = end + 1;
                            break;
                        }
                        catch (Exception ex) when (IsNetworkError(ex))
                        {
                            retryCount++;
                            if (retryCount > maxRetries)
                                throw;

                            await Task.Delay(2000);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public override async Task<MemoryStream> ReadFileStreamAsync(string fileId)
        {
            if (fileId == null)
            {
                return null!;
            }

            if (!New<IInternetState>().Connected)
            {
                return null!;
            }

            using MemoryStream memoryStream = new MemoryStream();

            using Stream? stream = await _graphClient.Drives[_userDriveId].Items[fileId].Content.GetAsync();
            await stream!.CopyToAsync(memoryStream);

            return memoryStream;
        }

        public override async Task<string> MoveFile(FilePickerItemViewModel actualfileItem, string fileName, IDataStore fileInfo)
        {
            try
            {
                return await UploadFileAsync(actualfileItem, fileName, fileInfo);
            }
            catch (HttpRequestException e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            return string.Empty;
        }

        private async Task<string> UploadFileAsync(FilePickerItemViewModel actualFileItem, string fileName, IDataStore fileInfo, bool overwrite = false)
        {
            CreateUploadSessionPostRequestBody uploadProps = new CreateUploadSessionPostRequestBody();

            if (!overwrite)
            {
                uploadProps.AdditionalData = new Dictionary<string, object>
                {
                    { "@microsoft.graph.conflictBehavior", "rename" }
                };
            }
            else
            {
                uploadProps.AdditionalData = new Dictionary<string, object>
                {
                    { "@microsoft.graph.conflictBehavior", "replace" }
                };
            }

            UploadSession? uploadSession;
            if (!overwrite)
            {
                string folderPath = actualFileItem.ParentPath.Replace(OneDriveRootFolderPath, "");
                folderPath += folderPath.Length > 0 ? "/" : "";

                uploadSession = await _graphClient
                    .Drives[_userDriveId]
                    .Items[RootFolderName]
                    .ItemWithPath(folderPath + fileName)
                    .CreateUploadSession.PostAsync(uploadProps);
            }
            else
            {
                uploadSession = await _graphClient
                    .Drives[_userDriveId]
                    .Items[actualFileItem.FileID]
                    .CreateUploadSession.PostAsync(uploadProps);
            }

            int maxSliceSize = 10 * 1024 * 1024;

            using Stream fileStream = fileInfo.OpenRead();
            long totalLength = fileStream.Length;

            LargeFileUploadTask<DriveItem> fileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, fileStream, maxSliceSize);

            IProgress<long> progress = new Progress<long>(prog =>
            {
                Console.WriteLine($"Uploaded {prog} bytes of {totalLength} bytes");
            });

            UploadResult<DriveItem> uploadResult = null;

            for (retryCount = 0; retryCount < maxRetries; retryCount++)
            {
                try
                {
                    if (retryCount == 0)
                    {
                        uploadResult = await fileUploadTask.UploadAsync(progress);
                        break;
                    }

                    uploadResult = await fileUploadTask.ResumeAsync();
                    break;
                }
                catch (ServiceException ex) when (IsNetworkError(ex))
                {
                    if (retryCount == maxRetries - 1)
                        throw;

                    await Task.Delay(2000);
                }
            }

            if (uploadResult!.UploadSucceeded)
            {
                return uploadResult.ItemResponse.Id!;
            }

            return string.Empty;
        }

        public override async Task<bool> DeleteFileAsync(string originalFilePath, FilePickerItemViewModel fileItem, string encryptedFilePathForOverWrite,
            string newFileId = "", bool rename = false)
        {
            if (!New<IInternetState>().Connected)
            {
                return false;
            }

            string oneDriveFilePath = fileItem.FileID;
            try
            {
                if (encryptedFilePathForOverWrite != null)
                {
                    IDataStore randomlyEncryptedFile = GenerateRandomFile(encryptedFilePathForOverWrite, false);
                    await UploadFileAsync(fileItem, originalFilePath, randomlyEncryptedFile, true);

                    string renamedFilePath = New<IDataStore>(oneDriveFilePath).Container.FullName + randomlyEncryptedFile.Name;

                    if (string.IsNullOrEmpty(renamedFilePath))
                    {
                        oneDriveFilePath = renamedFilePath;
                    }
                    randomlyEncryptedFile.Delete();
                }

                if (originalFilePath != null)
                {
                    IDataStore file = New<IDataStore>(originalFilePath);
                    if (file != null && file.IsAvailable)
                    {
                        WipeLocalFile(originalFilePath);
                    }
                }

                if (string.IsNullOrEmpty(oneDriveFilePath))
                {
                    throw new ArgumentException("The fileId cannot be empty.");
                }

                return await DeleteFileAsync(oneDriveFilePath);
            }
            catch (HttpRequestException e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            return false;
        }

        public async Task<bool> DeleteFileAsync(string itemId)
        {
            try
            {
                await _graphClient.Drives[_userDriveId].Items[itemId].DeleteAsync();

                return true;
            }
            catch (HttpRequestException hrex)
            {
                throw hrex;
            }
        }

        public override async Task<bool> UpdateFile(FilePickerItemViewModel fileItem, ActiveFile encryptedFile)
        {
            if (!New<IInternetState>().Connected)
            {
                return false;
            }

            try
            {
                await UploadFileAsync(fileItem, encryptedFile.EncryptedFileInfo.Name, encryptedFile.EncryptedFileInfo, true);
                return await Rename(fileItem.FileID, encryptedFile.EncryptedFileInfo.Name);
            }
            catch (HttpRequestException e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
            catch (TaskCanceledException e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            return false;
        }

        private async Task<bool> Rename(string fileId, string fullFilePath)
        {
            if (string.IsNullOrEmpty(fileId))
            {
                return false;
            }

            DriveItem? driveFile = await _graphClient.Drives[_userDriveId].Items[fileId].GetAsync();
            if (driveFile == null)
            {
                return false;
            }

            IDataStore newFileStore = New<IDataStore>(fullFilePath);
            if (driveFile.Name == newFileStore.Name)
            {
                return false;
            }

            FilePickerItemViewModel fileItem = new FilePickerItemViewModel()
            {
                FileID = driveFile.Id,
                FileName = driveFile.Name,
                IsFolder = driveFile.Folder != null,
                MimeType = driveFile.File?.MimeType,
                FileExtension = driveFile.Folder != null ? "" : System.IO.Path.GetExtension(driveFile.Name),
                ParentPath = driveFile.ParentReference!.Path!,
                Source = AxCrypt.Core.IO.FileProvider.OneDrive,
            };

            string newfileName = await TempFileName(newFileStore.Name, fileItem);
            if (driveFile.Name == newfileName)
            {
                return false;
            }
            DriveItem driveItemToUpdate = new DriveItem { Name = newfileName, };

            await _graphClient.Drives[_userDriveId].Items[fileId].PatchAsync(driveItemToUpdate);

            return true;
        }

        private int fileNameCounter = 0;
        private string originalFileName = "";

        private async Task<string> TempFileName(string fileName, FilePickerItemViewModel fileItem = null!, string fileExtension = null!)
        {
            GraphServiceClient graphClient = GetAuthenticatedClient();
            DriveItem? driveFile;
            try
            {
                string filePath = fileName;
                if (fileItem != null)
                {
                    filePath =
                        fileItem.ParentPath.Replace(OneDriveRootFolderPath, "") + "/" + fileName;
                }
                driveFile = await graphClient
                    .Drives[_userDriveId]
                    .Root
                    .ItemWithPath(fileName)
                    .GetAsync();
            }
            catch (Exception? exp)
            {
                fileNameCounter = 0;
                originalFileName = "";
                return fileName;
            }

            if (driveFile == null)
            {
                fileNameCounter = 0;
                originalFileName = "";
                return fileName;
            }

            if (fileNameCounter == 0)
            {
                originalFileName = fileName;
            }
            string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(
                originalFileName
            );
            if (fileExtension == null)
            {
                fileExtension = System.IO.Path.GetExtension(driveFile.Name)!;
            }

            string newNamePart =
                fileNameCounter > 0 ? $"({fileNameCounter}){fileExtension}" : fileExtension!;
            fileNameCounter++;
            return await TempFileName(
                $"{fileNameWithoutExtension}{newNamePart}",
                fileItem!,
                fileExtension!
            );
        }

        private GraphServiceClient GetAuthenticatedClient()
        {
            CustomTokenCredential tokenCredential = new CustomTokenCredential(
                _instance.AccessToken,
                _instance.AccessTokenExpireOffset
            );

            return new GraphServiceClient(tokenCredential);
        }
    }

    public class CustomTokenCredential : TokenCredential
    {
        private readonly AccessToken _AccessToken;

        public CustomTokenCredential(string accessToken, DateTimeOffset accessTokenExpireOffset)
        {
            _AccessToken = new AccessToken(accessToken, accessTokenExpireOffset);
        }

        public override AccessToken GetToken(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken
        )
        {
            return _AccessToken;
        }

        public override async ValueTask<AccessToken> GetTokenAsync(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken
        )
        {
            return await Task.FromResult(_AccessToken);
        }
    }
}