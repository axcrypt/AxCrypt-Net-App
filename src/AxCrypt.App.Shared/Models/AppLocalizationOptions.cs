namespace AxCrypt.App.Shared.Models;

public class AppLocalizationOptions
{
    public List<CultureOption> SupportedCultures { get; set; } = new List<CultureOption>();

    public AppLocalizationOptions()
    {
        SupportedCultures = new List<CultureOption>
            {
                new CultureOption { Name = "en", DisplayName = "English", ImageUrl = "images/flag/FrmEng.svg" },
                new CultureOption { Name = "ar", DisplayName = "Arabic", ImageUrl = "images/flag/FrmArbc.svg" },
                new CultureOption { Name = "de", DisplayName = "Deutsch", ImageUrl = "images/flag/FrmGrmn.svg" },
                new CultureOption { Name = "fr", DisplayName = "Français", ImageUrl = "images/flag/FrmFrnc.svg" },
                new CultureOption { Name = "it", DisplayName = "Italiano", ImageUrl = "images/flag/FrmItl.svg" },
                new CultureOption { Name = "zh", DisplayName = "中國人", ImageUrl = "images/flag/FrmChn.svg" },
                new CultureOption { Name = "ko", DisplayName = "한국인", ImageUrl = "images/flag/FrmKrn.svg" },
                new CultureOption { Name = "nl", DisplayName = "Nederlands", ImageUrl = "images/flag/FrmNdrl.svg" },
                new CultureOption { Name = "pl", DisplayName = "Polski", ImageUrl = "images/flag/FrmPlnd.svg" },
                new CultureOption { Name = "pt", DisplayName = "Português", ImageUrl = "images/flag/FrmPrtg.svg" },
                new CultureOption { Name = "ru", DisplayName = "Русский", ImageUrl = "images/flag/FrmRssn.svg" },
                new CultureOption { Name = "es", DisplayName = "Spanish", ImageUrl = "images/flag/FrmSpn.svg" },
                new CultureOption { Name = "sv", DisplayName = "Swedish", ImageUrl = "images/flag/FrmSwdn.svg" },
                new CultureOption { Name = "tr", DisplayName = "Türkçe", ImageUrl = "images/flag/FrmTrk.svg" }
            };
    }
}

public class CultureOption
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? ImageUrl { get; set; }
}