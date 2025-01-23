using AxCrypt.App.Desktop.Services.Interface;

namespace AxCrypt.App.Windows.Services;

public class CssService : ICssService
{
    public string[] ApplySubscriptionCssAsync(string subscriptionLevel)
    {
        string cssBasePath = subscriptionLevel switch
        {
            "business" => "business",
            "premium" => "premium",
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
            $"css/{cssBasePath}/support.min.css"
        };

        return cssFiles;
    }
}
