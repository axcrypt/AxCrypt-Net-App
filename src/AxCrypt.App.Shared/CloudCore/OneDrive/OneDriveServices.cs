using AxCrypt.Abstractions;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.UI.ViewModels;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.App.Shared.ViewModels.Authentication;
using AxCrypt.Content;
using AxCrypt.Core.IO;
using AxCrypt.Core.UI;
using Azure.Core;
using Microsoft.Graph;
using Microsoft.Graph.Drives.Item.Items.Item.CreateLink;
using Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;
using Microsoft.Graph.Drives.Item.Items.Item.Invite;
using Microsoft.Graph.Drives.Item.Items.Item.SearchWithQ;
using Microsoft.Graph.Drives.Item.Root;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.CloudCore.OneDrive
{
    public class OneDriveServices : FileStorageProvider
    {
        private OneDriveAuthenticator _instance;
        private GraphServiceClient _graphClient;

        private List<FilePickerItemViewModel> _files = new List<FilePickerItemViewModel>();

        private readonly string RootFolderName = "root";
        private string? _userDriveId;

        private int chunkFileSize = OneDriveConfiguration.ChunkFileSize;
        private const int maxRetries = 3;

        // retryCount was an instance field — moved to a local variable inside each
        // method that needs it so concurrent operations can't corrupt each other.

        public override List<FilePickerItemViewModel> Files => _files;

        private OAuth2Auth? _oAuth2Authenticator;

        public override OAuth2Auth OAuth2Authenticator => _oAuth2Authenticator;

        public override string PageTitle => Texts.KnownFolderNameOneDrive;

        private Action<FileStorageProvider> initiateFilePickerAsync { get; set; } = _ => { };

        public OneDriveServices(Action<FileStorageProvider> initiateFilePicker)
            : this(new OneDriveAuthenticator(), initiateFilePicker) { }

        public OneDriveServices(
            OneDriveAuthenticator instance,
            Action<FileStorageProvider> initiateFilePicker)
        {
            if (!New<IInternetState>().Connected)
            {
                throw new InvalidOperationException(
                    "No Internet access, please check your internet connection.");
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

            await LoadCloudDriveAsync();
            await ListFilesAsync();
            initiateFilePickerAsync(this);
        }

        private async Task LoadCloudDriveAsync()
        {
            _graphClient = GetAuthenticatedClient();
            Drive? driveInfo = await _graphClient.Me.Drive.GetAsync();
            _userDriveId = driveInfo!.Id;
        }

        #region List files

        public override async Task ListFilesAsync(string folderId = "")
        {
            if (!New<IInternetState>().Connected)
                return;

            try
            {
                IList<DriveItem> files = await GetDriveFilesAsync(folderId);
                if (files == null)
                    return;

                _files = files.Select(file => new FilePickerItemViewModel
                {
                    FileID = file.Id!,
                    FileName = file.Name!,
                    IsFolder = file.Folder != null,
                    MimeType = file.File?.MimeType!,
                    FileExtension = file.Folder != null ? "" : Path.GetExtension(file.Name)!,
                    ParentPath = file.ParentReference?.Path ?? "",
                    Source = FileProvider.OneDrive,
                }).OrderByDescending(f => f.IsFolder).ToList();
            }
            catch (Exception ex)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle,
                    $"Failed to load drive files: {ex.Message}. Please try again.");
            }
        }

        public override async Task ListSharedFilesAsync()
        {
            try
            {
                // Always start from a clean list to avoid accumulating duplicates
                // across repeated calls.
                List<FilePickerItemViewModel> result = new();

                DriveItemCollectionResponse? driveItems = await _graphClient.Drives[_userDriveId].Items["root"].Children
                    .GetAsync(config =>
                    {
                        config.QueryParameters.Select = new[]
                        {
                            "id", "name", "file", "folder", "size", "parentReference", "shared"
                        };
                    });

                foreach (DriveItem item in driveItems?.Value ?? [])
                {
                    if (item.Shared != null && IsAxCryptFile(item.Name!))
                    {
                        result.Add(new FilePickerItemViewModel
                        {
                            FileID = item.Id!,
                            FileName = item.Name!,
                            IsFolder = item.Folder != null,
                            MimeType = item.File?.MimeType!,
                            FileExtension = item.Folder != null ? "" : Path.GetExtension(item.Name ?? ""),
                            ParentPath = item.ParentReference?.Path ?? "",
                            Source = FileProvider.OneDrive,
                        });
                    }
                }

                _files = result;
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
                // Use the cached drive ID rather than making a redundant API call.
                Microsoft.Graph.Drives.Item.SharedWithMe.SharedWithMeGetResponse? result =
                    await _graphClient.Drives[_userDriveId].SharedWithMe.GetAsSharedWithMeGetResponseAsync();

                if (result?.Value == null)
                    return;

                _files = result.Value
                    .Where(file => file.RemoteItem != null && IsAxCryptFile(file.RemoteItem.Name!))
                    .Select(file => new FilePickerItemViewModel
                    {
                        FileID = file.RemoteItem!.Id!,
                        FileName = file.RemoteItem.Name!,
                        IsFolder = file.RemoteItem.Folder != null,
                        MimeType = file.RemoteItem.File?.MimeType!,
                        FileExtension = file.RemoteItem.Folder != null ? "" : Path.GetExtension(file.RemoteItem.Name!),
                        ParentPath = file.RemoteItem.ParentReference?.Path ?? "",
                        Source = FileProvider.OneDrive,
                    })
                    .OrderByDescending(f => f.IsFolder)
                    .ToList();
            }
            catch (Exception e)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, e.Message);
            }
        }

        private async Task<IList<DriveItem>> GetDriveFilesAsync(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                folder = RootFolderName;

            if (_userDriveId == null)
                return Array.Empty<DriveItem>();

            DriveItemCollectionResponse? response = await _graphClient
                .Drives[_userDriveId].Items[folder].Children.GetAsync();

            return (response?.Value != null ? response?.Value : Array.Empty<DriveItem>())!;
        }

        #endregion

        #region Search

        public override async Task SearchFileFolderAsync(string query, string path)
        {
            string searchText = query.Trim();
            if (string.IsNullOrEmpty(searchText))
                return;

            string searchQuery = Uri.EscapeDataString(searchText);

            try
            {
                SearchWithQGetResponse? searchResult = await _graphClient
                    .Drives[_userDriveId!].Items[path].SearchWithQ(searchQuery)
                    .GetAsSearchWithQGetResponseAsync(config =>
                    {
                        config.QueryParameters.Select = new[] { "id", "name", "folder", "file", "parentReference" };
                        config.QueryParameters.Top = 100;
                    });

                List<DriveItem> allItems = new();

                while (searchResult != null)
                {
                    if (searchResult.Value != null)
                        allItems.AddRange(searchResult.Value);

                    if (string.IsNullOrEmpty(searchResult.OdataNextLink))
                        break;

                    searchResult = await _graphClient.Drives[_userDriveId!].Items["root"]
                        .SearchWithQ(searchQuery)
                        .WithUrl(searchResult.OdataNextLink!)
                        .GetAsSearchWithQGetResponseAsync();
                }

                _files = allItems
                    .Where(item => !string.IsNullOrEmpty(item.Name) &&
                                   item.Root == null &&
                                   Path.GetFileNameWithoutExtension(item.Name)
                                       .Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    .Select(item => new FilePickerItemViewModel
                    {
                        FileID = item.Id!,
                        FileName = item.Name!,
                        IsFolder = item.Folder != null,
                        FileExtension = Path.GetExtension(item.Name!),
                        Source = FileProvider.OneDrive,
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, ex.Message);
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

            DriveItem? item = await _graphClient.Drives[_userDriveId].Items[fileItem.FileID].GetAsync();

            if (item == null)
                throw new Exception("File not found in OneDrive.");

            if (!item.AdditionalData.TryGetValue("@microsoft.graph.downloadUrl", out object? downloadUrlObj) || downloadUrlObj is null)
                throw new Exception("Download URL missing.");

            string downloadUrl = downloadUrlObj.ToString()!;
            long fileSize = item.Size ?? throw new Exception("File size missing.");
            long offset = 0;

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
            if (fileId == null || !New<IInternetState>().Connected)
                return null!;

            // Do NOT wrap in `using` — the MemoryStream must remain open for the caller.
            MemoryStream ms = new MemoryStream();
            using Stream? stream = await _graphClient.Drives[_userDriveId].Items[fileId].Content.GetAsync();
            await stream!.CopyToAsync(ms);
            ms.Position = 0;
            return ms;
        }

        #endregion

        #region Upload / Move

        public override async Task<string> MoveFile(FilePickerItemViewModel actualFileItem, string fileName, IDataStore fileInfo)
        {
            try
            {
                return await UploadFileAsync(actualFileItem, fileName, fileInfo);
            }
            catch (HttpRequestException e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            return string.Empty;
        }

        private async Task<string> UploadFileAsync(FilePickerItemViewModel actualFileItem, string fileName, IDataStore fileInfo, bool overwrite = false)
        {
            UploadSession? uploadSession = await CreateUploadSession(actualFileItem, fileName, fileInfo, overwrite);

            const int maxSliceSize = 10 * 1024 * 1024;

            using Stream fileStream = fileInfo.OpenRead();
            long totalLength = fileStream.Length;

            LargeFileUploadTask<DriveItem> fileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, fileStream, maxSliceSize);

            IProgress<long> progress = new Progress<long>(prog =>
                System.Diagnostics.Debug.WriteLine($"Uploaded {prog} of {totalLength} bytes"));

            UploadResult<DriveItem>? uploadResult = null;

            for (int retryCount = 0; retryCount < maxRetries; retryCount++)
            {
                try
                {
                    uploadResult = retryCount == 0
                        ? await fileUploadTask.UploadAsync(progress)
                        : await fileUploadTask.ResumeAsync();
                    break;
                }
                catch (ServiceException ex) when (IsNetworkError(ex))
                {
                    if (retryCount == maxRetries - 1) throw;
                    await Task.Delay(2000);
                }
            }

            return uploadResult?.UploadSucceeded == true
                ? uploadResult.ItemResponse.Id!
                : string.Empty;
        }

        private async Task<UploadSession> CreateUploadSession(FilePickerItemViewModel actualFileItem, string fileName, IDataStore fileInfo, bool overwrite = false)
        {
            string conflictBehavior = overwrite ? "replace" : "rename";

            CreateUploadSessionPostRequestBody uploadProps = new CreateUploadSessionPostRequestBody
            {
                Item = new DriveItemUploadableProperties
                {
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "@microsoft.graph.conflictBehavior", conflictBehavior }
                    }
                }
            };

            if (overwrite)
            {
                return (await _graphClient.Drives[_userDriveId].Items[actualFileItem.FileID]
                    .CreateUploadSession.PostAsync(uploadProps))!;
            }

            if (string.IsNullOrEmpty(actualFileItem.ParentPath))
            {
                return (await _graphClient.Drives[_userDriveId].Root
                    .ItemWithPath(fileName).CreateUploadSession.PostAsync(uploadProps))!;
            }

            string folderPath = actualFileItem.ParentPath.TrimEnd('/') + "/";
            return (await _graphClient.Drives[_userDriveId].Items[RootFolderName]
                .ItemWithPath(folderPath + fileName).CreateUploadSession.PostAsync(uploadProps))!;
        }

        #endregion

        #region Delete

        public override async Task<bool> DeleteFileAsync(string originalFilePath, FilePickerItemViewModel fileItem, string encryptedFilePathForOverWrite,
            string newFileId = "", bool rename = false)
        {
            if (!New<IInternetState>().Connected)
                return false;

            string oneDriveFileId = fileItem.FileID;
            try
            {
                if (encryptedFilePathForOverWrite != null)
                {
                    IDataStore randomlyEncryptedFile = GenerateRandomFile(encryptedFilePathForOverWrite, false);
                    await UploadFileAsync(fileItem, originalFilePath, randomlyEncryptedFile, overwrite: true);
                    randomlyEncryptedFile.Delete();
                }

                // Wipe local copy if it exists.
                if (!string.IsNullOrEmpty(originalFilePath))
                {
                    IDataStore file = New<IDataStore>(originalFilePath);
                    if (file != null && file.IsAvailable)
                        WipeLocalFile(originalFilePath);
                }

                if (string.IsNullOrEmpty(oneDriveFileId))
                    throw new ArgumentException("The fileId cannot be empty.");

                return await DeleteItemAsync(oneDriveFileId);
            }
            catch (HttpRequestException e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
                return false;
            }
        }

        // Renamed from the overloaded DeleteFileAsync(string) to avoid hiding the base method.
        private async Task<bool> DeleteItemAsync(string itemId)
        {
            await _graphClient.Drives[_userDriveId].Items[itemId].DeleteAsync();
            return true;
        }

        #endregion

        #region Update (re-encrypt in place)

        public override async Task<bool> UpdateFile(FilePickerItemViewModel cloudFileItem, IDataStore fileInfo, CancellationToken ct = default)
        {
            if (!New<IInternetState>().Connected)
                return false;

            try
            {
                string actualParentPath = cloudFileItem.ParentPath;
                cloudFileItem.ParentPath = "/MyAxcryptTempFile_" + GenerateRandomFolderName();

                string newFileId = await UploadFileAsync(cloudFileItem, fileInfo.Name, fileInfo);

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

                // Resolve the destination folder.
                string normalizedPath = actualParentPath?.Trim('/') ?? "";
                
                RootRequestBuilder driveRoot = _graphClient.Drives[_userDriveId].Root;

                DriveItem? destinationFolder = ResolveDestinationPath(normalizedPath) is { } resolvedPath
                ? await driveRoot.ItemWithPath(resolvedPath).GetAsync()
                : await driveRoot.GetAsync();

                DriveItem? tempFolder = await _graphClient.Drives[_userDriveId].Root
                    .ItemWithPath(cloudFileItem.ParentPath).GetAsync();

                if (tempFolder?.Id == null)
                    return false;

                if (!await MoveFile(newFileId, destinationFolder!.Id!))
                {
                    await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle,
                        "The file was uploaded to the temporary folder successfully. However, it could not be moved to the destination folder. The file remains in the temporary folder and must be moved manually.",
                        Common.DoNotShowAgainOptions.None);
                    return false;
                }

                try
                {
                    await _graphClient.Drives[_userDriveId].Items[tempFolder.Id].DeleteAsync(cancellationToken: ct);
                    cloudFileItem.FileID = newFileId;

                    return true;
                }
                catch (Exception)
                {
                    await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle,
                        "Your file was uploaded successfully. However, there was a problem deleting the temporary folder. Please remove the temporary folder manually.",
                        Common.DoNotShowAgainOptions.None);
                    return false;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
                return false;
            }
        }

        #endregion

        #region Share

        public override async Task<ShareResult> ShareFileAsync(string fileId, ShareRequest request)
        {
            try
            {
                InvitePostRequestBody inviteBody = new InvitePostRequestBody
                {
                    Recipients = request.RecipientEmailList
                        .Select(email => new DriveRecipient { Email = email })
                        .ToList(),
                    Roles = new List<string> { request.Permission == SharePermission.Editor ? "write" : "read" },
                    SendInvitation = true,
                    Message = request.Message
                };

                await _graphClient.Drives[_userDriveId].Items[fileId].Invite.PostAsInvitePostResponseAsync(inviteBody);
            }
            catch (ODataError odataError) when (
                odataError.Message?.Contains("NoResolvedUsers", StringComparison.OrdinalIgnoreCase) == true)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, "Invalid Recipients",
                    "One or more recipients do not have a OneDrive account. Please verify the email addresses and try again.");

                return new ShareResult
                {
                    ShareableLink = string.Empty,
                    PermissionSet = false,
                    RecipientEmailList = Enumerable.Empty<string>()
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                throw;
            }

            CreateLinkPostRequestBody linkBody = new CreateLinkPostRequestBody
            {
                Type = request.Permission == SharePermission.Editor ? "edit" : "view",
                Scope = request.LinkType == ShareLinkType.TeamOnly ? "organization" : "anonymous"
            };

            Permission? linkResult = await _graphClient.Drives[_userDriveId].Items[fileId].CreateLink.PostAsync(linkBody);

            return new ShareResult
            {
                ShareableLink = linkResult?.Link?.WebUrl!,
                PermissionSet = true,
                RecipientEmailList = request.RecipientEmailList
            };
        }

        #endregion

        #region Helpers

        private static string? ResolveDestinationPath(string? normalizedPath)
        {
            if (string.IsNullOrEmpty(normalizedPath) ||
                normalizedPath.Equals("root", StringComparison.OrdinalIgnoreCase))
                return null;

            if (normalizedPath.StartsWith("drives/", StringComparison.OrdinalIgnoreCase))
            {
                int rootIndex = normalizedPath.IndexOf("root:", StringComparison.OrdinalIgnoreCase);
                if (rootIndex >= 0)
                {
                    normalizedPath = normalizedPath[(rootIndex + "root:".Length)..].TrimStart('/');
                    return string.IsNullOrEmpty(normalizedPath) ? null : normalizedPath;
                }
            }

            return normalizedPath;
        }

        public async Task<bool> MoveFile(string fileId, string folderId)
        {
            try
            {
                DriveItem moveItem = new DriveItem
                {
                    ParentReference = new ItemReference { Id = folderId }
                };

                DriveItem? movedFile = await _graphClient.Drives[_userDriveId].Items[fileId].PatchAsync(moveItem);
                return movedFile != null;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OneDrive move failed: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> Rename(string fileId, string fullFilePath)
        {
            if (string.IsNullOrEmpty(fileId))
                return false;

            DriveItem? driveFile = await _graphClient.Drives[_userDriveId].Items[fileId].GetAsync();
            if (driveFile == null)
                return false;

            IDataStore newFileStore = New<IDataStore>(fullFilePath);
            if (driveFile.Name == newFileStore.Name)
                return false;

            FilePickerItemViewModel fileItem = new FilePickerItemViewModel
            {
                FileID = driveFile.Id!,
                FileName = driveFile.Name!,
                IsFolder = driveFile.Folder != null,
                MimeType = driveFile.File?.MimeType!,
                FileExtension = driveFile.Folder != null ? "" : Path.GetExtension(driveFile.Name)!,
                ParentPath = driveFile.ParentReference?.Path ?? "",
                Source = FileProvider.OneDrive,
            };

            string newFileName = await TempFileNameAsync(newFileStore.Name, fileItem);
            if (driveFile.Name == newFileName)
                return false;

            await _graphClient.Drives[_userDriveId].Items[fileId]
                .PatchAsync(new DriveItem { Name = newFileName });

            return true;
        }

        /// <summary>
        /// Returns a filename that does not already exist at the item's parent path,
        /// appending an incrementing counter suffix when needed.
        /// Recursive — counter and originalName are threading state passed through the
        /// call chain rather than stored as instance fields (which was not thread-safe).
        /// </summary>
        private async Task<string> TempFileNameAsync(
            string fileName,
            FilePickerItemViewModel? fileItem = null,
            string? fileExtension = null,
            int counter = 0,
            string? originalName = null)
        {
            // Resolve to what path we'll test for existence.
            string filePath = fileItem != null
                ? fileItem.ParentPath.TrimEnd('/') + "/" + fileName
                : fileName;

            DriveItem? driveFile;
            try
            {
                // Re-use the existing authenticated client instead of creating a new one.
                driveFile = await _graphClient.Drives[_userDriveId].Root.ItemWithPath(filePath).GetAsync();
            }
            catch (Exception)
            {
                // File does not exist — this name is free.
                return fileName;
            }

            if (driveFile == null)
                return fileName;

            // Determine base name and extension once, then recurse with incremented counter.
            originalName ??= fileName;
            fileExtension ??= Path.GetExtension(driveFile.Name)!;
            string baseName = Path.GetFileNameWithoutExtension(originalName);
            string suffix = counter > 0 ? $"({counter}){fileExtension}" : fileExtension;

            return await TempFileNameAsync(
                $"{baseName}{suffix}",
                fileItem,
                fileExtension,
                counter + 1,
                originalName);
        }

        private GraphServiceClient GetAuthenticatedClient()
        {
            CustomTokenCredential tokenCredential = new CustomTokenCredential(
                _instance.AccessToken!,
                _instance.AccessTokenExpireOffset);

            return new GraphServiceClient(tokenCredential);
        }

        #endregion
    }

    public class CustomTokenCredential : TokenCredential
    {
        private readonly AccessToken _accessToken;

        public CustomTokenCredential(string accessToken, DateTimeOffset accessTokenExpireOffset)
        {
            _accessToken = new AccessToken(accessToken, accessTokenExpireOffset);
        }

        public override AccessToken GetToken(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken) => _accessToken;

        public override ValueTask<AccessToken> GetTokenAsync(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken) => ValueTask.FromResult(_accessToken);
    }
}
