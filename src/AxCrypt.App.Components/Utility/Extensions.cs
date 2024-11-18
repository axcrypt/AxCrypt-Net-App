using AxCrypt.Api.Model;
using AxCrypt.Core.Runtime;

namespace AxCrypt.App.Components.Utility
{
    public static class Extensions
    {
        public static SubscriptionLevel GetLicenseStatus(this LicenseCapabilities licenseCapabilities)
        {
            if (licenseCapabilities == null)
            {
                return SubscriptionLevel.Unknown;
            }

            if (licenseCapabilities.Has(LicenseCapability.Business))
            {
                return SubscriptionLevel.Business;
            }

            if (licenseCapabilities.Has(LicenseCapability.Premium))
            {
                return SubscriptionLevel.Premium;
            }

            if (licenseCapabilities.Has(LicenseCapability.PasswordManager))
            {
                return SubscriptionLevel.PasswordManager;
            }

            if (licenseCapabilities.Has(LicenseCapability.Viewer))
            {
                return SubscriptionLevel.Free;
            }

            return SubscriptionLevel.Free;
        }

    }
}
