using AxCrypt.App.Shared.Models.Secret;
using System.Globalization;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Utility;

public static class SecretsUtility
{
    public static void OnExpirationDateInput(this SecretCardViewModel cardViewModel, Microsoft.AspNetCore.Components.ChangeEventArgs e)
    {
        cardViewModel.ExpirationDateError = null!;

        string input = e?.Value?.ToString() ?? string.Empty;

        if (input.Length > 6) input = input[..7];
        string formatted = input;
        if (!input.Contains("/"))
        {
            formatted = input.Length switch
            {
                >= 5 => $"{input[..2]}/{input[2..]}",
                >= 3 => $"{input[..2]}/{input[2..]}",
                _ => input
            };
        }

        cardViewModel.ExpirationDate = formatted;

        if (formatted.Length != 5 && formatted.Length != 7)
        {
            cardViewModel.ExpirationDateError = "Enter date in MM/YY or MM/YYYY format.";
            return;
        }

        string parseFormat = formatted.Length == 5 ? "MM/yy" : "MM/yyyy";
        if (!DateTime.TryParseExact(formatted, parseFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
        {
            cardViewModel.ExpirationDateError = "Invalid date.";
            return;
        }

        DateTime currentDateTime = New<Abstractions.INow>().Utc;
        DateTime now = new(currentDateTime.Year, currentDateTime.Month, 1);
        DateTime entered = new(parsed.Year, parsed.Month, 1);
        if (entered < now)
        {
            cardViewModel.ExpirationDateError = "Expiration must be this month or later.";
        }
        else
        {
            cardViewModel.ExpirationDateError = null;
        }
    }
}
