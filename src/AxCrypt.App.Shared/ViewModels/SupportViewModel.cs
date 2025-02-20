using AxCrypt.Content;
using AxCrypt.Api.Model;
using AxCrypt.Common;

using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Services.Interface;

namespace AxCrypt.App.Shared.ViewModels;

public class SupportViewModel
{
    private readonly SupportService? _supportService;
    private readonly IStatusAlertService statusAlertService;

    public string Body { get; set; } = "";

    public string Subject { get; set; }

    public bool IsLoading { get; set; }

    public string? ErrorMessage { get; set; }

    public SupportViewModel(SupportService supportService)
    {
        _supportService = supportService;
        statusAlertService = AxCServiceProvider.GetService<IStatusAlertService>();
        Initialize();
    }

    public void Initialize()
    {
    }

    public async Task SubmitSupportAsync(SubscriptionLevel SubscriptionLevel)
    {
        if (New<AxCryptOnlineState>().IsOffline)
        {
            ErrorMessage = Texts.NoInternetErrorMessage;
            return;
        }

        if (string.IsNullOrEmpty(Body))
        {
            ErrorMessage = "Fill the required(marked *) fields!";
            return;
        }

        bool submitted = false;
        Subject = Texts.PromptSupport;

        switch (SubscriptionLevel)
        {
            case SubscriptionLevel.Business:
                Subject = Texts.BusinessPrioritySupportTitle;
                break;
            case SubscriptionLevel.Premium:
                Subject = Texts.PrioritySupportTitle;
                break;
            case SubscriptionLevel.PasswordManager:
                Subject = $"{nameof(SubscriptionLevel.PasswordManager)} {Texts.PromptSupport}";
                break;
        }

        submitted = await _supportService.SendPremiumSupportRequestEmail(Subject, Body);
        if (submitted) 
        {
            statusAlertService.Success("Successfully send the mail to Premium support");
        }
        else
        {
            statusAlertService.Error("There are some error, Please try again!");
        }
        Body = string.Empty;
    }
}
