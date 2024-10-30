using AxCrypt.Content;
using System.ComponentModel.DataAnnotations;

namespace AxCrypt.App.Components.Models.Secret
{
    public class SecretPasswordViewModel : SecretBaseViewModel
    {
        public static SecretPasswordViewModel Empty = new SecretPasswordViewModel("", "", "", "", "");

        public SecretPasswordViewModel(string title, string url, string username, string secretDesc, string secretValue) : base(secretDesc)
        {
            Title = title;
            Url = url;
            Username = username;
            SecretValue = secretValue;
        }

        public string Title { get; set; }

        [Display(Name = nameof(Texts.PasswordUrlPrompt), ResourceType = typeof(Content.Resource))]
        public string Url { get; set; }

        [StringLength(256)]
        public string Username { get; set; }

        [Required, StringLength(1000)]
        [Display(Name = nameof(Texts.PromptXecretsSecretText), ResourceType = typeof(Content.Resource))]
        public string SecretValue { get; set; }
    }
}