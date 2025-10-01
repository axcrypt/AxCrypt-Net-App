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
        ViewModel!.BindPropertyChanged(nameof(FilePasswordViewModel.ShowPassword), (bool show) => { FilePasswordDialog.Show(); DialogResult = DialogResult.None; });
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