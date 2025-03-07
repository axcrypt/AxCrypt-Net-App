using AxCrypt.App.Shared.Services.Interface;
using System.Collections.Generic;
using System.Linq;

namespace AxCrypt.App.Desktop.Services;

public class CssService : ICssService
{
    public string[] ApplySubscriptionCssAsync(string subscriptionLevel)
    {
        string cssBasePath = subscriptionLevel switch
        {
            "business" => "business",
            "premium" => "premium",
            "passwordmanager" => "free",
            "free" => "free",
            _ => "default"
        };

        IList<string> cssFiles = new List<string>
        {
            $"css/{cssBasePath}/sidemenu.min.css",
            $"css/{cssBasePath}/index.min.css",
            $"css/{cssBasePath}/site.min.css",
            $"css/{cssBasePath}/topmenu.min.css",
            $"css/{cssBasePath}/notification.min.css",
            $"css/{cssBasePath}/newsecret.min.css",
            $"css/{cssBasePath}/secretlist.min.css",
            $"css/{cssBasePath}/securedmessenger.min.css",
            $"css/{cssBasePath}/support.min.css",
        };

        if (subscriptionLevel != "free")
            cssFiles.Add($"css/{cssBasePath}/sharesecret.min.css");

        return cssFiles.ToArray();
    }
}