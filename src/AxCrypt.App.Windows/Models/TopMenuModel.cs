using AxCrypt.Api.Model;
using AxCrypt.App.Components.Models;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.Models;

public class TopMenuModel
{
    public TopMenuModel()
    {
        SubscriptionLevel = New<AccountStatusViewModel>().SubscriptionLevel;
        UserEmail = New<AccountStatusViewModel>().UserEmail;
    }

    public SubscriptionLevel SubscriptionLevel { get; set; }

    public bool AccountPopup { get; set; }

    public bool SettingsPopup { get; set; }

    public bool NotifyPopup { get; set; }

    public bool ShowDropdown { get; set; }

    public bool IsWideScreen { get; set; }

    public bool IsLargeScreen { get; set; }

    public string? SelectedLanguage { get; set; }

    public string SelectedLanguageImageUrl { get; set; } = "images/flag/FrmEng.svg";

    public string SelectedLanguageDisplayName { get; set; } = "Eng(US)";

    public string? UserEmail { get; set; }
}
