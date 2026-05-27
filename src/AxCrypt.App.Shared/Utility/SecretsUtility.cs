using AxCrypt.App.Shared.Models.Secret;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Utility;

public static class SecretsUtility
{
    /// <summary>
    /// MM/YYYY input masker.
    ///
    /// Behaviour:
    ///   • The user types or pastes anything — letters, slashes, junk — and
    ///     we keep only the digits (up to 6: 2 for the month, 4 for the year).
    ///   • As soon as the user enters the second month digit, we auto-append
    ///     a "/" so the next keystroke lands in the year (e.g. typing "1" then
    ///     "2" shows "12/" — slash appears automatically).
    ///   • Once a year digit exists, the format is "MM/YYYY".
    ///   • Backspace stays usable: when the new value is shorter than the
    ///     previously bound value the user is deleting, so we don't re-append
    ///     the slash they just removed — otherwise it would be impossible to
    ///     backspace past "12/".
    /// </summary>
    public static void OnExpirationDateInput(this SecretCardViewModel cardViewModel, ChangeEventArgs e)
    {
        string input = e?.Value?.ToString() ?? string.Empty;
        string previousValue = cardViewModel.ExpirationDate ?? string.Empty;
        bool isDeleting = input.Length < previousValue.Length;

        // Keep up to 6 digits (MM + YYYY); strip everything else (incl. any
        // slashes the user typed — we'll re-insert ours at the canonical spot).
        string digits = new string(input.Where(char.IsDigit).Take(6).ToArray());

        string formatted;
        if (digits.Length < 2)
        {
            formatted = digits;
        }
        else if (digits.Length == 2)
        {
            // Auto-insert the slash after MM — but only while growing.
            // While shrinking we keep just the two digits so the user can
            // backspace further.
            formatted = isDeleting ? digits : $"{digits}/";
        }
        else
        {
            formatted = $"{digits[..2]}/{digits[2..]}";
        }

        cardViewModel.ExpirationDate = formatted;

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
