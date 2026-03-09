using AxCrypt.App.Shared.UI.ViewModels;
using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.ViewModels.Authentication;
using AxCrypt.Core;
using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
using System.Text;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.CloudCore
{
    public abstract class FileStorageProvider
    {
        private const string ImportedFilesDirectory = "ImportedFiles";

        public string GetImportedFilePath(string importedFileName)
        {
            New<WorkFolder>().FileInfo.CreateFolder(ImportedFilesDirectory);
            string importedDirectoryPath = System.IO.Path.Combine(
                New<WorkFolder>().FileInfo.FullName,
                ImportedFilesDirectory
            );

            return System.IO.Path.Combine(importedDirectoryPath, importedFileName);
        }

        public abstract Task ListFilesAsync(string fileId = "");

        public abstract Task<MemoryStream> ReadFileStreamAsync(string fileId);

        public abstract Task CopyFileToImportedFiles(FilePickerItemViewModel file, Stream destinationFileStream);

        public abstract Task<bool> UpdateFile(FilePickerItemViewModel cloudFileItem, IDataStore fileInfo, CancellationToken ct = default);

        public abstract Task<string> MoveFile(FilePickerItemViewModel fileItem, string fileName, IDataStore fileInfo);

        public abstract Task<bool> DeleteFileAsync(
            string fileName,
            FilePickerItemViewModel fileItem,
            string encryptedFilePathForOverWrite,
            string newFileId = "",
            bool rename = false
        );

        public abstract List<FilePickerItemViewModel> Files { get; }

        public abstract OAuth2Auth OAuth2Authenticator { get; }

        public abstract string PageTitle { get; }

        public FileOperationOption SelectedFileOperation { get; set; }

        protected void WipeLocalFile(string fullFileName)
        {
            try
            {
                IDataStore moveToFileInfo = GenerateRandomFile(fullFileName, true);
                moveToFileInfo.Delete();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal static IDataStore GenerateRandomFile(string fullFileName, bool canMove)
        {
            string randomName;
            do
            {
                randomName = GenerateRandomFileName(fullFileName);
            }
            while (New<IDataStore>(randomName).IsAvailable);

            IDataStore moveToFileInfo = New<IDataStore>(fullFileName);
            moveToFileInfo = MoveOrCopyTo(moveToFileInfo, randomName, canMove);
            moveToFileInfo.IsWriteProtected = false;

            using (Stream stream = moveToFileInfo.OpenUpdate())
            {
                long length = stream.Length + OS.Current.StreamBufferSize - stream.Length % OS.Current.StreamBufferSize;

                for (long position = 0; position < length; position += OS.Current.StreamBufferSize)
                {
                    byte[] random = Resolve.RandomGenerator.Generate(OS.Current.StreamBufferSize);
                    stream.Write(random, 0, random.Length);
                    stream.Flush();
                }
            }

            return moveToFileInfo;
        }

        private static IDataStore MoveOrCopyTo(
            IDataStore moveToFileInfo,
            string randomName,
            bool canMove
        )
        {
            if (canMove)
            {
                moveToFileInfo.MoveTo(randomName);
                return moveToFileInfo;
            }

            IDataStore randomFileInfo = New<IDataStore>(randomName);
            using (Stream srcStream = moveToFileInfo.OpenRead())
            {
                using (Stream destStream = randomFileInfo.OpenWrite())
                {
                    srcStream.CopyTo(destStream);
                    destStream.Flush();
                }
                srcStream.Flush();
            }

            return randomFileInfo;
        }

        private static string GenerateRandomFileName(string originalFullName)
        {
            const string validFileNameChars = "abcdefghijklmnopqrstuvwxyz";

            string directory = Resolve.Portable.Path().GetDirectoryName(originalFullName);
            string fileName = Resolve.Portable.Path().GetFileNameWithoutExtension(originalFullName);

            int randomLength = fileName.Length < 8 ? 8 : fileName.Length;
            StringBuilder randomName = new StringBuilder(randomLength + 4);
            byte[] random = Resolve.RandomGenerator.Generate(randomLength);
            for (int i = 0; i < randomLength; ++i)
            {
                randomName.Append(validFileNameChars[random[i] % validFileNameChars.Length]);
            }
            randomName.Append(".tmp");

            return Resolve.Portable.Path().Combine(directory, randomName.ToString());
        }

        public static string GenerateRandomFolderName()
        {
            const string validFileNameChars = "abcdefghijklmnopqrstuvwxyz";
            const int randomLength = 8;
            
            StringBuilder randomName = new StringBuilder(randomLength + 4);
            byte[] random = Resolve.RandomGenerator.Generate(randomLength);
            for (int i = 0; i < randomLength; ++i)
            {
                randomName.Append(validFileNameChars[random[i] % validFileNameChars.Length]);
            }

            return randomName.ToString();
        }

        public static bool IsNetworkError(Exception ex)
        {
            return ex is HttpRequestException
                || ex is IOException
                || ex is TimeoutException
                || ex is TaskCanceledException;
        }

        public string NormalizeToCloudPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "/";

            path = path.Replace("\\", "/");

            int colonIndex = path.IndexOf(':');
            if (colonIndex >= 0)
                path = path[(colonIndex + 1)..];

            while (path.Contains("//"))
                path = path.Replace("//", "/");

            int lastSlashIndex = path.LastIndexOf('/');
            path = lastSlashIndex >= 0
                ? path[..(lastSlashIndex + 1)]
                : "/";

            path = "/" + path.TrimStart('/');

            return path.EndsWith("/") ? path : path + "/";
        }
    }
}