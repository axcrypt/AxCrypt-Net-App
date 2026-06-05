using AxCrypt.Abstractions;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.UI.ViewModels;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.App.Shared.ViewModels.Authentication;
using AxCrypt.Content;
using AxCrypt.Core.IO;
using AxCrypt.Core.UI;
using System.Diagnostics;
using System.Net.Http.Headers;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.CloudCore.iCloud
{
    internal class iCloudServices : FileStorageProvider
    {
        private const int MaxRetries = 3;

        private readonly iCloudAuthenticator _instance;
        private readonly IICloudPlatformFileAccess _iCloudPlatformFileAccess;

        private HttpClient? _httpClient;
        private OAuth2Auth? _oAuth2Authenticator;

        private int _retryCount = 0;

        private List<FilePickerItemViewModel> _files = new();

        private Action<FileStorageProvider> _initiateFilePickerAsync { get; set; } = _ => { };

        public override List<FilePickerItemViewModel> Files => _files;

        public override OAuth2Auth? OAuth2Authenticator => _oAuth2Authenticator;

        public override string PageTitle { get; } = Texts.KnownFolderNameICloud;

        public iCloudServices(Action<FileStorageProvider> initiateFilePicker) : this(new iCloudAuthenticator(), initiateFilePicker) { }

        public iCloudServices(iCloudAuthenticator instance, Action<FileStorageProvider> initiateFilePicker)
        {
            if (!New<IInternetState>().Connected)
            {
                New<IPopup>().ShowAsync(PopupButtons.OkCancel, Texts.WarningTitle,
                    $"{Texts.NoInternetErrorMessage}\n\nYou can continue to access your iCloud files, but changes will sync only after the device reconnects to the internet.");
            }

            _instance = instance;
            _iCloudPlatformFileAccess = New<IICloudPlatformFileAccess>();
            _oAuth2Authenticator = instance.Auth;

            if (_oAuth2Authenticator != null)
                _oAuth2Authenticator.Completed += async (sender, e) => await Presenter_Completed(sender, e);

            StartOAuthLoginPresenter();

            _initiateFilePickerAsync = initiateFilePicker;
        }

        private async void StartOAuthLoginPresenter()
        {
            await InitializeAsync();
        }

        private async Task InitializeAuth()
        {
            string message = $"AxCrypt needs your permission to access your {PageTitle} to open, encrypt, decrypt, and securely key share your files.\n" +
                $"Your privacy is our priority — we never store your files or share your data.\n\n" +
                $"Would you like to connect now?";

            PopupButtons popupResult = await New<IPopup>().ShowAsync(PopupButtons.OkCancel, $"Connect to {PageTitle}", message);

            if (popupResult == PopupButtons.Cancel) return;

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
            using (ProcessIndicator processIndicator = new ProcessIndicator())
            {
                await LoadiCloudFilesAsync();
            }
        }

        public async Task LoadiCloudFilesAsync()
        {
            await CleanupOrphanedFilesAsync();
            await LoadCloudContainerAsync();
            await ListFilesAsync();

            _initiateFilePickerAsync(this);
        }

        private async Task LoadCloudContainerAsync()
        {
            try
            {
                if (!_iCloudPlatformFileAccess.IsAvailable)
                {
                    await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle,
                        "iCloud Drive is not available on this device. " +
                        "Please ensure iCloud Drive is enabled in System Settings and " +
                        "the app is signed with the correct entitlements.");
                }
            }
            catch (Exception ex)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, $"Failed to initialise iCloud container: {ex.Message}");
            }
        }

        private async Task CleanupOrphanedFilesAsync()
        {
            try
            {
                string containerPath = _iCloudPlatformFileAccess.GetDocumentsContainerPath();

                foreach (string orphan in Directory.GetFiles(containerPath, "*.uploading", SearchOption.AllDirectories))
                {
                    try
                    {
                        string intended = orphan[..^".uploading".Length];

                        if (File.Exists(intended))
                            File.Delete(orphan);
                        else
                            File.Move(orphan, intended);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[iCloudServices] CleanupOrphanedFilesAsync (.uploading) error: {ex.Message}");
                    }
                }

                foreach (string pending in Directory.GetFiles(containerPath, "*.deletepending", SearchOption.AllDirectories))
                {
                    try
                    {
                        File.Delete(pending);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[iCloudServices] CleanupOrphanedFilesAsync (.deletepending) error: {ex.Message}");
                    }
                }

                foreach (string backup in Directory.GetFiles(containerPath, "*.backup", SearchOption.AllDirectories))
                {
                    try
                    {
                        string original = backup[..^".backup".Length];

                        if (!File.Exists(original))
                            File.Move(backup, original);
                        else
                            File.Delete(backup);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[iCloudServices] CleanupOrphanedFilesAsync (.backup) error: {ex.Message}");
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[iCloudServices] CleanupOrphanedFilesAsync error: {ex.Message}");
            }
        }

        public override async Task ListFilesAsync(string fileId = "")
        {
            try
            {
                List<FilePickerItemViewModel> items = await _iCloudPlatformFileAccess.GetItemsAsync(fileId);

                _files = items;
            }
            catch (Exception ex)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, ex.Message);
                throw;
            }
        }

        public override async Task CopyFileToImportedFiles(FilePickerItemViewModel fileItem, Stream destinationFileStream)
        {
            if (!New<IInternetState>().Connected)
                throw new InvalidOperationException("No Internet Access. Please check your internet connection.");

            if (fileItem == null) throw new ArgumentNullException(nameof(fileItem));
            if (destinationFileStream == null) throw new ArgumentNullException(nameof(destinationFileStream));

            try
            {
                await _iCloudPlatformFileAccess.EnsureFileDownloadedAsync(fileItem.FileID);

                using MemoryStream sourceStream = await ReadFileStreamAsync(fileItem.FileID);
                await sourceStream.CopyToAsync(destinationFileStream);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public override async Task<bool> UpdateFile(FilePickerItemViewModel cloudFileItem, IDataStore fileInfo, CancellationToken ct = default)
        {
            return await UpdateFileInternal(cloudFileItem, fileInfo, deleteOriginal: true, ct);
        }

        public async Task<bool> UpdateFileWithoutDelete(FilePickerItemViewModel cloudFileItem, IDataStore fileInfo, CancellationToken ct = default)
        {
            return await UpdateFileInternal(cloudFileItem, fileInfo, deleteOriginal: false, ct);
        }

        private async Task<bool> UpdateFileInternal(FilePickerItemViewModel cloudFileItem, IDataStore fileInfo, bool deleteOriginal, CancellationToken ct = default)
        {
            if (!New<IInternetState>().Connected) return false;

            try
            {
                if (iCloudConfiguration.SupportsNativeiCloudIntegration)
                {
                    string originalFileId = cloudFileItem.FileID;

                    string newFileId = await UploadFileToNativeContainerAsync(cloudFileItem, fileInfo.Name, fileInfo, ct);

                    if (string.IsNullOrEmpty(newFileId))
                    {
                        await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle,
                            "Your file was successfully encrypted, however there was a problem " +
                            "when moving the encrypted file. The encrypted file is not updated and try again.",
                            Common.DoNotShowAgainOptions.None);
                        return false;
                    }

                    if (deleteOriginal)
                    {
                        bool uploadedExists = await _iCloudPlatformFileAccess.WaitForFileAvailabilityAsync(newFileId);

                        if (!uploadedExists)
                        {
                            await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle,
                                "Your file was successfully encrypted, however there was a problem " +
                                "when deleting the original file. The original file is left untouched " +
                                "and needs to be removed manually.",
                                Common.DoNotShowAgainOptions.None);
                            return false;
                        }

                        if (!string.Equals(originalFileId, newFileId, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!await DeleteNativeFileAsync(originalFileId, cloudFileItem))
                            {
                                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle,
                                    "Updated file uploaded successfully, however the " +
                                    "original file could not be removed.",
                                    Common.DoNotShowAgainOptions.None);
                                return false;
                            }
                        }
                    }

                    cloudFileItem.FileID = newFileId;
                    return true;
                }

                InitializeHttpClient();
                _retryCount = 0;

                while (_retryCount < MaxRetries)
                {
                    try
                    {
                        using Stream fileStream = fileInfo.OpenRead();
                        using StreamContent content = new(fileStream);
                        content.Headers.ContentType = new MediaTypeHeaderValue(cloudFileItem.MimeType ?? "application/octet-stream");

                        string uploadUrl = $"{iCloudConfiguration.CloudKitAPIUrl}records/{cloudFileItem.FileID}/upload";
                        HttpResponseMessage response = await _httpClient!.PutAsync(uploadUrl, content, ct);

                        if (response.IsSuccessStatusCode) return true;

                        _retryCount++;
                        await Task.Delay(2000, ct);
                    }
                    catch (Exception ex) when (IsNetworkError(ex))
                    {
                        _retryCount++;
                        await Task.Delay(2000, ct);
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public override async Task<string> MoveFile(FilePickerItemViewModel fileItem, string fileName, IDataStore fileInfo)
        {
            if (!New<IInternetState>().Connected) return string.Empty;
            if (fileName == null) return string.Empty;

            if (fileInfo == null || !fileInfo.IsAvailable) return string.Empty;

            try
            {
                if (iCloudConfiguration.SupportsNativeiCloudIntegration)
                    return await UploadFileToNativeContainerAsync(fileItem, fileName, fileInfo);

                return await UploadFileToCloudKitAsync(fileItem, fileName, fileInfo);
            }
            catch
            {
                return string.Empty;
            }
        }

        public override async Task<bool> DeleteFileAsync(string fullFileName, FilePickerItemViewModel fileItem, string encryptedFilePathForOverWrite, string newFileId = "", bool rename = false)
        {
            if (!New<IInternetState>().Connected) return false;

            try
            {
                bool deleted;

                if (iCloudConfiguration.SupportsNativeiCloudIntegration)
                {
                    deleted = await DeleteNativeFileAsync(fullFileName, fileItem);
                }
                else
                {
                    InitializeHttpClient();
                    string deleteUrl = $"{iCloudConfiguration.CloudKitAPIUrl}records/{fileItem.FileID}";
                    HttpResponseMessage response = await _httpClient!.DeleteAsync(deleteUrl);
                    deleted = response.IsSuccessStatusCode;
                }

                if (deleted && !string.IsNullOrWhiteSpace(encryptedFilePathForOverWrite))
                {
                    IDataStore file = New<IDataStore>(encryptedFilePathForOverWrite);
                    if (file != null && file.IsAvailable)
                        WipeLocalFile(encryptedFilePathForOverWrite);
                }

                return deleted;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> UploadFileToNativeContainerAsync(FilePickerItemViewModel fileItem, string fileName, IDataStore fileInfo, CancellationToken ct = default)
        {
            try
            {
                string containerDocs = _iCloudPlatformFileAccess.GetDocumentsContainerPath();

                string targetDir = string.IsNullOrWhiteSpace(fileItem.ParentPath) ? containerDocs : fileItem.ParentPath;

                Directory.CreateDirectory(targetDir);

                string finalPath = Path.Combine(targetDir, fileName);
                string tempPath = finalPath + ".uploading";

                if (File.Exists(tempPath))
                    File.Delete(tempPath);

                using (Stream source = fileInfo.OpenRead())
                using (FileStream dest = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                {
                    await source.CopyToAsync(dest, ct);
                    await dest.FlushAsync(ct);
                }

                bool uploaded = await _iCloudPlatformFileAccess.WaitForFileAvailabilityAsync(tempPath);

                if (!uploaded)
                    return string.Empty;

                string? replaced = await _iCloudPlatformFileAccess.SafeReplaceFileAsync(finalPath, tempPath);

                if (string.IsNullOrWhiteSpace(replaced))
                    return string.Empty;

                bool verified = await _iCloudPlatformFileAccess.WaitForFileAvailabilityAsync(replaced);

                if (!verified)
                    return string.Empty;

                return replaced;
            }
            catch (Exception ex)
            {
                throw new Exception("Upload failed: " + ex.Message);
            }
        }

        private async Task<bool> DeleteNativeFileAsync(string pathToDelete, FilePickerItemViewModel fileItem)
        {
            string target = !string.IsNullOrWhiteSpace(fileItem.FileID) ? fileItem.FileID : pathToDelete;

            try
            {
                if (!File.Exists(target) && !Directory.Exists(target))
                    return true;

                if (Directory.Exists(target))
                {
                    Directory.Delete(target, recursive: true);
                    return true;
                }

                string pendingPath = target + ".deletepending";
                File.Move(target, pendingPath);

                File.Delete(pendingPath);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[iCloudServices] DeleteNativeFileAsync error: {ex.Message}");
                return false;
            }
        }

        private async Task<string> UploadFileToCloudKitAsync(FilePickerItemViewModel fileItem, string fileName, IDataStore fileInfo, CancellationToken ct = default)
        {
            InitializeHttpClient();
            _retryCount = 0;

            while (_retryCount < MaxRetries)
            {
                try
                {
                    using Stream fileStream = fileInfo.OpenRead();
                    fileStream.Position = 0;

                    using StreamContent content = new(fileStream);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    content.Headers.ContentLength = fileStream.Length;

                    string uploadUrl = $"{iCloudConfiguration.CloudKitAPIUrl}records/{fileItem.FileID}/upload";
                    HttpResponseMessage response = await _httpClient!.PutAsync(uploadUrl, content, ct);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseJson = await response.Content.ReadAsStringAsync(ct);
                        dynamic result = Serializer.Deserialize<dynamic>(responseJson);
                        return (string)(result?.recordName ?? string.Empty);
                    }

                    _retryCount++;
                    await Task.Delay(2000, ct);
                }
                catch (Exception ex) when (IsNetworkError(ex))
                {
                    _retryCount++;
                    await Task.Delay(2000, ct);
                }
            }

            return string.Empty;
        }

        public override async Task SearchFileFolderAsync(string query, string path = "")
        {
            try
            {
                List<FilePickerItemViewModel> items = await _iCloudPlatformFileAccess.SearchAsync(query, path);

                _files = items;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[iCloudServices] SearchFileFolderAsync error: {ex}");
            }
        }

        public override async Task<MemoryStream> ReadFileStreamAsync(string fileId)
        {
            await _iCloudPlatformFileAccess.EnsureFileDownloadedAsync(fileId);

            MemoryStream ms = new();

            using (FileStream fs = File.OpenRead(fileId))
                await fs.CopyToAsync(ms);

            ms.Position = 0;
            return ms;
        }

        private void InitializeHttpClient()
        {
            if (iCloudConfiguration.SupportsNativeiCloudIntegration) return;
            if (_httpClient != null) return;

            _httpClient = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            })
            {
                Timeout = TimeSpan.FromSeconds(iCloudConfiguration.RequestTimeoutSeconds)
            };

            if (_instance.CurrentAccessInfo?.AccessToken != null)
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_instance.CurrentAccessInfo.TokenType ?? "Bearer", _instance.CurrentAccessInfo.AccessToken);

            string serviceToken = iCloudConfiguration.CloudKitServiceToken;
            if (!string.IsNullOrWhiteSpace(serviceToken))
                _httpClient.DefaultRequestHeaders.Add("X-Apple-CloudKit-Request-KeyID", serviceToken);

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AxCrypt-iCloud/1.0");
        }

        public override async Task<ShareResult> ShareFileAsync(string fileId, ShareRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileId))
                    throw new ArgumentNullException(nameof(fileId));

                if (iCloudConfiguration.SupportsNativeiCloudIntegration)
                {
                    if (!File.Exists(fileId))
                    {
                        await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, "The selected file does not exist.");
                        return new ShareResult();
                    }

                    await _iCloudPlatformFileAccess.EnsureFileDownloadedAsync(fileId);
                    await _iCloudPlatformFileAccess.ShareFileAsync(fileId, request.Message);

                    return new ShareResult
                    {
                        ShareableLink = fileId,
                        PermissionSet = true,
                        RecipientEmailList = request.RecipientEmailList
                    };
                }

                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, "iCloud sharing is currently supported only on Apple devices.");
                return new ShareResult();
            }
            catch (Exception ex)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, $"Unable to share file: {ex.Message}");
                return new ShareResult();
            }
        }

        private static readonly string[] SUFFIXES = { "Bytes", "KB", "MB", "GB", "TB", "PB" };

        private string FormatSize(long? bytes)
        {
            if (bytes == null) return string.Empty;
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1) { number /= 1024; counter++; }
            return $"{number:n1}{SUFFIXES[counter]}";
        }

        public override async Task ListSharedWithFilesAsync()
        {
            await _iCloudPlatformFileAccess.GetSharedWithMeItemsAsync();
        }

        public override async Task ListSharedFilesAsync()
        {
            await _iCloudPlatformFileAccess.GetSharedByMeItemsAsync();
        }

        private static IStringSerializer Serializer => New<IStringSerializer>();
    }
}