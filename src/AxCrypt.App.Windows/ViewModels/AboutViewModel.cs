using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.ViewModels;

public class AboutViewModel
{
    public string? SubscriptionStatusAndExpiration { get; set; }
    public string? ProductName { get; private set; }
    public string? Version { get; private set; }
    public string? Copyright { get; private set; }

    public void InitializeProperties()
    {
        SubscriptionStatusAndExpiration = new Display().GetLicenseStatusAndExpiration();
        ProductName = New<AboutAssembly>().AssemblyProduct;
        Version = New<AboutAssembly>().AboutVersionText;
        Copyright = New<AboutAssembly>().AssemblyCopyright;
    }
}
