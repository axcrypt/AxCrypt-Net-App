using AxCrypt.Abstractions;
using AxCrypt.Content;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Core.UI;
using AxCrypt.App.Components.Services.Interface;
using Microsoft.AspNetCore.Components;
using AxCrypt.App.Components.Models;

using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.App.Windows.Services;

namespace AxCrypt.App.Windows.ViewModels;

public class AdvancedOptionsViewModel : ComponentBase
{
    public string? TempConfigPath { get; set; }
    public string? AppConfigPath { get; set; }

    private LogOnViewModel _logOnViewModel;

    public AdvancedOptionsViewModel()
    {
        _logOnViewModel = AxCServiceProvider.LogOnViewModel!; 
    }

    public void Initialize()
    {
        AppConfigPath = New<WorkFolder>().FileInfo.FullName;
        TempConfigPath = New<UserSettings>().TemporaryFilePath;
    }

    public bool ShowAdvancedOptions { get; set; }
    public string ErrorMessage { get; set; }

    public async void BrowseButton_click(EventArgs e)
    {
        IFolderPicker folderPicker = new Services.FolderPickerWindows();
        string selectedPath = await folderPicker.PickFolderAsync();
        if (selectedPath != null)
        {
            TempConfigPath = selectedPath;
        }
        _logOnViewModel.UIStateChanged();
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
        ShowAdvancedOptions = false;
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
        ShowAdvancedOptions = false;
        return;
    }

    void Update()
    {
        InvokeAsync(() =>
        {
            StateHasChanged();
        });
    }
}
