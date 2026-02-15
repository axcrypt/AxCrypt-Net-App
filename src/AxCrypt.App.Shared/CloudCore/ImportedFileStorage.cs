using AxCrypt.Abstractions;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Core;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.CloudCore
{
    /// <summary>
    /// Represents the long-term storage for files, which are imported in application (via document picker in import mode, or via file type association).
    /// </summary>
    public class ImportedFileStorage
    {
        private const string ImportedFilesDirectory = "ImportedFiles";

        private string _applicationDirectoryPath;

        private List<string> _filesAddedInCurrentSession = new List<string>();

        private bool _isCleanupAlreadyInvoked;

        public ImportedFileStorage(string applicationDirectoryPath)
        {
            _applicationDirectoryPath = applicationDirectoryPath;
        }

        public Task<FileOperationContext> CopyFileToImportedFiles(string filePath)
        {
            IDataStore importedFile = New<IDataStore>(filePath);
            return CopyFileToImportedFiles(importedFile);
        }

        public Task<FileOperationContext> CopyFileToImportedFiles(IDataStore importedFile)
        {
            return Task.Run(() =>
            {
                New<WorkFolder>().FileInfo.CreateFolder(ImportedFilesDirectory);
                string importedDirectoryPath = GetImportedDirectory();
                string copiedFilePath = System.IO.Path.Combine(importedDirectoryPath, importedFile.Name);
                bool fileAlreadyInImportedDirectory = String.Equals(importedFile.FullName, copiedFilePath, StringComparison.CurrentCultureIgnoreCase);
                if (fileAlreadyInImportedDirectory)
                {
                    _filesAddedInCurrentSession.Add(importedFile.FullName);
                    return new FileOperationContext(importedFile.FullName, ErrorStatus.Success);
                }

                IDataStore destination = New<IDataStore>(copiedFilePath);
                bool isLocalFileAlreadyExist = destination.IsAvailable;
                try
                {
                    using (FileLock destinationLock = New<FileLocker>().Acquire(destination))
                    {
                        using (FileLock lockedBackup = New<FileLocker>().Acquire(importedFile))
                        {
                            using (Stream sourceStream = importedFile.OpenRead())
                            {
                                using (Stream destinationStream = destination.OpenWrite())
                                {
                                    sourceStream.CopyTo(destinationStream);
                                }
                            }
                        }
                    }
                }
                catch (IOException ex)
                {
                    DeleteLocalFileIfCreated(destination);
                    if (ex.IsFileOrDirectoryNotFound())
                    {
                        return new FileOperationContext(importedFile.FullName, ex.Message, ErrorStatus.FileDoesNotExist);
                    }
                    else
                    {
                        return new FileOperationContext(importedFile.FullName, ex.Message, ErrorStatus.Exception);
                    }
                }
                catch (Exception ex)
                {
                    DeleteLocalFileIfCreated(destination);
                    return new FileOperationContext(importedFile.FullName, ex.Message, ErrorStatus.Exception);
                }
                finally
                {
                    // Remove file if it stored in temporary local folders to avoid the name conflict.
                    if (IsFileInDirectory(importedFile.FullName, _applicationDirectoryPath))
                    {
                        using (FileLock lockedBackup = New<FileLocker>().Acquire(importedFile))
                        {
                            importedFile.Delete();
                        }

                        if (New<ICloudDriveConfiguration>().CurrentDeviceCategory == DeviceCategory.iOS)
                        {
                            // Delete all old files from temporary /Inbox directory. This helps to avoid the name conflict in iOS app.
                            IDataStore[] allFilesInDirectory = importedFile.Container.Files.ToArray();
                            foreach (IDataStore file in allFilesInDirectory)
                            {
                                using (FileLock fileLock = New<FileLocker>().Acquire(file))
                                {
                                    file.Delete();
                                }
                            }
                        }
                    }
                }

                // Save all files, which were imported after app launching. These files haven't added to recent files yet,
                // but also should be ignored during cleanup.
                _filesAddedInCurrentSession.Add(destination.FullName);

                // Cleanup is invoked only after the first file import. This allows don't wait while the long cleanup process is completed,
                // so app launched faster.
                CleanupUnknownFiles();
                return new FileOperationContext(destination.FullName, ErrorStatus.Success);
            });
        }

        public async Task<FileOperationContext> CopyFileToImportedFiles(Func<Stream, Task> getFileStream, IDataStore importedFile)
        {
            New<WorkFolder>().FileInfo.CreateFolder(ImportedFilesDirectory);
            string importedDirectoryPath = GetImportedDirectory();
            string copiedFilePath = System.IO.Path.Combine(importedDirectoryPath, importedFile.Name);

            IDataStore destination = New<IDataStore>(copiedFilePath);
            bool isLocalFileAlreadyExist = destination.IsAvailable;
            try
            {
                using (FileLock destinationLock = New<FileLocker>().Acquire(destination))
                {
                    using (Stream destinationStream = destination.OpenWrite())
                    {
                        await getFileStream(destinationStream);
                    }
                }
            }
            catch (IOException ex)
            {
                DeleteLocalFileIfCreated(destination);
                if (ex.IsFileOrDirectoryNotFound())
                {
                    return new FileOperationContext(importedFile.FullName, ex.Message, ErrorStatus.FileDoesNotExist);
                }
                else
                {
                    return new FileOperationContext(importedFile.FullName, ex.Message, ErrorStatus.Exception);
                }
            }
            catch (Exception ex)
            {
                DeleteLocalFileIfCreated(destination);
                return new FileOperationContext(importedFile.FullName, ex.Message, ErrorStatus.Exception);
            }
            finally
            {
                // Remove file if it stored in temporary local folders to avoid the name conflict.
                //if (IsFileInDirectory(importedFile.FullName, _applicationDirectoryPath))
                //{
                //    using (FileLock lockedBackup = New<FileLocker>().Acquire(importedFile))
                //    {
                //        importedFile.Delete();
                //    }

                //    if (Device.RuntimePlatform == Device.iOS)
                //    {
                //        // Delete all old files from temporary /Inbox directory. This helps to avoid the name conflict in iOS app.
                //        IDataStore[] allFilesInDirectory = importedFile.Container.Files.ToArray();
                //        foreach (IDataStore file in allFilesInDirectory)
                //        {
                //            using (FileLock fileLock = New<FileLocker>().Acquire(file))
                //            {
                //                file.Delete();
                //            }
                //        }
                //    }
                //}
            }

            // Save all files, which were imported after app launching. These files haven't added to recent files yet,
            // but also should be ignored during cleanup.
            _filesAddedInCurrentSession.Add(destination.FullName);

            // Cleanup is invoked only after the first file import. This allows don't wait while the long cleanup process is completed,
            // so app launched faster.
            CleanupUnknownFiles();
            return new FileOperationContext(destination.FullName, ErrorStatus.Success);
        }

        private static void DeleteLocalFileIfCreated(IDataStore destination)
        {
            if (destination.IsAvailable)
            {
                destination.Delete();
            }
        }

        /// <summary>
        /// Removes files which don't presented in recent list. This helps to avoid "garbage" in imported files directory.
        /// E.g. user can start file adding and close app during decryption.
        /// </summary>
        /// <returns></returns>
        private async void CleanupUnknownFiles()
        {
            if (_isCleanupAlreadyInvoked)
            {
                return;
            }

            _isCleanupAlreadyInvoked = true;

            await Task.Run(() =>
            {
                FileSystemState fileSystemState = New<FileSystemState>();
                List<string> knownPathes = fileSystemState.ActiveFiles.Select(f => f.EncryptedFileInfo.FullName).ToList();

                string importedDirectoryPath = GetImportedDirectory();
                IDataContainer importedDirectory = New<IDataContainer>(importedDirectoryPath);

                foreach (IDataStore file in importedDirectory.Files.ToArray())
                {
                    using (FileLock lockedBackup = New<FileLocker>().Acquire(file))
                    {
                        bool isKnownFile = knownPathes.Any(path => String.Equals(file.FullName, path, StringComparison.CurrentCultureIgnoreCase)) ||
                                           _filesAddedInCurrentSession.Any(path => String.Equals(file.FullName, path, StringComparison.CurrentCultureIgnoreCase));
                        if (!isKnownFile)
                        {
                            New<AxCryptFile>().Wipe(lockedBackup, new ProgressContext());
                        }
                    }
                }
            });
        }

        public async Task RemoveFile(IDataStore dataStore)
        {
            await Task.Run(() =>
            {
                string importedDirectoryPath = GetImportedDirectory();
                IDataContainer importedDirectory = New<IDataContainer>(importedDirectoryPath);
                bool isFileInImportDirectory = IsFileInDirectory(dataStore.FullName, importedDirectory.FullName);

                if (!isFileInImportDirectory)
                {
                    return;
                }

                using (FileLock lockedBackup = New<FileLocker>().Acquire(dataStore))
                {
                    New<AxCryptFile>().Wipe(lockedBackup, new ProgressContext());
                }
            });
        }

        private static bool IsFileInDirectory(string filePath, string directoryPath)
        {
            // We use contains instead of StartWith, because filePath file path can be the same.
            bool isInDirectory = filePath.ToLower().Contains(directoryPath.ToLower());
            return isInDirectory; //Contains(directoryPath StringComparison.CurrentCultureIgnoreCase);
        }

        private static string GetImportedDirectory()
        {
            string importedDirectoryPath = System.IO.Path.Combine(New<WorkFolder>().FileInfo.FullName, ImportedFilesDirectory);
            return importedDirectoryPath;
        }
    }
}