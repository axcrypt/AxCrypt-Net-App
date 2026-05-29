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
using Dropbox.Api.Sharing;
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

        // retryCount is a local variable inside each upload method — keeping it as
        // an instance field was a thread-safety hazard when concurrent uploads run.

        private List<FilePickerItemViewModel> _files = new List<FilePickerItemViewModel>();

        public override List<FilePickerItemViewModel> Files => _files;

        private OAuth2Auth _oAuth2Authenticator;

        public override OAuth2Auth OAuth2Authenticator => _oAuth2Authenticator;

        public override string PageTitle => Texts.KnownFolderNameDropbox;

        private Action<FileStorageProvider> initiateFilePickerAsync { get; set; } = _ => { };

        public DropBoxServices(Action<FileStorageProvider> initiateFilePicker)
            : this(new DropBoxAuthenticator(), initiateFilePicker) { }

        public DropBoxServices(
            DropBoxAuthenticator instance,
            Action<FileStorageProvider> initiateFilePicker)
        {
            _instance = instance;
            _oAuth2Authenticator = instance.Auth!;
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
                $"AxCrypt needs your permission to access your {PageTitle} to open, encrypt, decrypt, and securely share your files.\n" +
                $"Your privacy is our priority — we never store your files or share your data.\n\nWould you like to connect now?";

            PopupButtons popupResult = await New<IPopup>().ShowAsync(PopupButtons.OkCancel, $"Connect to {PageTitle}", message);

            if (popupResult == PopupButtons.Cancel)
                return;

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
            await InitializeFilesAsync();
        }

        private async Task InitializeFilesAsync()
        {
            if (!New<IInternetState>().Connected || _instance.AccessToken == null)
                return;

            using (ProcessIndicator processIndicator = new ProcessIndicator())
            {
                await LoadDriveFilesAsync();
            }
        }

        public async Task LoadDriveFilesAsync()
        {
            if (!New<IInternetState>().Connected)
                return;

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

            DropboxClientConfig config = new DropboxClientConfig { HttpClient = httpClient };
            return new DropboxClient(_instance.AccessToken, config);
        }

        // ── Token expiry helper ────────────────────────────────────────────────
        // Several API calls can fail with expired_access_token. Centralise the
        // recovery so it doesn't have to be copy-pasted into every catch block.
        private void HandleExpiredToken(Exception ex)
        {
            if (ex.Message.StartsWith("expired_access_token/"))
            {
                _instance.RemoveExpiredDropBoxToken();
                _instance = new DropBoxAuthenticator();
            }
        }

        #region List files

        public override async Task ListFilesAsync(string fileId = "")
        {
            // Dropbox uses "" for root; callers may pass "root" as a sentinel.
            if (fileId == "root")
                fileId = "";

            try
            {
                ListFolderResult page = await _dropboxclient.Files.ListFolderAsync(fileId);
                List<Metadata> allEntries = new List<Metadata>(page.Entries);

                // Paginate — the original code only fetched the first page.
                while (page.HasMore)
                {
                    page = await _dropboxclient.Files.ListFolderContinueAsync(page.Cursor);
                    allEntries.AddRange(page.Entries);
                }

                _files = allEntries
                    .Select(file => new FilePickerItemViewModel
                    {
                        FileID = file.PathLower,
                        FileName = file.Name,
                        IsFolder = file.IsFolder,
                        FileExtension = Path.GetExtension(file.PathLower),
                        Source = FileProvider.DropBox,
                    })
                    .OrderByDescending(f => f.IsFolder)
                    .ToList();
            }
            catch (Exception e)
            {
                HandleExpiredToken(e);
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, e.Message);
            }
        }

        public override async Task ListSharedFilesAsync()
        {
            try
            {
                ListSharedLinksResult page = await _dropboxclient.Sharing.ListSharedLinksAsync();
                List<SharedLinkMetadata> allItems = new List<SharedLinkMetadata>(page.Links);

                while (page.HasMore)
                {
                    page = await _dropboxclient.Sharing.ListSharedLinksAsync(cursor: page.Cursor);
                    allItems.AddRange(page.Links);
                }

                _files = allItems
                    .OfType<FileLinkMetadata>()
                    .Where(file => IsAxCryptFile(file.Name))
                    .Select(file => new FilePickerItemViewModel
                    {
                        FileID = file.Id,
                        FileName = file.Name,
                        IsFolder = false,
                        FileExtension = Path.GetExtension(file.Name),
                        Source = FileProvider.DropBox,
                    })
                    .OrderBy(f => f.FileName)
                    .ToList();
            }
            catch (Exception e)
            {
                HandleExpiredToken(e);
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, e.Message);
            }
        }

        public override async Task ListSharedWithFilesAsync()
        {
            try
            {
                ListFilesResult page = await _dropboxclient.Sharing.ListReceivedFilesAsync(limit: 100);
                List<SharedFileMetadata> allItems = new List<SharedFileMetadata>(page.Entries);

                while (!string.IsNullOrEmpty(page.Cursor))
                {
                    page = await _dropboxclient.Sharing.ListReceivedFilesContinueAsync(page.Cursor);
                    allItems.AddRange(page.Entries);
                }

                _files = allItems
                    .Where(file => IsAxCryptFile(file.Name))
                    .Select(file => new FilePickerItemViewModel
                    {
                        FileID = file.Id,
                        FileName = file.Name,
                        IsFolder = false,
                        FileExtension = Path.GetExtension(file.Name),
                        Source = FileProvider.DropBox,
                    })
                    .OrderBy(f => f.FileName)
                    .ToList();
            }
            catch (Exception e)
            {
                HandleExpiredToken(e);
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, e.Message);
            }
        }

        #endregion

        #region Search

        public override async Task SearchFileFolderAsync(string query, string path)
        {
            if (path == "root")
                path = "";

            try
            {
                SearchV2Arg searchQuery = new SearchV2Arg(
                    query: query,
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

                _files = allMatches
                    .Select(m => new FilePickerItemViewModel
                    {
                        FileID = m.Metadata.AsMetadata.Value.PathLower,
                        FileName = m.Metadata.AsMetadata.Value.Name,
                        IsFolder = m.Metadata.AsMetadata.Value.IsFolder,
                        FileExtension = Path.GetExtension(m.Metadata.AsMetadata.Value.PathLower),
                        Source = FileProvider.DropBox,
                    })
                    .ToList();
            }
            catch (Exception e)
            {
                HandleExpiredToken(e);
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, e.Message);
            }
        }

        #endregion

        #region Download

        public override async Task CopyFileToImportedFiles(FilePickerItemViewModel fileItem, Stream destinationFileStream)
        {
            if (!New<IInternetState>().Connected)
                throw new InvalidOperationException("No Internet access, please check your internet connection.");

            if (fileItem == null)
                throw new ArgumentNullException(nameof(fileItem));

            if (destinationFileStream == null)
                throw new ArgumentNullException(nameof(destinationFileStream));

            GetTemporaryLinkResult tempLink = await _dropboxclient.Files.GetTemporaryLinkAsync(fileItem.FileID);
            string downloadUrl = tempLink.Link;
            long fileSize = (long)tempLink.Metadata.Size;
            long offset = destinationFileStream.Length;

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
                        if (retryCount > maxRetries) throw;
                        await Task.Delay(2000);
                    }
                }
            }
        }

        public override async Task<MemoryStream> ReadFileStreamAsync(string fileId)
        {
            if (!New<IInternetState>().Connected)
                return null!;

            // GetContentAsStreamAsync returns a generic Stream — it is NOT guaranteed
            // to be a MemoryStream, so the previous direct cast could throw. Copy into
            // a fresh MemoryStream and reset the position for the caller.
            Dropbox.Api.Stone.IDownloadResponse<FileMetadata> response =
                await _dropboxclient.Files.DownloadAsync(fileId);

            MemoryStream ms = new MemoryStream();
            using Stream stream = await response.GetContentAsStreamAsync();
            await stream.CopyToAsync(ms);
            ms.Position = 0;
            return ms;
        }

        #endregion

        #region Upload / Move

        public override async Task<string> MoveFile(FilePickerItemViewModel actualFileItem, string fileName, IDataStore fileInfo)
        {
            if (!New<IInternetState>().Connected || fileName == null || fileInfo == null || !fileInfo.IsAvailable)
                return string.Empty;

            try
            {
                string dropboxPath = NormalizeToCloudPath(actualFileItem.FileID) + fileName;
                return await UploadFileAsync(fileInfo, dropboxPath, WriteMode.Add.Instance, autoRename: true);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public async Task<string> UploadFileAsync(IDataStore fileInfo, string destinationPath, WriteMode writeMode, bool autoRename, CancellationToken ct = default)
        {
            if (fileInfo.Length() <= fileUploadLimit)
            {
                using Stream filecontent = fileInfo.OpenRead();
                UploadArg uploadArg = new UploadArg(destinationPath, writeMode, autoRename, DateTime.Now);
                FileMetadata result = await _dropboxclient.Files.UploadAsync(uploadArg, filecontent);
                return result?.Id ?? string.Empty;
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
            int retryCount = 0;          // local — safe for concurrent uploads
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
                            using MemoryStream memStream = new MemoryStream(buffer, 0, bytesRead);
                            sessionStart = await _dropboxclient.Files.UploadSessionStartAsync(body: memStream);
                            sessionId = sessionStart.SessionId;
                            offset += (ulong)bytesRead;
                            retryCount = 0;
                            continue;
                        }

                        UploadSessionCursor cursor = new UploadSessionCursor(sessionId, offset);

                        if (offset + (ulong)bytesRead < (ulong)totalLength)
                        {
                            using MemoryStream memStream = new MemoryStream(buffer, 0, bytesRead);
                            await _dropboxclient.Files.UploadSessionAppendV2Async(cursor, body: memStream);
                            offset += (ulong)bytesRead;
                            retryCount = 0;
                            continue;
                        }

                        using MemoryStream memStreamFinal = new MemoryStream(buffer, 0, bytesRead);
                        CommitInfo commitInfo = new CommitInfo(destinationPath, writeMode, autoRename);
                        UploadSessionFinishArg finishArg = new UploadSessionFinishArg(cursor, commitInfo);
                        FileMetadata result = await _dropboxclient.Files.UploadSessionFinishAsync(finishArg, memStreamFinal);
                        return result.Id;
                    }
                    catch (Exception ex) when (IsNetworkError(ex))
                    {
                        retryCount++;
                        if (retryCount > maxRetries) throw;
                        filecontent.Position = (long)offset;
                        await Task.Delay(2000, ct);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Upload failed: " + ex.Message, ex);
            }

            return string.Empty;
        }

        #endregion

        #region Delete

        /*
         * Dropbox does not allow files to be permanently deleted via the API
         * in the same way as local storage. The approach here is:
         *   1. Overwrite with a randomly encrypted file so the original content
         *      is unrecoverable even from Dropbox version history.
         *   2. Rename (obfuscate the filename).
         *   3. Delete the resulting entry.
         */
        public override async Task<bool> DeleteFileAsync(string originalFilePath, FilePickerItemViewModel fileItem, string encryptedFilePathForOverWrite,
            string newFileId = "", bool rename = false)
        {
            if (!New<IInternetState>().Connected)
                return false;

            string dropBoxFilePath = fileItem.FileID;
            try
            {
                if (encryptedFilePathForOverWrite != null)
                {
                    IDataStore randomlyEncryptedFile = GenerateRandomFile(encryptedFilePathForOverWrite, false);
                    await UploadFileAsync(randomlyEncryptedFile, dropBoxFilePath, WriteMode.Overwrite.Instance, autoRename: false);

                    string renamedFilePath = NormalizeToCloudPath(dropBoxFilePath) + randomlyEncryptedFile.Name;
                    if (await MoveFile(fileItem.FileID, renamedFilePath))
                        dropBoxFilePath = renamedFilePath;

                    randomlyEncryptedFile.Delete();
                }

                DeleteArg deleteArg = new DeleteArg(dropBoxFilePath);
                DeleteResult result = await _dropboxclient.Files.DeleteV2Async(deleteArg);

                if (result == null)
                    return false;

                if (!string.IsNullOrEmpty(originalFilePath))
                {
                    IDataStore file = New<IDataStore>(originalFilePath);
                    if (file != null && file.IsAvailable)
                        WipeLocalFile(originalFilePath);
                }

                // Refresh the file list so the deletion is reflected immediately.
                await LoadDriveFilesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<bool> MoveFile(string fileId, string toPath)
        {
            try
            {
                RelocationArg relocationArg = new RelocationArg(fileId, toPath, allowOwnershipTransfer: false, autorename: true);
                RelocationResult result = await _dropboxclient.Files.MoveV2Async(relocationArg);
                return result != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dropbox move failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Update (re-encrypt in place)

        public override async Task<bool> UpdateFile(FilePickerItemViewModel cloudFileItem, IDataStore fileInfo, CancellationToken ct = default)
        {
            if (!New<IInternetState>().Connected)
                return false;

            try
            {
                string tempFolder = "/MyAxcryptTempFile_" + GenerateRandomFolderName();
                string dropboxPath = tempFolder + "/" + fileInfo.Name;
                string newFileId = await UploadFileAsync(fileInfo, dropboxPath, WriteMode.Add.Instance, autoRename: true);

                if (string.IsNullOrEmpty(newFileId))
                {
                    await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle,
                        "Your file was successfully encrypted, however there was a problem when moving the encrypted file. The encrypted file is not updated — please try again.",
                        Common.DoNotShowAgainOptions.None);
                    return false;
                }

                if (!await DeleteFileAsync(fileInfo.FullName, cloudFileItem, fileInfo.FullName))
                {
                    await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle,
                        "Your file was successfully encrypted, however there was a problem when deleting the original file. The original file is left untouched and needs to be removed manually.",
                        Common.DoNotShowAgainOptions.None);
                    return false;
                }

                if (!await MoveFile(dropboxPath, cloudFileItem.FileID))
                {
                    await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle,
                        "The file was uploaded to the temporary folder successfully. However, it could not be moved to the destination folder. The file remains in the temporary folder and must be moved manually.",
                        Common.DoNotShowAgainOptions.None);
                    return false;
                }

                try
                {
                    DeleteResult deleteResult = await _dropboxclient.Files.DeleteV2Async(new DeleteArg(tempFolder));
                    cloudFileItem.FileID = newFileId;

                    // Refresh so the UI reflects the updated file immediately.
                    await LoadDriveFilesAsync();
                    return deleteResult != null;
                }
                catch (Exception)
                {
                    await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle,
                        "Your file was uploaded successfully. However, there was a problem deleting the temporary folder. Please remove the temporary folder manually.",
                        Common.DoNotShowAgainOptions.None);
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Share

        public override async Task<ShareResult> ShareFileAsync(string filePath, ShareRequest request)
        {
            try
            {
                await _dropboxclient.Sharing.AddFileMemberAsync(
                    new AddFileMemberArgs(
                        file: filePath,
                        members: request.RecipientEmailList.Select(email => new MemberSelector.Email(email)),
                        accessLevel: request.Permission == SharePermission.Editor
                            ? AccessLevel.Editor.Instance
                            : AccessLevel.Viewer.Instance,
                        quiet: false,
                        customMessage: request.Message));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dropbox share member error: {ex.Message}");
                throw;
            }

            RequestedVisibility visibility = request.LinkType == ShareLinkType.TeamOnly
                ? (RequestedVisibility)RequestedVisibility.TeamOnly.Instance
                : RequestedVisibility.Public.Instance;

            SharedLinkMetadata linkResult = await _dropboxclient.Sharing.CreateSharedLinkWithSettingsAsync(
                new CreateSharedLinkWithSettingsArg(
                    path: filePath,
                    settings: new SharedLinkSettings(requestedVisibility: visibility)));

            return new ShareResult
            {
                ShareableLink = linkResult.Url,
                PermissionSet = true,
                RecipientEmailList = request.RecipientEmailList
            };
        }

        #endregion
    }
}
