namespace AxCrypt.App.Shared.Services.Interface;

public interface ICssService
{
    IList<KeyValuePair<string, string>> ApplySubscriptionCssAsync(string subscriptionLevel);
}