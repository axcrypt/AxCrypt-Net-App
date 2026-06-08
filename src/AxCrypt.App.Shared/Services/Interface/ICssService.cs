namespace AxCrypt.App.Shared.Services.Interface;

public interface ICssService
{
    string GetSubscriptionCssBasePath(string subscriptionLevel);

    IList<KeyValuePair<string, string>> ApplySubscriptionCssAsync(string subscriptionLevel);
}
