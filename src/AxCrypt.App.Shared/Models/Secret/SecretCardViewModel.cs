using AxCrypt.Content;
using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;

namespace AxCrypt.App.Shared.Models.Secret;

public class SecretCardViewModel : SecretBaseViewModel
{
    public static SecretCardViewModel Empty = new SecretCardViewModel("", "", "", "", "");

    public SecretCardViewModel(string cardNumber, string secretDesc, string nameOnCard, string securityCode, string expirationDate) : base(secretDesc)
    {
        SecretDesc = secretDesc;
        CardNumber = cardNumber;
        NameOnCard = nameOnCard;
        SecurityCode = securityCode;
        ExpirationDate = expirationDate;
    }

    [Required, MinLength(12), MaxLength(19)]
    [RegularExpression("([0-9]+)")]
    [Display(Name = nameof(Texts.CardNumberPrompt), ResourceType = typeof(Content.Resource))]
    public string CardNumber { get; set; }

    [Required, StringLength(256)]
    [Display(Name = nameof(Texts.NameOnCardPrompt), ResourceType = typeof(Content.Resource))]
    public string NameOnCard { get; set; }

    [Required, MinLength(3), MaxLength(4)]
    [RegularExpression("([0-9]+)")]
    [Display(Name = nameof(Texts.SecurityCodePrompt), ResourceType = typeof(Content.Resource))]
    public string SecurityCode { get; set; }

    [Required, StringLength(7)]
    [RegularExpression("([0-9/]+)")]
    [Display(Name = nameof(Texts.ExpirationDatePrompt), ResourceType = typeof(Content.Resource))]
    public string ExpirationDate { get; set; }

    public void OnExpirationDateInput(ChangeEventArgs e)
    {
        string value = e.Value?.ToString() ?? "";

        value = new string(value.Where(char.IsDigit).ToArray());

        if (value.Length > 2)
        {
            value = value.Insert(2, "/");
        }

        if (value.Length > 7)
        {
            value = value.Substring(0, 7);
        }

        ExpirationDate = value;
    }

    public string ExpirationDateError { get; set; } = null;
}