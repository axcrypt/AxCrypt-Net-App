using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Content;
using AxCrypt.Core.Extensions;
using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.Services.UI;

namespace AxCrypt.App.Shared.Models;

public class FilePasswordDialogViewModel : ViewModelBase
{
    public FilePasswordDialogViewModel()
    {
        DialogResult = DialogResult.None;
        FilePasswordDialog = new CommonDialogService();
        ErrorMessage = "";
    }

    public FilePasswordViewModel? ViewModel { get; private set; }

    public DialogResult DialogResult { get; set; }

    public string ErrorMessage { get; set; }

    public bool IsShowMoreVisible { get; set; }

    public CommonDialogService FilePasswordDialog { get { return GetProperty<CommonDialogService>(nameof(FilePasswordDialog)); } set { SetProperty(nameof(FilePasswordDialog), value); } }

    public async Task ShowFilePasswordDialog(string encryptedFileFullName)
    {
        ViewModel = new FilePasswordViewModel(encryptedFileFullName);
        BindPropertyChangedEvents();

        FilePasswordDialog.Show();

        while (DialogResult == DialogResult.None)
        {
            await Task.Delay(1000);
        }

        FilePasswordDialog.Close();
    }

    private void BindPropertyChangedEvents()
    {
        ViewModel.BindPropertyChanged(nameof(FilePasswordViewModel.ShowPassword), (bool show) => { FilePasswordDialog.Show(); DialogResult = DialogResult.None; });
        ViewModel.BindPropertyChanged(nameof(FilePasswordViewModel.FileName), (string fileName) => { FilePasswordDialog.Show(); DialogResult = DialogResult.None; });
        ViewModel.BindPropertyChanged(nameof(FilePasswordViewModel.IsLegacyFile), (bool isLegacy) => { IsShowMoreVisible = isLegacy; FilePasswordDialog.Show(); DialogResult = DialogResult.None; });
    }

    public void OkButton_Click(EventArgs e)
    {
        if (!AdHocValidationDueToMonoLimitations())
        {
            DialogResult = DialogResult.None;
            return;
        }

        DialogResult = DialogResult.OK;
    }
    
    public void CancelButton_Click(EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        ViewModel = new FilePasswordViewModel("");
        ErrorMessage = "";
        FilePasswordDialog.Close();
    }

    private bool AdHocValidationDueToMonoLimitations()
    {
        ErrorMessage = "";

        if (ViewModel![nameof(FilePasswordViewModel.KeyFileName)].Length > 0)
        {
            ErrorMessage = Texts.FileNotFound;
            return false;
        }

        if (ViewModel[nameof(FilePasswordViewModel.PasswordText)].Length == 0)
        {
            return true;
        }

        if (String.IsNullOrEmpty(ViewModel.FileName))
        {
            ErrorMessage = Texts.UnknownLogOn;
        }
        else
        {
            ErrorMessage = ViewModel.ValidationError.ToValidationMessage();
        }
        return false;
    }
}




//using AxCrypt.App.Shared.Desktop.Models;
//using AxCrypt.App.Shared.Desktop.Utility;
//using AxCrypt.Content;
//using AxCrypt.Core.UI.ViewModel;
//using AxCrypt.Core.Extensions;
//using AxCrypt.App.Windows.Services;
//using AxCrypt.Core.Crypto;

//namespace AxCrypt.App.Windows.ViewModels;

//public class FilePasswordDialogViewModel : ViewModelBase
//{
//    private LogOnViewModel _logOnViewModel;
//    private string _fileName;
//    private FilePasswordViewModel? _viewModel { get; set; }

//    public FilePasswordDialogViewModel()
//    {
//        _logOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
//    }

//    public void SetFilePassword(string encryptedFileFullName)
//    {
//        _viewModel = new FilePasswordViewModel(encryptedFileFullName);
//        FileName = _viewModel.FileName;
//        InitializePropertyValues();
//        _logOnViewModel.FilePasswordDialog.Show();
//        while (DialogResult == DialogResult.None)
//        {
//            Task.Delay(1000);
//        }

//        //_logOnViewModel.UIStateChanged();
//    }

