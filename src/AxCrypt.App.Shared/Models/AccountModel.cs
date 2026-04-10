using AxCrypt.Api.Model;
using AxCrypt.App.Entitlement.Contracts;

namespace AxCrypt.App.Shared.Models;

public class AccountModel
{
    public string? UserEmail { get; set; }
    public int DaysLeft { get; set; }
    public SubscriptionLevel SubscriptionLevel { get; set; }
    public string? Subscription { get; set; }
    public DateTime CreatedTime { get; set; } = DateTime.MinValue;
    public bool IsLoggedOn { get; set; }
    public bool ImportPass { get; set; }
    public bool CreateId { get; set; }
    public bool IsImportAxCryptIDDisabled { get; set; } = true;

    /// <summary>
    /// The plan the user originally signed up on. Sticky across sign-ins;
    /// drives feature-targeted upgrade copy and the Upgrade page filter.
    /// </summary>
    public SignUpFrom SignUpFrom { get; set; } = SignUpFrom.None;
}