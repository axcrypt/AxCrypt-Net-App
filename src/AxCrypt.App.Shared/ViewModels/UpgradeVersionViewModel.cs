using AxCrypt.App.Shared.Utility;
using AxCrypt.Core.UI;

namespace AxCrypt.App.Shared.ViewModels;

public class UpgradeVersionViewModel
{
    public UpgradeVersionViewModel(PopupButtons buttons, string title)
    {
        AvailablePopupButtons = possibleButtons.Where(b => buttons.HasFlag(b)).ToArray();
        Title = title;
    }

    private readonly PopupButtons[] possibleButtons = new PopupButtons[]
    {
        PopupButtons.Ok,
        PopupButtons.Cancel,
        PopupButtons.Exit,
    };

    public string? Title { get; set; }

    public PopupButtons[]? AvailablePopupButtons { get; set; }

    public DialogResult PopupResult { get; set; }

    public PopupButtons SelectedButton { get; set; }
}

