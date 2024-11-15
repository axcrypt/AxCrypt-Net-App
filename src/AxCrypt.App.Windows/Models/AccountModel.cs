using AxCrypt.Api.Model;

namespace AxCrypt.App.Windows.Models;

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
}
