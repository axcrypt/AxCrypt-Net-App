using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AxCrypt.Abstractions;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.IO;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.CloudCore
{
    /// <summary>
    /// Provides logic for managing recent files list.
    /// </summary>
    public class RecentFilesProvider
    {
        private ActiveFileAction _activeFileAction = new ActiveFileAction();

        private FileSystemState _fileSystemState = New<FileSystemState>();

        private ImportedFileStorage _importedFileStorage = New<ImportedFileStorage>();

        public async Task<bool> RemoveFilesFromRecent(IEnumerable<IDataStore> files)
        {
            // Avoid working with files in UI thread.
            return await Task.Run(async () =>
            {
                await PurgeActiveFiles();
                return await RemoveAll(files);
            });
        }

        private async Task<bool> RemoveAll(IEnumerable<IDataStore> files)
        {
            foreach (IDataStore file in files)
            {
                ActiveFile activeFile = _fileSystemState.FindActiveFileFromEncryptedPath(file.FullName);
                if (activeFile == null)
                {
                    continue;
                }

                _fileSystemState.RemoveActiveFile(activeFile);
                activeFile = _fileSystemState.FindActiveFileFromEncryptedPath(file.FullName);
                if (activeFile != null)
                {
                    continue;
                }

                await _importedFileStorage.RemoveFile(file);
            }

            await _fileSystemState.Save();
            return true;
        }

        public async Task RefreshRecentFilesStates()
        {
            // Avoid working with files in UI thread.
            await Task.Run(async () =>
            {
                await PurgeActiveFiles();
                string[] knownFiles = _fileSystemState.ActiveFiles.Select(f => f.EncryptedFileInfo.FullName).ToArray();
                await _fileSystemState.UpdateActiveFiles(knownFiles);
            });
        }

        public async Task PurgeActiveFilesAsync()
        {
            await Task.Run(async () =>
            {
                await PurgeActiveFiles();
            });
        }

        private async Task PurgeActiveFiles()
        {
            await _activeFileAction.ClearExceptionState();
            await _activeFileAction.PurgeActiveFiles(new ProgressContext());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Not nested when used via async pattern.")]
        public async Task<IEnumerable<ActiveFile>> LoadRecentFiles()
        {
            // Avoid working with files in UI thread.
            // return await Task.Run(async () =>
            // {
                await _activeFileAction.CheckActiveFiles(new ProgressContext());
                return _fileSystemState.ActiveFiles;
            //});
        }

        public async Task<EncryptedProperties> LoadPropertiesAsync(IDataStore file, LogOnIdentity identity)
        {
            // Avoid working with files in UI thread.
            return await Task.Run(() =>
            {
                try
                {
                    if (identity == LogOnIdentity.Empty)
                    {
                        identity = New<KnownIdentities>().DefaultEncryptionIdentity;
                    }

                    return EncryptedProperties.Create(file, identity);
                }
                catch (Exception ex)
                {
                    // File can contains incorrect format, etc.
                    New<IReport>().Exception(ex);

                    return null;
                }
            });
        }

        public async Task<FileOpenedContext> DecryptAndLaunch(IDataStore file, Passphrase passphrase)
        {
            // Avoid working with files in UI thread.
            return await Task.Run(async () =>
            {
                IExternalDataStore externalDataStore = file as IExternalDataStore;
                Task<IDisposable> openTask = Task.FromResult<IDisposable>(null);
                if (externalDataStore != null)
                {
                    // Loads external file before the long manipulations with it content.
                    // This allows to avoid multiple open/close operations.
                    openTask = externalDataStore.OpenAsync();
                }

                using (await openTask)
                {
                    ProgressContext progressContext = new ProgressContext();
                    FileOperationsController operationsController = new FileOperationsController(progressContext);

                    KnownIdentities knownIdentities = New<KnownIdentities>();
                    operationsController.QuerySaveFileAs += (object sender, FileOperationEventArgs e) =>
                    {
                    };

                    operationsController.QueryDecryptionPassphrase = async (arg) =>
                    {
                        if (passphrase == null)
                        {
                            // If file password is unknown, cancel file decryption.
                            // When user input correct passphrase, DecryptAndLaunch can be called again.
                            arg.Cancel = true;
                            return;
                        }

                        LogOnIdentity identity = LogOnIdentity.Empty;
                        foreach (Passphrase candidate in _fileSystemState.KnownPassphrases)
                        {
                            if (candidate.Thumbprint == passphrase.Thumbprint)
                            {
                                identity = new LogOnIdentity(passphrase);
                                break;
                            }
                        }

                        if (identity == LogOnIdentity.Empty)
                        {
                            identity = new LogOnIdentity(passphrase);
                            _fileSystemState.KnownPassphrases.Add(passphrase);
                            await _fileSystemState.Save();
                        }

                        await knownIdentities.AddAsync(identity);
                        Resolve.UserSettings.EncryptionUpgradeMode = EncryptionUpgradeMode.NotDecided;
                        arg.LogOnIdentity = identity;

                        return;
                    };

                    operationsController.KnownKeyAdded = new AsyncDelegateAction<FileOperationEventArgs>(async (FileOperationEventArgs e) =>
                    {
                        if (!_fileSystemState.KnownPassphrases.Any(i => i.Thumbprint == e.LogOnIdentity.Passphrase.Thumbprint))
                        {
                            _fileSystemState.KnownPassphrases.Add(e.LogOnIdentity.Passphrase);
                        }

                        await knownIdentities.AddAsync(e.LogOnIdentity);
                    });

                    operationsController.Completed += (object sender, FileOperationEventArgs e) =>
                    {
                        return Task.CompletedTask;
                    };

                    FileOperationContext fileOperationContext = await operationsController.DecryptAndLaunchAsync(file);
                    ActiveFile associatedFile = _fileSystemState.FindActiveFileFromEncryptedPath(file.FullName);
                    return new FileOpenedContext(fileOperationContext, associatedFile);
                }
            });
        }
    }
}