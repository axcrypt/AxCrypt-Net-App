using AxCrypt.App.Shared.Services.Interface;
using System.Collections.Generic;

namespace AxCrypt.App.Shared.Services;

public class CssService : ICssService
{
    public IList<KeyValuePair<string, string>> ApplySubscriptionCssAsync(string subscriptionLevel)
    {
        string cssBasePath = subscriptionLevel switch
        {
            "business" => "business",
            "premium" => "premium",
            "passwordmanager" => "free",
            "free" => "free",
            _ => "free"
        };

        IList<KeyValuePair<string, string>> cssFiles = new List<KeyValuePair<string, string>>()
        {
            new KeyValuePair<string, string>(cssBasePath, "sidemenu.min.css"),
            new KeyValuePair<string, string>(cssBasePath, "index.min.css"),
            new KeyValuePair<string, string>(cssBasePath, "site.min.css"),
            new KeyValuePair<string, string>(cssBasePath, "topmenu.min.css"),
            new KeyValuePair<string, string>(cssBasePath, "notification.min.css"),
            new KeyValuePair<string, string>(cssBasePath, "newsecret.min.css"),
            new KeyValuePair<string, string>(cssBasePath, "secretlist.min.css"),
            new KeyValuePair<string, string>(cssBasePath, "securedmessenger.min.css"),
            new KeyValuePair<string, string>(cssBasePath, "support.min.css"),
        };

        if (subscriptionLevel != "free")
        {
            cssFiles.Add(new KeyValuePair<string, string>(cssBasePath, "sharesecret.min.css"));
            cssFiles.Add(new KeyValuePair<string, string>(cssBasePath, "findfiles.min.css"));
            cssFiles.Add(new KeyValuePair<string, string>(cssBasePath, "filepicker.min.css"));
            cssFiles.Add(new KeyValuePair<string, string>(cssBasePath, "vault.min.css"));
            cssFiles.Add(new KeyValuePair<string, string>(cssBasePath, "vaultsettings.min.css"));
            cssFiles.Add(new KeyValuePair<string, string>(cssBasePath, "textencryption.min.css"));
        }

        return cssFiles;
    }
}