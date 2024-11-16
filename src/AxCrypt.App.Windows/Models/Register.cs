using AxCrypt.App.Components.Services;
using AxCrypt.App.Components.Utility;

namespace AxCrypt.App.Windows.Models;

public class RegisterModel
{
    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string? VerifyPassword { get; set; }

    public string ErrorMessage { get; set; }

    public DialogResult DialogResult { get; set; }
}
