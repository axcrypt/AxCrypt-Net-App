using AxCrypt.Abstractions;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.UI.ViewModels;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.App.Shared.ViewModels.Authentication;
using AxCrypt.Content;
using AxCrypt.Core.IO;
using AxCrypt.Core.UI;
using Dropbox.Api;
using Dropbox.Api.Files;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.CloudCore.DropBox
{
    public class DropBoxServices : FileStorageProvider
    {
        private DropBoxAuthenticator _instance;
        private DropboxClient _dropboxclient;
        private int chunkFileSize = DropBoxConfiguration.ChunkFileSize;
        private const int fileUploadLimit = 5 * 1024 * 1024;
        private const int maxRetries = 3;
        private int retryCount = 0;

        private List<FilePickerItemViewModel> _files = new List<FilePickerItemViewModel>();

        public override List<FilePickerItemViewModel> Files
        {
            get => _files;
        }

        private OAuth2Auth _oAuth2Authenticator;

        public override OAuth2Auth OAuth2Authenticator
        {
            get => _oAuth2Authenticator;
        }

        public override string PageTitle => AxCrypt.Content.Texts.KnownFolderNameDropbox;

        private Action<FileStorageProvider> initiateFilePickerAsync { get; set; } = _ => { };

        public DropBoxServices(Action<FileStorageProvider> initiateFilePicker)
            : this(new DropBoxAuthenticator(), initiateFilePicker) { }

        public DropBoxServices(
            DropBoxAuthenticator instance,
            Action<FileStorageProvider> initiateFilePicker
        )
        {
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

            _dropboxclient = CreateDropboxClient();

            await ListFilesAsync();
            initiateFilePickerAsync(this);
        }

        private DropboxClient CreateDropboxClient()
        {
            HttpClient httpClient = new HttpClient(new DropboxHandler())
            {
                Timeout = TimeSpan.FromMinutes(2)
            };

            DropboxClientConfig config = new DropboxClientConfig() { HttpClient = httpClient };

            return new DropboxClient(_instance.AccessToken, config);
        }

        public override async Task SearchFileFolderAsync(string query, string path)
        {
            if (path == "root")
                path = "";

            try
            {
                SearchV2Arg searchQuery = new SearchV2Arg(query: query,
                                    options: new SearchOptions(
                                    path: path,
                                    maxResults: 100,
                                    fileStatus: FileStatus.Active.Instance,
                                    filenameOnly: true));

                SearchV2Result searchResult = await _dropboxclient.Files.SearchV2Async(searchQuery);

                List<SearchMatchV2> allMatches = new List<SearchMatchV2>();
                while (true)
                {
                    allMatches.AddRange(searchResult.Matches.Where(m => m.Metadata.IsMetadata));
                    if (!searchResult.HasMore) break;

                    searchResult = await _dropboxclient.Files.SearchContinueV2Async(searchResult.Cursor);
                }

                _files = allMatches.Select(m => new FilePickerItemViewModel()
                {
                     FileID = m.Metadata.AsMetadata.Value.PathLower,
                     FileName = m.Metadata.AsMetadata.Value.Name,
                     IsFolder = m.Metadata.AsMetadata.Value.IsFolder,
                     FileExtension = Path.GetExtension(m.Metadata.AsMetadata.Value.PathLower),
                     Source = FileProvider.DropBox,
                }).ToList() ?? new List<FilePickerItemViewModel>();
            }
            catch (Exception e)
            {
                if (e.Message.StartsWith("expired_access_token/"))
                {
                    _instance.RemoveExpiredDropBoxToken();
                    _instance = new DropBoxAuthenticator();
                }

                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, e.Message);
            }
        }

        public override async Task ListFilesAsync(string fileId = "")
        {
            try
            {
                ListFolderResult files = await _dropboxclient.Files.ListFolderAsync(fileId);
                GenerateFileItemLists(files);
            }
            catch (Exception e)
            {
                if (e.Message.StartsWith("expired_access_token/"))
                {
                    _instance.RemoveExpiredDropBoxToken();
                    _instance = new DropBoxAuthenticator();
                }

                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, e.Message);
            }
        }

        private void GenerateFileItemLists(ListFolderResult files)
        {
            if (files == null)
            {
                return;
            }

            _files = files
                .Entries.Select(file => new FilePickerItemViewModel()
                {
                    FileID = file.PathLower,
                    FileName = file.Name,
                    IsFolder = file.IsFolder,
                    FileExtension = Path.GetExtension(file.PathLower),
                    Source = FileProvider.DropBox,
                })
                .OrderByDescending(file => file.IsFolder)
                .ToList();
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

            long offset = destinationFileStream.Length;

            try
            {
                GetTemporaryLinkResult tempLink = await _dropboxclient.Files.GetTemporaryLinkAsync(fileItem.FileID);

                string downloadUrl = tempLink.Link;
                long fileSize = (long)tempLink.Metadata.Size;

                using HttpClient httpClient = new HttpClient();

                while (offset < fileSize)
                {
                    long end = Math.Min(offset + chunkFileSize - 1, fileSize - 1);
                    int retryCount = 0;

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
            catch
            {
                throw;
            }
        }

        public override async Task<MemoryStream> ReadFileStreamAsync(string fileId)
        {
            if (!New<IInternetState>().Connected)
            {
                return null;
            }

            try
            {
                Dropbox.Api.Stone.IDownloadResponse<FileMetadata> response = await _dropboxclient.Files.DownloadAsync(fileId);
                using Stream stream = await response.GetContentAsStreamAsync();
                return (MemoryStream)stream;
            }
            catch
            {
                throw;
            }
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
                string dropboxPath = NormalizeToCloudPath(actualFileItem.FileID) + fileName;
                return await UploadFileAsync(fileInfo, dropboxPath, WriteMode.Add.Instance, true);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /*
         *  Dropbox API does not allows to delete files permanantly. So overwrite the original file with random encrypted file.
         */

        public override async Task<bool> DeleteFileAsync(string originalFilePath, FilePickerItemViewModel fileItem, string encryptedFilePathForOverWrite,
            string newFileId = "", bool rename = false)
        {
            if (!New<IInternetState>().Connected)
            {
                return false;
            }

            string dropBoxFilePath = fileItem.FileID;
            try
            {
                if (encryptedFilePathForOverWrite != null)
                {
                    IDataStore randomlyEncryptedFile = GenerateRandomFile(encryptedFilePathForOverWrite, false);
                    await UploadFileAsync(randomlyEncryptedFile, dropBoxFilePath, WriteMode.Overwrite.Instance, false);

                    //string renamedFilePath = New<IDataStore>(dropBoxFilePath).Container.FullName + randomlyEncryptedFile.Name;

                    string originalCloudPath = dropBoxFilePath;

                    int lastSlash = originalCloudPath.LastIndexOf('/');
                    string dir = lastSlash > 0 ? originalCloudPath[..lastSlash] : "/";

                    string renamedFilePath = NormalizeToCloudPath(dropBoxFilePath) + randomlyEncryptedFile.Name;

                    if (await MoveFile(fileItem.FileID, renamedFilePath))
                    {
                        dropBoxFilePath = renamedFilePath;
                    }

                    randomlyEncryptedFile.Delete();
                }

                DeleteArg deleteArg = new DeleteArg(dropBoxFilePath);
                DeleteResult Result = await _dropboxclient.Files.DeleteV2Async(deleteArg);

                if (Result == null)
                {
                    return false;
                }

                if (string.IsNullOrEmpty(originalFilePath))
                {
                    return true;
                }

                IDataStore file = New<IDataStore>(originalFilePath);
                if (file != null && file.IsAvailable)
                {
                    WipeLocalFile(originalFilePath);
                }

                return true;
            }
            catch (ApiException<DeleteError>)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<bool> MoveFile(string fileId, string toPath)
        {
            try
            {
                RelocationArg relocationArg = new RelocationArg(fileId, toPath, false, true);
                RelocationResult result = await _dropboxclient.Files.MoveV2Async(relocationArg);

                if (result == null)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<string> UploadFileAsync(IDataStore fileInfo, string destinationPath, WriteMode writeMode, bool autoRename, CancellationToken ct = default)
        {
            if (fileInfo.Length() <= fileUploadLimit)
            {
                using Stream filecontent = fileInfo.OpenRead();
                UploadArg uploadArg = new UploadArg(destinationPath, writeMode, autoRename, DateTime.Now);
                FileMetadata result = await _dropboxclient.Files.UploadAsync(uploadArg, filecontent);
                if (result == null)
                {
                    return string.Empty;
                }

                return result.Id;
            }

            return await UploadLargeFileAsync(fileInfo, destinationPath, writeMode, autoRename, ct);
        }

        public async Task<string> UploadLargeFileAsync(IDataStore fileInfo, string destinationPath, WriteMode writeMode, bool autoRename, CancellationToken ct = default)
        {
            using Stream filecontent = fileInfo.OpenRead();
            byte[] buffer = new byte[chunkFileSize];

            int bytesRead;
            ulong offset = 0;
            string sessionId = null!;
            retryCount = 0;
            long totalLength = filecontent.Length;

            try
            {
                UploadSessionStartResult sessionStart = null!;
                while ((bytesRead = await filecontent.ReadAsync(buffer, 0, chunkFileSize, ct)) > 0)
                {
                    try
                    {
                        if (sessionStart == null)
                        {
                            using (MemoryStream memStream = new MemoryStream(buffer, 0, bytesRead))
                            {
                                sessionStart = await _dropboxclient.Files.UploadSessionStartAsync(body: memStream);
                                sessionId = sessionStart.SessionId;
                                offset += (ulong)bytesRead;
                                retryCount = 0;
                                continue;
                            }
                        }

                        UploadSessionCursor cursor = new UploadSessionCursor(sessionId, offset);

                        if (offset + (ulong)bytesRead < (ulong)totalLength)
                        {
                            using (MemoryStream memStream2 = new MemoryStream(buffer, 0, bytesRead))
                            {
                                await _dropboxclient.Files.UploadSessionAppendV2Async(cursor, body: memStream2);
                            }

                            offset += (ulong)bytesRead;
                            retryCount = 0;
                            continue;
                        }

                        using (MemoryStream memStreamFinal = new MemoryStream(buffer, 0, bytesRead))
                        {
                            CommitInfo commitInfo = new CommitInfo(destinationPath, writeMode, autoRename);
                            UploadSessionFinishArg finishArg = new UploadSessionFinishArg(cursor, commitInfo);

                            FileMetadata result = await _dropboxclient.Files.UploadSessionFinishAsync(finishArg, memStreamFinal);

                            return result.Id;
                        }
                    }
                    catch (Exception ex) when (IsNetworkError(ex))
                    {
                        retryCount++;
                        if (retryCount > maxRetries)
                            throw;

                        filecontent.Position = (long)offset;
                        await Task.Delay(2000, ct);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Upload failed: " + ex.Message, ex);
            }

            return string.Empty;
        }

        public override async Task<bool> UpdateFile(FilePickerItemViewModel cloudFileItem, IDataStore fileInfo, CancellationToken ct = default)
        {
            if (!New<IInternetState>().Connected)
            {
                return false;
            }

            try
            {
                string temCloudFolder = "/MyAxcryptTempFile_" + GenerateRandomFolderName();
                string dropboxPath = temCloudFolder + "/" + fileInfo.Name;
                string newFileId = await UploadFileAsync(fileInfo, dropboxPath, WriteMode.Add.Instance, true);

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

                if (!await MoveFile(dropboxPath, cloudFileItem.FileID))
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
                    DeleteArg deleteArg = new DeleteArg(temCloudFolder);
                    DeleteResult Result = await _dropboxclient.Files.DeleteV2Async(deleteArg);
                    return Result != null;
                }
                catch (Exception ex)
                {
                    await New<IPopup>().ShowAsync(
                        PopupButtons.Ok,
                        Texts.WarningTitle,
                        "Your file was uploaded successfully. However, there was a problem deleting the temporary folder created for the secure upload. Please remove the temporary folder manually.",
                        Common.DoNotShowAgainOptions.None
                    );

                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}