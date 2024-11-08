using AxCrypt.Abstractions;
using AxCrypt.Content;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Core.UI;
using AxCrypt.App.Components.Services.Interface;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.ViewModels;

public class AdvancedOptionsViewModel
{
    public string? _tempConfigPath { get; set; }

    public IFolderPicker? FolderPicker { get; set; }

    private async void BrowseButton_click(object sender, EventArgs e)
    {
        string selectedPath = await FolderPicker.PickFolderAsync();
        if (selectedPath != null)
        {
            _tempConfigPath = selectedPath;
        }
    }

    private async void ButtonOk_Click(object sender, EventArgs e)
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

    private void CancelButton_Click(object sender, EventArgs e)
    {
        return; 
    }
}
