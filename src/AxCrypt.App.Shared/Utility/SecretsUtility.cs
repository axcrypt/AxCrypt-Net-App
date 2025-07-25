using AxCrypt.App.Shared.Models.Secret;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Utility;

public static class SecretsUtility
{
    public static void OnExpirationDateInput(this SecretCardViewModel cardViewModel, ChangeEventArgs e)
    {
        string input = e?.Value?.ToString() ?? string.Empty;
        if (input.Length > 6)
            input = input[..7];

        if (!input.Contains("/"))
        {
            input = input.Length switch
            {
                >= 5 => $"{input[..2]}/{input[2..]}",
                >= 3 => $"{input[..2]}/{input[2..]}",
                _ => input
            };
        }
        cardViewModel.ExpirationDate = input;

        ValidateExpirationDate(cardViewModel);
    }

    public static bool ValidateExpirationDate(this SecretCardViewModel cardViewModel)
    {
        cardViewModel.ExpirationDateError = null!;

        string formatted = cardViewModel.ExpirationDate;
        if (formatted.Length != 5 && formatted.Length != 7)
        {
            cardViewModel.ExpirationDateError = "Enter date in MM/YY or MM/YYYY format.";
            return false;
        }

        string parseFormat = formatted.Length == 5 ? "MM/yy" : "MM/yyyy";
        if (!DateTime.TryParseExact(formatted,parseFormat,CultureInfo.InvariantCulture,DateTimeStyles.None,out DateTime parsed))
        {
            cardViewModel.ExpirationDateError = "Invalid month.";
            return false;
        }

        DateTime now = New<Abstractions.INow>().Utc;
        DateTime currentMonth = new(now.Year, now.Month, 1);
        DateTime enteredMonth = new(parsed.Year, parsed.Month, 1);

        if (enteredMonth < currentMonth)
        {
            cardViewModel.ExpirationDateError = "Expiration must be this month or later.";
            return false;
        }

        return true;
    }
}
