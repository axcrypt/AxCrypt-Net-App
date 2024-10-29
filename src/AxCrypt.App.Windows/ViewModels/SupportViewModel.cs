using AxCrypt.Api.Model;
using AxCrypt.App.Windows.Helpers;
using AxCrypt.App.Windows.Services;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.UI.ViewModel;
using System.ComponentModel.DataAnnotations;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.ViewModels;

public class SupportViewModel : ViewModelBase
{
    private SupportService supportService;

    public SupportViewModel(SupportService supportService)
    {
        this.supportService = supportService;
    }

    [Required, StringLength(100000)]
    [Display(Name = nameof(Texts.SupportBodyPrompt), ResourceType = typeof(Content.Resource))]
    public string Body { get; set; }

    public string Subject { get; set; }

    public bool SubmittedSuccess { get; set; } = false;

    public string ErrorMessage { get; set; }

    public async Task<bool> SupportAsync(SupportViewModel model)
    {
        if (New<AxCryptOnlineState>().IsOffline)
        {
            ErrorMessage = Texts.NoInternetErrorMessage;
            return false;
        }

        if (string.IsNullOrEmpty(model.Body))
        {
            ErrorMessage = "Fill the required(marked *) fields!";
            return false;
        }
        bool submitted = false;
        model.Subject = Texts.PromptSupport;
        if (Utility.IsPasswordManager)
        {
            model.Subject = $"{nameof(SubscriptionLevel.PasswordManager)} {Texts.PromptSupport}";
        }
        if (Utility.IsPremiumUser)
        {
            model.Subject = Texts.PrioritySupportTitle;
        }
        if (Utility.IsBusinessUser)
        {
            model.Subject = Texts.BusinessPrioritySupportTitle;
        }

        submitted = await supportService.SendPremiumSupportRequestEmail(model.Subject, model.Body);
        SubmittedSuccess = submitted;
        return submitted;
    }
}
