using AxCrypt.App.Desktop.Models;
using AxCrypt.App.Desktop.Utility;
using AxCrypt.App.Windows.Services;
using AxCrypt.Content;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;

namespace AxCrypt.App.Windows.ViewModels;

public class ImportPrivateKeyViewModel
{
    private ImportPrivateKeysViewModel _viewModel;
    private LogOnViewModel _logOnViewModel;

    public ImportPrivateKeyViewModel()
    {
        _logOnViewModel = AxCServiceProvider.LogOnViewModel!;
    }

    public void ShowDialog(UserSettings userSettings, KnownIdentities knownIdentities)
    {
        _viewModel = new ImportPrivateKeysViewModel(userSettings, knownIdentities);
    }

    private void Initialized()
    {
        _viewModel.BindPropertyChanged<bool>(nameof(_viewModel.ImportSuccessful), (ok) => { if (!ok) { ErrorMessage = Texts.FailedPrivateImport; } });
        _viewModel.BindPropertyChanged<bool>(nameof(_viewModel.ShowPassword), (show) => { ShowPassPhrase = show; });
    }

    public string ErrorMessage { get; set; }
    public bool ShowPassPhrase { get; set; }
    public string? PrivateKeyFileName { get; set; }
    public string? PasswordText { get; set; }
    private DialogResult PageResult { get; set; }

    public async void ButtonOk_Click(EventArgs e)
    {
        if (!AdHocValidationDueToMonoLimitations())
        {
            PageResult = DialogResult.None;
            return;
        }

        _viewModel.PasswordText = PasswordText;
        await _viewModel.ImportFile.ExecuteAsync(null);
        if (!_viewModel.ImportSuccessful)
        {
            PageResult = DialogResult.None;
            return;
        }
        PageResult = DialogResult.OK;
    }

    private bool AdHocValidationDueToMonoLimitations()
    {
        bool validated = true;

        if (_viewModel[nameof(ImportPrivateKeysViewModel.PasswordText)].Length > 0)
        {
            ErrorMessage = Texts.WrongPassphrase;
            validated = false;
        }
        else
        {
            ErrorMessage = "";
        }

        if (_viewModel[nameof(ImportPrivateKeysViewModel.PrivateKeyFileName)].Length > 0)
        {
            ErrorMessage = Texts.FileNotFound;
            validated = false;
        }
        else
        {
            ErrorMessage = "";
        }

        return validated;
    }

    //private void _browsePrivateKeyFileButton_Click(object sender, EventArgs e)
    //{
    //    using (OpenFileDialog ofd = new OpenFileDialog())
    //    {
    //        ofd.Title = Texts.ImportPrivateKeysFileSelectionTitle;
    //        ofd.Multiselect = false;
    //        ofd.CheckFileExists = true;
    //        ofd.CheckPathExists = true;
    //        ofd.DefaultExt = New<IRuntimeEnvironment>().AxCryptExtension;
    //        ofd.Filter = Texts.FileFilterDialogFilterPatternWin.InvariantFormat("." + ofd.DefaultExt, Texts.FileFilterFileTypeAxCryptIdFiles, Texts.FileFilterFileTypeAllFiles);
    //        DialogResult result = ofd.ShowDialog();
    //        if (result == DialogResult.OK)
    //        {
    //            _privateKeyFileTextBox.Text = ofd.FileName;
    //            _privateKeyFileTextBox.SelectionStart = ofd.FileName.Length;
    //            _privateKeyFileTextBox.SelectionLength = 1;
    //            _passphraseTextBox.Focus();
    //        }
    //    }
    //}

    public async Task BrowsePrivateKeyFile()
    {
        FileResult fileResult = await FilePicker.Default.PickAsync();
        if (fileResult != null)
        {
            PrivateKeyFileName = fileResult.FullPath;
            _viewModel.PrivateKeyFileName = fileResult.FullPath;
        }
    }

    public void ClearErrorProviders()
    {
        ErrorMessage = "";
    }
}
