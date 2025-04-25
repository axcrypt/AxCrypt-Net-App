using AxCrypt.Abstractions;
using AxCrypt.Content;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Core.UI;
using System;
using System.Threading.Tasks;
using AxCrypt.App.Desktop.Helpers;
using System.Linq;
using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.Core;

namespace AxCrypt.App.Desktop.ViewModels.Main;

public class AdvancedOptionsViewModel : ViewModelBase
{
    public string? TempConfigPath { get; set; }
    public string? AppConfigPath { get; set; }

    public LogOnViewModel LogOnViewModel { get; set; }

    public AdvancedOptionsViewModel()
    {
        LogOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
    }

    public void Initialize()
    {
        AppConfigPath = New<WorkFolder>().FileInfo.FullName;
        TempConfigPath = New<UserSettings>().TemporaryFilePath;
    }

    public string? ErrorMessage { get; set; }

    public async void BrowseButton_click(EventArgs e)
    {
        FileSelectionEventArgs eventArgs = new FileSelectionEventArgs(new string[] { })
        {
            FileSelectionType = FileSelectionType.Folder
        };

        await New<IDataItemSelection>().HandleSelection(eventArgs);
        if (eventArgs.SelectedFiles == null || !eventArgs.SelectedFiles.Any())
        {
            return;
        }

        string selectedPath = eventArgs.SelectedFiles.First();
        if (selectedPath != null)
        {
            TempConfigPath = selectedPath;
        }

        UpdateViewState();
    }

    public async void ButtonOk_Click(EventArgs e)
    {
        if (string.IsNullOrEmpty(TempConfigPath))
        {
            ErrorMessage = "Temporary path cannot be null";
            return;
        }

        if (TempConfigPath == New<UserSettings>().TemporaryFilePath)
        {
            ErrorMessage = "Given path is already selected";
            return;
        }

        LogOnViewModel.AdvancedOptionsDialog.Close();

        if (!await New<IVerifySignInPassword>().Verify(Texts.ChangeOptionGenericWarning))
        {
            return;
        }

        PopupButtons result = await New<IPopup>().ShowAsync(PopupButtons.OkCancel, Texts.IncludeSubfoldersConfirmationTitle, "Are you want to confirm to move all the file to new path");

        if (result == PopupButtons.Cancel)
        {
            return;
        }

        New<UserSettings>().TemporaryFilePath = TempConfigPath;
        await ShutDownAnd(New<IUIThread>().RestartApplication);
    }

    private async Task ShutDownAnd(Action finalAction)
    {
        await new ApplicationManager().ShutdownBackgroundSafe();
        await EncryptPendingFiles();

        finalAction();
    }

    private async Task EncryptPendingFiles()
    {
        new ApplicationManager().WaitForBackgroundToComplete();
        await New<MainViewModel>().EncryptPendingFiles.ExecuteAsync(null);
        new ApplicationManager().WaitForBackgroundToComplete();
    }

    public void CancelButton_Click(EventArgs e)
    {
        ErrorMessage = "";
        TempConfigPath = "";
        new AdvancedOptionsViewModel();
        LogOnViewModel.AdvancedOptionsDialog.Close();
        UpdateViewState();
        return;
    }
}
