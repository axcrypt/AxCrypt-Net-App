using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Common;
using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.ViewModels;

public class GlobalDialogViewModel
{
    public GlobalDialogViewModel()
    {
        LogOnViewModel = AxCServiceProviderExtension.LogOnViewModel;
        LogOnViewModel!.PopupButtons = [PopupButtons.None];
    }

    public GlobalDialogViewModel(string title, string message, DoNotShowAgainOptions dontShowAgain)
    {
        Title = title;
        MessageText = message;
        DontShowAgainOptions = dontShowAgain;
    }

    public string? Title { get; set; }

    public string? MessageText { get; set; }

    public DoNotShowAgainOptions DontShowAgainOptions { get; set; }

    public bool IsCheckboxDontShowThisAgain { get; set; }

    public LogOnViewModel? LogOnViewModel { get; set; }

    public async Task<PopupButtons[]> ShowPopupDialog(PopupButtons[] buttons, string title, string message, DoNotShowAgainOptions dontShowAgain)
    {
        LogOnViewModel!.GlobalViewModel = new GlobalDialogViewModel(title, message, dontShowAgain);
        LogOnViewModel.PopupResult = DialogResult.None;
        LogOnViewModel.PopupButtons = buttons;

        if ((New<UserSettings>().DoNotShowAgain & dontShowAgain) != 0)
        {
            return LogOnViewModel.PopupButtons;
        }

        LogOnViewModel.GlobalPopupDialog.Show();

        while (LogOnViewModel.PopupResult == DialogResult.None)
        {
            await Task.Delay(1000);
        }

        LogOnViewModel.GlobalPopupDialog.Close();
        return LogOnViewModel.PopupButtons;
    }

    public void Button_OkClicked()
    {
        if (LogOnViewModel!.GlobalViewModel!.DontShowAgainOptions != DoNotShowAgainOptions.None && IsCheckboxDontShowThisAgain)
        {
            New<UserSettings>().DoNotShowAgain = New<UserSettings>().DoNotShowAgain | LogOnViewModel.GlobalViewModel.DontShowAgainOptions!;
        }

        LogOnViewModel!.PopupResult = DialogResult.OK;
        LogOnViewModel.PopupButtons = [PopupButtons.Ok];
        LogOnViewModel.GlobalPopupDialog.Close();
    }

    public void Button_CancelClicked()
    {
        if (LogOnViewModel!.GlobalViewModel!.DontShowAgainOptions != DoNotShowAgainOptions.None && IsCheckboxDontShowThisAgain)
        {
            New<UserSettings>().DoNotShowAgain = (DoNotShowAgainOptions)(New<UserSettings>().DoNotShowAgain | LogOnViewModel.GlobalViewModel.DontShowAgainOptions)!;
        }

        LogOnViewModel!.PopupResult = DialogResult.Cancel;
        LogOnViewModel.PopupButtons = [PopupButtons.Cancel];
        LogOnViewModel.GlobalPopupDialog.Close();
    }
}