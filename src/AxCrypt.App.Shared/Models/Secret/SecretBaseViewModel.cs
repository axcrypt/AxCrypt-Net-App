using System.ComponentModel.DataAnnotations;

namespace AxCrypt.App.Shared.Models.Secret;

public class SecretBaseViewModel
{
    public SecretBaseViewModel(string secretDesc)
    {
        SecretDesc = secretDesc;
    }

    [Required, StringLength(100000)]
    [Display(Name = "Description")]
    public string? SecretDesc { get; set; }
}