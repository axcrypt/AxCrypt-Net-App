using AxCrypt.Abstractions;
using AxCrypt.App.Shared.FileOperations.IO;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.IO;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.FileOperations.Vault;

public class VaultOperationViewModel : ViewModelBase
{
    private CustomParallelFileOperation _fileOperation;
    private KnownIdentities _knownIdentities;

    public IEnumerable<UserPublicKey>? Recipients { get; set; } = null;

    private IdentityViewModel _identityViewModel { get; set; }

    public VaultOperationViewModel(
        KnownIdentities knownIdentities,
        CustomParallelFileOperation customParallelFileOperation
    )
    {
        _identityViewModel = AxCServiceProviderExtension.LogOnViewModel!.FileOperationViewModel.IdentityViewModel;
        _fileOperation = customParallelFileOperation;
        _knownIdentities = knownIdentities;

        InitializePropertyValues();
    }

    private void InitializePropertyValues()
    {
        EncryptFiles = new AsyncDelegateAction<IEnumerable<IVaultDataStore>>(EncryptFilesActionAsync);
        DecryptFiles = new AsyncDelegateAction<IEnumerable<IVaultDataStore>>(DecryptFilesActionAsync);
    }

    public IAsyncAction EncryptFiles { get; private set; }

    public IAsyncAction DecryptFiles { get; private set; }

    private async Task EncryptFilesActionAsync(IEnumerable<IVaultDataStore> files)
    {
        if (!files.Any())
        {
            return;
        }
        if (!_knownIdentities.IsLoggedOn)
        {
            return;
        }

        await EncryptFewOrManyFilesAsync(files);
    }

    private async Task EncryptFewOrManyFilesAsync(IEnumerable<IVaultDataStore> encryptableFiles)
    {
        if (!encryptableFiles.Any())
        {
            return;
        }

        if (encryptableFiles.Count() > New<UserSettings>().FewFilesThreshold)
        {
            await _fileOperation.DoFilesAsync(encryptableFiles, EncryptFileWorkManyAsync, (status) => CheckEncryptionStatus(status));
            return;
        }

        await _fileOperation.DoFilesAsync(encryptableFiles, EncryptFileWorkOneAsync, (status) => CheckEncryptionStatus(status));
    }

    private Task<FileOperationContext> EncryptFileWorkOneAsync(IVaultDataStore dataStore, IProgressContext progress)
    {
        FileOperationsController controller = EncryptFileWorkController(progress, dataStore.CurrentPath);
        controller.Completed += (object sender, FileOperationEventArgs e) =>
        {
            if (e.Status.ErrorStatus == ErrorStatus.Success) { }
            if (e.Status.ErrorStatus == ErrorStatus.FileAlreadyEncrypted)
            {
                e.Status = new FileOperationContext(string.Empty, ErrorStatus.Success);
            }

            return Task.CompletedTask;
        };
        return controller.EncryptFileAsync(dataStore.File, Recipients!);
    }

    private Task<FileOperationContext> EncryptFileWorkManyAsync(IVaultDataStore dataStore, IProgressContext progress)
    {
        FileOperationsController controller = EncryptFileWorkController(progress, dataStore.CurrentPath);
        return controller.EncryptFileAsync(dataStore.File, Recipients!);
    }

    private static FileOperationsController EncryptFileWorkController(IProgressContext progress, IDataContainer saveFileDataContainer)
    {
        FileOperationsController operationsController = new FileOperationsController(
            progress,
            saveFileDataContainer
        );

        operationsController.QuerySaveFileAs += (object sender, FileOperationEventArgs e) =>
        {
            using (FileLock lockedSave = e.SaveFileFullName.CreateUniqueFile())
            {
                e.SaveFileFullName = lockedSave.DataStore.FullName;
                lockedSave.DataStore.Delete();
            }
        };

        return operationsController;
    }

    private async Task DecryptFilesActionAsync(IEnumerable<IVaultDataStore> files)
    {
        if (!files.Any())
        {
            return;
        }

        await _fileOperation.DoFilesAsync(files, DecryptFileWork, (status) => Task.FromResult(CheckStatusAndShowMessage(status, string.Empty)));
    }

