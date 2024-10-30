using AxCrypt.App.Components.Models;

namespace AxCrypt.App.Components.Services
{
    public interface ISupportService
    {
        void SendPremiumSupportRequestEmail(SupportViewModel model);
    }
}