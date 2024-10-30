using AxCrypt.App.Components.Services;
using AxCrypt.Core.UI;
using System.ComponentModel.DataAnnotations;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Components.Models
{
    public class LoginModel
    {
        private readonly INavigationManagerService _navigateTo;

        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public bool RememberMe { get; set; }

        public string ErrorMessage { get; set; }

        public LoginModel()
        {
            RememberMe = New<UserSettings>().RememberMe;
            if (RememberMe)
            {
                Email = New<UserSettings>().UserEmail;
            }
        }

        public LoginModel(Services.INavigationManagerService navigateTo)
        {
            
       }
    }
}