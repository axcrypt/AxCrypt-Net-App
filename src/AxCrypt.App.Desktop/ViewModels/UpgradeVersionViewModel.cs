using AxCrypt.App.Desktop.Helpers;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Common;
using AxCrypt.Core.UI;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Desktop.ViewModels;

public class UpgradeVersionViewModel
{
    public UpgradeVersionViewModel()
    {
        LogOnViewModel = AxCServiceProviderExtension.GetService<LogOnViewModel>();
        LogOnViewModel.PopupButtons = PopupButtons.None;
    }

    public UpgradeVersionViewModel(string title, string message, DoNotShowAgainOptions dontShowAgain)
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

    public async Task<PopupButtons> ShowVersionDialog(PopupButtons buttons, string title, string message, DoNotShowAgainOptions dontShowAgain)
    {
        LogOnViewModel!.UpgradeVersion = new UpgradeVersionViewModel(title, message, dontShowAgain);
        LogOnViewModel.PageResult = DialogResult.None;

        DoNotShowAgainOptions savedFlags = New<UserSettings>().DoNotShowAgain;
        DoNotShowAgainOptions currentFlags = LogOnViewModel.UpgradeVersion.DontShowAgainOptions;

        if ((savedFlags & currentFlags) != 0)
        {
            return LogOnViewModel.PopupButtons; 
        }

        LogOnViewModel.UpgradeVersionDialog.Show();

        while (LogOnViewModel.PageResult == DialogResult.None)
        {
            await Task.Delay(1000);
        }

        LogOnViewModel.UpgradeVersionDialog.Close();
        return LogOnViewModel.PopupButtons;
    }

    public void Button_OkClicked()
    {
        if (LogOnViewModel.UpgradeVersion!.DontShowAgainOptions != DoNotShowAgainOptions.None && IsCheckboxDontShowThisAgain)
        {
            New<UserSettings>().DoNotShowAgain = New<UserSettings>().DoNotShowAgain | LogOnViewModel.UpgradeVersion.DontShowAgainOptions!;
        }

        LogOnViewModel!.PageResult = DialogResult.OK;
        LogOnViewModel.PopupButtons = PopupButtons.Ok;
    }

    public void Button_CancelClicked()
    {
        if (LogOnViewModel.UpgradeVersion!.DontShowAgainOptions != DoNotShowAgainOptions.None && IsCheckboxDontShowThisAgain)
        {
            New<UserSettings>().DoNotShowAgain = (DoNotShowAgainOptions)(New<UserSettings>().DoNotShowAgain | LogOnViewModel.UpgradeVersion.DontShowAgainOptions)!;
        }

        LogOnViewModel!.PageResult = DialogResult.Cancel;
        LogOnViewModel.PopupButtons = PopupButtons.Cancel;
    }
}