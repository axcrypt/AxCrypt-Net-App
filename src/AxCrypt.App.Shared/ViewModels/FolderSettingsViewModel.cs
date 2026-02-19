using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Content;
using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.ViewModels
{
    public class FolderSettingsViewModel : ViewModelBase
    {
        public LogOnViewModel LogOnViewModel;

        private IList<string> _selectedFilesOrFolders = new List<string>();
        private IList<string> _ignoredFoldersList = new List<string>();
        private IList<string> _originalFoldersList = new List<string>();

        private FolderSettingViewModel? _viewModel;

        public FolderSettingsViewModel()
        {
            LogOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
        }

        public DialogResult PageResult
        { get { return GetProperty<DialogResult>(nameof(PageResult)); } set { SetProperty(nameof(PageResult), value); } }

        public async Task SetFolderSettings(IEnumerable<string> filesOrFoldersPath, FolderSettingViewModel viewModel, Action OkAction)
        {
            ClearErrorProviders();
            FoldersInput = "";
            PageResult = DialogResult.None;
            _selectedFilesOrFolders = new List<string>(filesOrFoldersPath);

            _viewModel = viewModel;
            _viewModel.BindPropertyChanged<IEnumerable<string>>(nameof(FolderSettingViewModel.IgnoredFolders), (aks) =>
            {
                IgnoredFoldersList = aks.Distinct().ToList();
            });
            _originalFoldersList = IgnoredFoldersList;
            LogOnViewModel.FolderSettingsDialog.Show();

            while (PageResult == DialogResult.None)
            {
                await Task.Delay(1000);
            }

            if (PageResult == DialogResult.OK)
            {
                OkAction();
            }

            LogOnViewModel.FolderSettingsDialog.Close();
        }

        public string FoldersInput
        {
            get { return GetProperty<string>(nameof(FoldersInput)); }
            set
            {
                SetProperty(nameof(FoldersInput), value);
                LogOnViewModel.UIStateChanged();
                ClearErrorProviders();
            }
        }

        public IList<string> SelectedFilesOrFolders
        {
            get => _selectedFilesOrFolders;
        }

        public IList<string> IgnoredFoldersList
        {
            get => _ignoredFoldersList;
            private set
            {
                _ignoredFoldersList = value;
                LogOnViewModel.UIStateChanged();
            }
        }

        public bool HasFolderChanges
        {
            get
            {
                return !_ignoredFoldersList.SequenceEqual(_originalFoldersList);
            }
        }

        public string? ErrorMessage { get; set; }

        public void ApplyFolderSettings()
        {
            if (!HasFolderChanges)
            {
                ErrorMessage = "No changes detected";
                return;
            }

            _originalFoldersList = _ignoredFoldersList;
            LogOnViewModel.UIStateChanged();
            PageResult = DialogResult.OK;
        }

        public async Task SelectIgnoreFolder(EventArgs eventArgs)
        {
            await PremiumFeature_ClickAsync(LicenseCapability.SecureFolders, async (ss, ee) => { await WatchedFoldersBrowseIgnoreFolder_Click(ss, ee); }, null!, eventArgs);
        }

        private async Task PremiumFeature_ClickAsync(LicenseCapability requiredCapability, Func<object, EventArgs, Task> realHandler, object sender, EventArgs e)
        {
            if (LogOnViewModel.License.Has(requiredCapability))
            {
                if (realHandler != null)
                {
                    await realHandler(sender, e);
                }
                return;
            }

            AxCServiceProviderExtension.UpgradeSubscriptionViewModel!.ShowUpgradeDialog();
        }

        private async Task WatchedFoldersBrowseIgnoreFolder_Click(object sender, EventArgs e)
        {
            FileSelectionEventArgs eventArgs = new FileSelectionEventArgs(new string[] { })
            {
                FileSelectionType = FileSelectionType.Folder,
            };
            await New<IDataItemSelection>().HandleSelection(eventArgs);
            if (eventArgs.SelectedFiles == null || !eventArgs.SelectedFiles.Any())
            {
                return;
            }

            FoldersInput = eventArgs.SelectedFiles?.FirstOrDefault() ?? "";
        }

        public void AddIgnoredFolder()
        {
            string securedFolder = SelectedFilesOrFolders[0];
            if (FoldersInput == null || FoldersInput.Trim() == string.Empty)
            {
                ErrorMessage = Texts.InvalidFolder;
                LogOnViewModel.UIStateChanged();
                return;
            }

            IDataContainer container = New<IDataContainer>(FoldersInput);
            if (!container.IsFolder)
            {
                ErrorMessage = Texts.InvalidFolder;
                LogOnViewModel.UIStateChanged();
                return;
            }

            if (!container.FullName.StartsWith(securedFolder))
            {
                ErrorMessage = Texts.InvalidSubFolder;
                LogOnViewModel.UIStateChanged();
                return;
            }

            if (IgnoredFoldersList.Contains(this.FoldersInput))
            {
                ErrorMessage = Texts.InvalidSubFolder;
                LogOnViewModel.UIStateChanged();
                return;
            }

            _viewModel!.AddIgnoreFolder.Execute(FoldersInput);
            this.FoldersInput = string.Empty;
        }

        public async Task RemoveFolder(string folderPath)
        {
            await _viewModel!.RemoveIgnoreFolder.ExecuteAsync(folderPath);
        }

        private void ClearErrorProviders()
        {
            ErrorMessage = "";
        }
    }
}