    private Task<FileOperationContext> DecryptFileWork(IVaultDataStore dataStore, IProgressContext progress)
    {
        ActiveFile activeFile = New<FileSystemState>().FindActiveFileFromEncryptedPath(dataStore.File.FullName);
        if (activeFile != null && activeFile.Status.HasFlag(ActiveFileStatus.AssumedOpenAndDecrypted))
        {
            return Task.FromResult(new FileOperationContext(dataStore.File.FullName, ErrorStatus.FileLocked));
        }

        FileOperationsController operationsController = new FileOperationsController(progress, dataStore.CurrentPath);

        operationsController.QueryDecryptionPassphrase = HandleQueryDecryptionPassphraseEventAsync;

        operationsController.QuerySaveFileAs += async (object sender, FileOperationEventArgs e) =>
        {
            FileSelectionEventArgs fileSelectionArgs = new FileSelectionEventArgs(new string[] { e.SaveFileFullName })
            {
                FileSelectionType = FileSelectionType.SaveAsDecrypted,
            };
            await OnSelectingFilesAsync(fileSelectionArgs);
            if (fileSelectionArgs.Cancel)
            {
                e.Cancel = true;
                return;
            }
            e.SaveFileFullName = fileSelectionArgs.SelectedFiles[0];
        };

        operationsController.KnownKeyAdded = new AsyncDelegateAction<FileOperationEventArgs>(async (FileOperationEventArgs e) =>
        {
            await _knownIdentities.AddAsync(e.LogOnIdentity);
        });

        operationsController.Completed += async (object sender, FileOperationEventArgs e) =>
        {
            if (e.Status.ErrorStatus == ErrorStatus.Success)
            {
                await New<ActiveFileAction>().RemoveRecentFiles(new IDataStore[] { New<IDataStore>(e.OpenFileFullName) }, progress);
            }
        };

        return operationsController.DecryptFileAsync(dataStore.File);
    }

    private Task HandleQueryDecryptionPassphraseEventAsync(FileOperationEventArgs e)
    {
        return QueryDecryptPassphraseAsync(e);
    }

    private async Task QueryDecryptPassphraseAsync(FileOperationEventArgs e)
    {
        await _identityViewModel.AskForDecryptPassphrase.ExecuteAsync(e.OpenFileFullName);
        if (_identityViewModel.LogOnIdentity == LogOnIdentity.Empty)
        {
            e.Cancel = true;
            return;
        }
        e.LogOnIdentity = _identityViewModel.LogOnIdentity;
    }

    public event Func<object, FileSelectionEventArgs, Task> SelectingFilesAsync;

    // Async method to trigger the event
    protected virtual async Task OnSelectingFilesAsync(FileSelectionEventArgs e)
    {
        if (SelectingFilesAsync != null)
        {
            Delegate[] eventHandlers = SelectingFilesAsync.GetInvocationList();

            foreach (Delegate handler in eventHandlers)
            {
                Func<object, FileSelectionEventArgs, Task> asyncHandler = (Func<object, FileSelectionEventArgs, Task>)handler;
                try
                {
                    await asyncHandler(this, e);  // Await the async handler
                }
                catch (Exception ex)
                {
                    // Handle exception (optional)
                    Console.WriteLine(string.Format(Texts.ErrorInvokingNotification, ex.Message));
                }
            }
        }
    }

    private static Task<bool> CheckEncryptionStatus(FileOperationContext foc)
    {
        if (foc.ErrorStatus == ErrorStatus.FileAlreadyEncrypted)
        {
            foc = new FileOperationContext(foc.FullName, ErrorStatus.Success);
        }

        return Task.FromResult(CheckStatusAndShowMessage(foc, string.Empty));
    }

    private static bool CheckStatusAndShowMessage(FileOperationContext context, string fallbackName)
    {
        return Resolve.StatusChecker.CheckStatusAndShowMessage(
            context.ErrorStatus,
            string.IsNullOrEmpty(context.FullName) ? fallbackName : context.FullName,
            context.InternalMessage
        );
    }