//    private void InitializePropertyValues()
//    {
//        //_passphraseTextBox.TextChanged += (sender, e) => { ViewModel.PasswordText = _passphraseTextBox.Text; ClearErrorProviders(); };
//        // _keyFileTextBox.TextChanged += (sender, e) => { ViewModel.KeyFileName = _keyFileTextBox.Text; };
//        //_showPassphraseCheckBox.CheckedChanged += (sender, e) => { ViewModel.ShowPassword = _showPassphraseCheckBox.Checked; };
//        BindPropertyChangedEvents();
//    }

//    private void BindPropertyChangedEvents()
//    {
//        _viewModel.BindPropertyChanged(nameof(FilePasswordViewModel.ShowPassword), (bool show) => { ShowPassPhrase = show; });
//        _viewModel.BindPropertyChanged(nameof(FilePasswordViewModel.FileName), (string fileName) => { FileName = fileName; });
//        //ViewModel.BindPropertyChanged(nameof(FilePasswordViewModel.IsLegacyFile), (bool isLegacy) => { _moreButton.Visible = isLegacy; });
//    }

//    public Passphrase PassPhrase { get; set; }
//    public bool ShowPassPhrase { get; set; }
//    public string FileName { get; set; } = string.Empty;
//    public string KeyFile { get; set; } = string.Empty;
//    public DialogResult DialogResult { get; set; } = DialogResult.None;
//    public string ErrorMessage { get; set; }

//    public void OkButton_Click(EventArgs e)
//    {
//        if (!AdHocValidationDueToMonoLimitations())
//        {
//            DialogResult = DialogResult.None;
//            return;
//        }

//        DialogResult = DialogResult.OK;
//    }

//    private bool AdHocValidationDueToMonoLimitations()
//    {
//        ErrorMessage = "";

//        if (_viewModel![nameof(FilePasswordViewModel.KeyFileName)].Length > 0)
//        {
//            ErrorMessage = Texts.FileNotFound;
//            return false;
//        }

//        if (_viewModel[nameof(FilePasswordViewModel.PasswordText)].Length == 0)
//        {
//            return true;
//        }

//        if (String.IsNullOrEmpty(_viewModel.FileName))
//        {
//            ErrorMessage = Texts.UnknownLogOn;
//        }
//        else
//        {
//            ErrorMessage = _viewModel.ValidationError.ToValidationMessage();
//        }
//        return false;
//    }

//    private void PassphraseTextBox_Enter(EventArgs e)
//    {
//        ErrorMessage = "";
//    }

//    private void KeyFileTextBox_Enter(object sender, EventArgs e)
//    {
//        ErrorMessage = "";
//    }

//    public async void KeyFileBrowseForButton_Click(object sender, EventArgs e)
//    {
//        //using (OpenFileDialog ofd = new OpenFileDialog())
//        //{
//        //    ofd.Title = Texts.KeyFileBrowseTitle;
//        //    ofd.Multiselect = false;
//        //    ofd.CheckFileExists = true;
//        //    ofd.CheckPathExists = true;
//        //    ofd.DefaultExt = ".txt";
//        //    ofd.Filter = Texts.FileFilterDialogFilterPatternWin.InvariantFormat("." + ofd.DefaultExt, Texts.FileFilterFileTypeKeyFile, Texts.FileFilterFileTypeAllFiles);
//        //    DialogResult result = ofd.ShowDialog();
//        //    if (result == DialogResult.OK)
//        //    {
//        //        _keyFileTextBox.Text = ofd.FileName;
//        //        _keyFileTextBox.SelectionStart = ofd.FileName.Length;
//        //        _keyFileTextBox.SelectionLength = 1;
//        //        _keyFileTextBox.Focus();
//        //    }
//        //}
//        if (string.IsNullOrEmpty(_fileName))
//        {
//            FileResult fileResult = await FilePicker.Default.PickAsync()!;
//            if (fileResult != null)
//            {
//                _fileName = fileResult.FullPath;
//            }
//        }
//    }

//    //private void MoreButton_Click(object sender, EventArgs e)
//    //{
//    //    KeyFilePanel.Visible = true;
//    //    _moreButton.Visible = false;
//    //}

//    public void ClearErrorProviders()
//    {
//        ErrorMessage = "";
//    }
//}
