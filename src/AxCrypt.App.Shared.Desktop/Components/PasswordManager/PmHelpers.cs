// Pure static helpers shared across all PasswordManager child components.
// No DI — every method is deterministic given its inputs.

using AxCrypt.Api.Model.Secret;
using AxCrypt.App.Shared.Models.Secret;
using AxCrypt.Content;
using System;

namespace AxCrypt.App.Shared.Desktop.Components.PasswordManager;

internal static class PmHelpers
{
    // CSS slug for the type icon/badge — drives pmx-row-icon--{key} class.
    public static string TypeKey(SecretType t) => t switch
    {
        SecretType.Card => "card",
        SecretType.Note => "note",
        _ => "password",
    };

    public static string TypeName(SecretType t) => t switch
    {
        SecretType.Card => Texts.CardPrompt,
        SecretType.Note => Texts.NoteLabel,
        _ => Texts.PassphrasePrompt,
    };

    public static string DisplayTitle(SecretViewModel s)
    {
        string title = s.SecretType switch
        {
            SecretType.Card => s.Card?.SecretDesc ?? "",
            SecretType.Note => s.Note?.SecretDesc ?? "",
            _ => s.Password?.SecretDesc ?? "",
        };
        return string.IsNullOrWhiteSpace(title) ? "(Untitled)" : title;
    }

    public static string DisplaySubtitle(SecretViewModel s) => s.SecretType switch
    {
        SecretType.Password => DomainOrUser(s.Password),
        SecretType.Card => MaskCard(s.Card?.CardNumber),
        SecretType.Note => "Secure note",
        _ => "",
    };

    // Shows domain when URL is set, otherwise the username.
    public static string DomainOrUser(SecretPasswordViewModel? p)
    {
        if (p == null) return "";
        if (!string.IsNullOrWhiteSpace(p.Url))
        {
            string clean = p.Url.Replace("https://", "").Replace("http://", "").Replace("www.", "");
            int slash = clean.IndexOf('/');
            return slash > 0 ? clean[..slash] : clean;
        }
        return p.Username ?? "";
    }

    public static string Mask(string? value)
        => string.IsNullOrEmpty(value) ? "—" : new string('•', Math.Min(value.Length, 16));

    // Shows only the last 4 digits of a card number.
    public static string MaskCard(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber)) return "—";
        string t = cardNumber.Replace(" ", "").Trim();
        return t.Length < 4 ? "•••• " + t : "•••• " + t[^4..];
    }
}
