using AxCrypt.App.Shared.Services.Interface;
using System.Collections.Generic;

namespace AxCrypt.App.Shared.Services;

public class CssService : ICssService
{
    public string GetSubscriptionCssBasePath(string subscriptionLevel)
    {
        return (subscriptionLevel ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "business" => "business",
            "premium" => "premium",
            "passwordmanager" => "premium",
            "free" => "free",
            _ => "free"
        };
    }

    public IList<KeyValuePair<string, string>> ApplySubscriptionCssAsync(string subscriptionLevel)
    {
        string cssBasePath = GetSubscriptionCssBasePath(subscriptionLevel);
        bool isFreeLayout = cssBasePath == "free";
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
            new KeyValuePair<string, string>(cssBasePath, "modern.min.css")
        };

        if (isFreeLayout)
        {
            cssFiles.Add(new KeyValuePair<string, string>(cssBasePath, "upgradesubscription.min.css"));
        }
        else
        {
            cssFiles.Add(new KeyValuePair<string, string>(cssBasePath, "sharesecret.min.css"));
            cssFiles.Add(new KeyValuePair<string, string>(cssBasePath, "filepicker.min.css"));
            cssFiles.Add(new KeyValuePair<string, string>(cssBasePath, "textencryption.min.css"));
        }

        return cssFiles;
    }
}
