using AxCrypt.Api.Model;

namespace AxCrypt.App.Windows.Models;

public class TopMenuModel
{
    public SubscriptionLevel SubscriptionLevel { get; set; }

    public bool IsWideScreen { get; set; }

    public bool IsLargeScreen { get; set; }

    public string? SelectedLanguage { get; set; }

    public string SelectedLanguageImageUrl { get; set; } = "images/flag/FrmEng.svg";

    public string SelectedLanguageDisplayName { get; set; } = "Eng(US)";

    public string? UserEmail { get; set; }
}
