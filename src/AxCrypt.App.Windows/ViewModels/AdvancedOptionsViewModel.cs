using AxCrypt.Abstractions;
using AxCrypt.Content;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Core.UI;
using AxCrypt.App.Components.Services.Interface;
using Microsoft.AspNetCore.Components;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.ViewModels;

public class AdvancedOptionsViewModel : ComponentBase
{
    public string? _tempConfigPath { get; set; }
    public string? AppConfigPath { get; set; }

    public IFolderPicker? FolderPicker { get; set; }

    public AdvancedOptionsViewModel(IFolderPicker folderPicker)
    {
        FolderPicker = folderPicker;
    }

    public void Initialize()
    {
        AppConfigPath = New<WorkFolder>().FileInfo.FullName;
        _tempConfigPath = New<UserSettings>().TemporaryFilePath;
    }

    public async void BrowseButton_click(EventArgs e)
    {
        string selectedPath = await FolderPicker.PickFolderAsync();
        if (selectedPath != null)
        {
            _tempConfigPath = selectedPath;
            StateHasChanged();
        }
    }

    public async void ButtonOk_Click(EventArgs e)
    {
        if (string.IsNullOrEmpty(_tempConfigPath))
        {
            return;
        }

        if (_tempConfigPath == New<UserSettings>().TemporaryFilePath)
        {
            return;
        }

        if (!New<IVerifySignInPassword>().Verify(Texts.ChangeOptionGenericWarning))
        {
            return;
        }

        PopupButtons result = await New<IPopup>().ShowAsync(PopupButtons.OkCancel, Texts.IncludeSubfoldersConfirmationTitle, "Are you want to confirm to move all the file to new path");

        if (result == PopupButtons.Cancel)
        {
            return;
        }

        New<UserSettings>().TemporaryFilePath = _tempConfigPath;
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
        return;
    }
}
