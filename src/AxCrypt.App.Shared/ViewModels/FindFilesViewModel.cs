using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.Content;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.IO;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI.FindFilesActivity;
using AxCrypt.Core.UI.ViewModel;
using System.Data;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.ViewModels
{
    public class FindFilesViewModel : ViewModelBase
    {
        private FileOperationViewModel _fileOperationViewModel;
        private IStatusAlertService _statusAlertService;

        public FindFilesViewModel(IStatusAlertService StatusAlerService)
        {
            _fileOperationViewModel = New<FileOperationViewModel>();
            //_fileOperationViewModel = AxCServiceProviderExtension.LogOnViewModel!.FileOperationViewModel;
            _statusAlertService = StatusAlerService;
        }

        public string? SelectedFile { get; set; }

        public string SelectedFilePath { get; set; }

        public int FindFilesCount
        {
            get
            {
                return FindFilesList?.Count() ?? 0;
            }
        }

        public IEnumerable<FindFilesLog> FindFilesList = new List<FindFilesLog>();

        public bool HasFindFilesCapability { get; set; }

        public void LoadSecuredFilesList()
        {
            FindFilesList = New<FindFilesStore>()?.GetFindFilesLogs()!;
            HasFindFilesCapability = AxCServiceProviderExtension.LogOnViewModel!.License.Has(AxCrypt.Core.Runtime.LicenseCapability.FindFiles);
        }

        public void FilterSecuredFiles(string filename)
        {
            SelectedFile = null;

            LoadSecuredFilesList();
            if (string.IsNullOrWhiteSpace(filename))
            {
                return;
            }

            FindFilesList = FindFilesList
                .Where(f => Path.GetFileName(f.FilePath)
                    .Contains(filename, StringComparison.OrdinalIgnoreCase));

            UpdateViewState();
        }

        public async Task HandleActionAsync(string action, string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            IDataStore dataStore = New<IDataStore>(filePath);
            if (!dataStore.IsAvailable)
            {
                _statusAlertService.Error(Texts.FileDoesNotExist.InvariantFormat(Path.GetFileName(filePath)));
                return;
            }

            switch (action)
            {
                case "Open":
                    try
                    {
                        await _fileOperationViewModel.OpenFiles.ExecuteAsync(new[] { filePath });
                    }
                    catch (Exception ex)
                    {
                        _statusAlertService.Error(Texts.FileOpenFailedAlertMsg.InvariantFormat(Path.GetFileName(filePath), ex.Message));
                    }
                    break;
                case "Delete":
                    try
                    {
                        New<FindFilesStore>().PurgeIfExists(filePath);
                        _statusAlertService.Success(Texts.DeletionSuccess.InvariantFormat(Path.GetFileName(filePath)));
                    }
                    catch (Exception ex)
                    {
                        _statusAlertService.Error(Texts.DeletionFailed.InvariantFormat(Path.GetFileName(filePath), ex.Message));
                    }
                    break;

                case "Decrypt":
                    try
                    {
                        await _fileOperationViewModel.DecryptFiles.ExecuteAsync(new[] { filePath });
                        if (!CheckActiveFiles(dataStore.FullName))
                        {
                            _statusAlertService.Success(Texts.FileDecryptionSuccessAlertMsg.InvariantFormat(Path.GetFileName(filePath)));
                        }
                    }
                    catch (Exception ex)
                    {
                        _statusAlertService.Error(Texts.FileDecryptionFailedAlertMsg.InvariantFormat(Path.GetFileName(filePath), ex.Message));
                    }
                    break;

                case "Reveal":
                    try
                    {
                        await _fileOperationViewModel.ShowInFolder.ExecuteAsync(new[] { filePath });
                    }
                    catch (Exception ex)
                    {
                        _statusAlertService.Error(Texts.FolderOpenFailedAlertMsg.InvariantFormat(Path.GetFileName(filePath), ex.Message));
                    }
                    break;

                case "RenameAnonymously":
                    try
                    {
                        await _fileOperationViewModel.RandomRenameFiles.ExecuteAsync(new[] { filePath });
                        if (!CheckActiveFiles(dataStore.FullName))
                        {
                            _statusAlertService.Success(Texts.FileRenameSuccessAlertMsg.InvariantFormat(Path.GetFileName(filePath)));
                        }
                    }
                    catch (Exception ex)
                    {
                        _statusAlertService.Error(Texts.FileRenameFailedAlertMsg.InvariantFormat(Path.GetFileName(filePath), ex.Message));
                    }
                    break;

                case "RenameOriginal":
                    try
                    {
                        await _fileOperationViewModel.RestoreRandomRenameFiles.ExecuteAsync(new[] { filePath });
                        if (!CheckActiveFiles(dataStore.FullName))
                        {
                            _statusAlertService.Success(Texts.FileRestoreRenameSuccessAlertMsg.InvariantFormat(Path.GetFileName(filePath)));
                        }
                    }
                    catch (Exception ex)
                    {
                        _statusAlertService.Error(Texts.FileRestoreRenameFailedAlertMsg.InvariantFormat(Path.GetFileName(filePath), ex.Message));
                    }
                    break;
                default:
                    break;
            }

            UpdateState();
        }

        private void UpdateState()
        {
            LoadSecuredFilesList();
            UpdateViewState();
        }

        private bool CheckActiveFiles(string filePath)
        {
            ActiveFile activeFile = New<FileSystemState>().FindActiveFileFromEncryptedPath(filePath);
            if (activeFile?.Status == ActiveFileStatus.AssumedOpenAndDecrypted)
            {
                return true;
            }

            return false;
        }
    }
}
