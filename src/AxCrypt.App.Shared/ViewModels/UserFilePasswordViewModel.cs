using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Content;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.UI.ViewModel;

namespace AxCrypt.App.Shared.ViewModels;

public class UserFilePasswordViewModel : ViewModelBase
{
    public UserFilePasswordViewModel(IFilePasswordWindowService? filePasswordWindowService)
    {
        _filePasswordService = filePasswordWindowService;
    }

    public FilePasswordViewModel? ViewModel { get; set; }

    public LogOnViewModel? LogOnViewModel { get; set; }

    private IFilePasswordWindowService? _filePasswordService;

    public DialogResult DialogResult { get; set; }

    public string? UserEmail { get; set; }
    public string? FileName { get; set; }
    public string? ErrorMessage { get; set; }

    public bool IsShowMoreVisible { get; set; }
    public bool ShowVisible { get; set; }

    public TaskCompletionSource<DialogResult>? FilePasswordTcs;

    public void OkButton_Click(EventArgs e)
    {
        if (!AdHocValidationDueToMonoLimitations())
        {
            return;
        }

        LogOnViewModel!.LogOnAccountModel.UserEmail = UserEmail!;
        LogOnViewModel.LogOnAccountModel.ReadOnlyUserEmail = !string.IsNullOrEmpty(UserEmail);

        FilePasswordTcs?.TrySetResult(DialogResult.OK);

        _filePasswordService.Close();
        IsWindowActive = false;
        UpdateViewState();
    }

    public void CancelButton_Click(EventArgs e)
    {
        _filePasswordService.Close();
        IsWindowActive = false;
        UpdateViewState();
        FilePasswordTcs?.TrySetResult(DialogResult.Cancel);
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

    public bool IsWindowActive { get; set; }

}
