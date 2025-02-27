using AxCrypt.App.Shared.Services.Interface;

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

        string[] cssFiles = new[]
        {
            $"css/{cssBasePath}/sidemenu.min.css",
            $"css/{cssBasePath}/index.min.css",
            $"css/{cssBasePath}/site.min.css",
            $"css/{cssBasePath}/topmenu.min.css",
            $"css/{cssBasePath}/notification.min.css",
            $"css/{cssBasePath}/newsecret.min.css",
            $"css/{cssBasePath}/secretlist.min.css",
            $"css/{cssBasePath}/support.min.css",
            $"css/{cssBasePath}/sharesecret.min.css"
        };

        return cssFiles;
    }
}