    public async Task EncryptDirectoryAsync(IDataContainer sourceDirContainer, string vaultCurrentDir)
    {
        if (!sourceDirContainer.IsAvailable)
            return;

        if (New<FileFilter>().IsForbiddenFolder(sourceDirContainer.FullName))
        {
            await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.SystemFolderForbiddenText.InvariantFormat(sourceDirContainer));
            return;
        }

        if (sourceDirContainer.IsVault())
        {
            await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.VaultValidationCannotAddVaultFldrToVault);
            return;
        }

        string vaultDestinationDir = Path.Combine(vaultCurrentDir, sourceDirContainer.Name);
        vaultDestinationDir = (await ResolveNameConflict(vaultDestinationDir))!;
        if (vaultDestinationDir == null)
        {
            return;
        }

        try
        {
            IDataContainer vaultDestinationContainer = New<IDataContainer>(vaultDestinationDir);
            if (!vaultDestinationContainer.IsAvailable)
            {
                vaultDestinationContainer.CreateFolder();
            }

            IEnumerable<IVaultDataStore> vaultFileDataStores = sourceDirContainer.Files.Select((file) => New<IVaultDataStore>().Create(file, vaultDestinationContainer.FullName));
            await EncryptFiles.ExecuteAsync(vaultFileDataStores);

            // Copy subdirectories recursively
            foreach (IDataContainer subdir in sourceDirContainer.Folders)
            {
                string destSubDir = Path.Combine(vaultDestinationContainer.FullName, subdir.Name);
                await EncryptDirectoryAsync(subdir, destSubDir);
            }

            sourceDirContainer.Delete();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task DecryptDirectoryAsync(IDataContainer sourceDirContainer, string vaultFolderDecryptPath)
    {
        if (!sourceDirContainer.IsAvailable)
            return;

        string vaultDestinationDir = Path.Combine(vaultFolderDecryptPath, sourceDirContainer.Name);
        vaultDestinationDir = (await ResolveNameConflict(vaultDestinationDir))!;
        if (vaultDestinationDir == null)
        {
            return;
        }

        try
        {
            IDataContainer vaultDestinationContainer = New<IDataContainer>(vaultDestinationDir);
            if (!vaultDestinationContainer.IsAvailable)
            {
                vaultDestinationContainer.CreateFolder();
            }

            if (vaultDestinationContainer.IsVault())
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.VaultValidationCannotAddVaultFldrToVault);
                return;
            }

            IEnumerable<IVaultDataStore> vaultFileDataStores = sourceDirContainer.Files.Select((file) => New<IVaultDataStore>().Create(file, vaultDestinationContainer.FullName));
            await DecryptFiles.ExecuteAsync(vaultFileDataStores);

            // Copy subdirectories recursively
            foreach (IDataContainer subdir in sourceDirContainer.Folders)
            {
                string destSubDir = Path.Combine(vaultDestinationContainer.FullName, subdir.Name);
                await DecryptDirectoryAsync(subdir, destSubDir);
            }

            sourceDirContainer.Delete();
        }
        catch (Exception)
        {
            throw;
        }
    }

    private static async Task<string?> ResolveNameConflict(string targetPath)
    {
        IDataContainer targetDataContainer = New<IDataContainer>(targetPath);
        if (!targetDataContainer.IsAvailable)
        {
            return targetPath;
        }

        PopupButtons popupResult = await New<IPopup>().ShowAsync(PopupButtons.OkCancel, Texts.InformationTitle, Texts.CreateFolderWhenAlreadyExistsConfirmText);
        if (popupResult == PopupButtons.Cancel)
        {
            return null;
        }

        string name = targetDataContainer.Name;
        string dir = targetDataContainer.FullName.Replace(targetDataContainer.Name + "\\", "");
        //string dir = Path.GetDirectoryName(targetPath);

        string newName = $"{name}_{DateTime.Now:yyyyMMddHHmmss}";
        return Path.Combine(dir, newName);
    }
}