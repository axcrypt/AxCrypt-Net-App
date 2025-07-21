using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Core.UI;

namespace AxCrypt.App.Shared.Services;

public class UpgradeVersionService : IUpgradeVersionService
{
    public UpgradeVersionService()
    {
        LogOnViewModel = AxCServiceProviderExtension.LogOnViewModel;
    }

    public LogOnViewModel? LogOnViewModel { get; set; }

    public async Task<PopupButtons> ShowDialogAsync(PopupButtons buttons, string title, string message)
    {
        LogOnViewModel!.UpgradeVersionViewModel = new UpgradeVersionViewModel(buttons, message);
        LogOnViewModel!.UpgradeVersionDialog.Show();

        while (LogOnViewModel.UpgradeVersionViewModel.PopupResult == DialogResult.None)
        {
            await Task.Delay(1000);
        }

        LogOnViewModel.UpgradeVersionDialog.Close();
        return LogOnViewModel!.UpgradeVersionViewModel.SelectedButton;
    }
}