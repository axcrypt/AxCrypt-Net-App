using AxCrypt.Content;
using AxCrypt.Api.Model;
using AxCrypt.Common;
using AxCrypt.App.Components.Models;
using AxCrypt.App.Components.Services;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Components.ViewModels;

public class SupportViewModel
{
    private readonly SupportService? _supportService;

    public SupportModel? Model { get; set; }
    public bool IsWideScreen { get; set; }
    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }
    public bool SubmittedSuccess { get; set; }

    public SupportViewModel(SupportService supportService)
    {
        _supportService = supportService;
    }

    public void Initialize()
    {
        Model = new SupportModel();
        Model.SubscriptionLevel = New<AccountStatusViewModel>().SubscriptionLevel;
    }

    public async Task SubmitSupportAsync()
    {
        if (New<AxCryptOnlineState>().IsOffline)
        {
            ErrorMessage = Texts.NoInternetErrorMessage;
            return;
        }

        if (string.IsNullOrEmpty(Model.Body))
        {
            ErrorMessage = "Fill the required(marked *) fields!";
            return;
        }

        bool submitted = false;
        Model.Subject = Texts.PromptSupport;

        if (Model.SubscriptionLevel == SubscriptionLevel.PasswordManager)
        {
            Model.Subject = $"{nameof(SubscriptionLevel.PasswordManager)} {Texts.PromptSupport}";
        }

        if (Model.SubscriptionLevel == SubscriptionLevel.Premium)
        {
            Model.Subject = Texts.PrioritySupportTitle;
        }

        if (Model.SubscriptionLevel == SubscriptionLevel.Business)
        {
            Model.Subject = Texts.BusinessPrioritySupportTitle;
        }

        submitted = await _supportService.SendPremiumSupportRequestEmail(Model.Subject, Model.Body);
        Model.Body = string.Empty;
    }
}
