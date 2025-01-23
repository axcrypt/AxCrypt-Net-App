using AxCrypt.Content;
using AxCrypt.Api.Model;
using AxCrypt.Common;

using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Models;

namespace AxCrypt.App.Shared.ViewModels;

public class SupportViewModel
{
    private readonly SupportService? _supportService;

    public SupportModel? Model { get; set; }
    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }

    public SupportViewModel(SupportService supportService)
    {
        _supportService = supportService;
        Initialize();
    }

    public void Initialize()
    {
        Model = new SupportModel();
    }

    public async Task SubmitSupportAsync(SubscriptionLevel SubscriptionLevel)
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

        switch (SubscriptionLevel)
        {
            case SubscriptionLevel.Business:
                Model.Subject = Texts.BusinessPrioritySupportTitle;
                break;
            case SubscriptionLevel.Premium:
                Model.Subject = Texts.PrioritySupportTitle;
                break;
            case SubscriptionLevel.PasswordManager:
                Model.Subject = $"{nameof(SubscriptionLevel.PasswordManager)} {Texts.PromptSupport}";
                break;
        }

        submitted = await _supportService.SendPremiumSupportRequestEmail(Model.Subject, Model.Body);
        Model.Body = string.Empty;
    }
}
