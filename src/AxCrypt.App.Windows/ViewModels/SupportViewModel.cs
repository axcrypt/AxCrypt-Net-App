using AxCrypt.App.Windows.Helpers;
using AxCrypt.App.Windows.Services;
using AxCrypt.App.Windows.Models;
using Microsoft.JSInterop;
using AxCrypt.Content;
using AxCrypt.Api.Model;
using AxCrypt.Common;

using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.App.Components.Models;

namespace AxCrypt.App.Windows.ViewModels;

public class SupportViewModel
{
    private readonly SupportService _supportService;
    private readonly IJSRuntime _jsRuntime;

    public SupportModel Model { get; set; }
    public bool IsWideScreen { get; set; }
    public bool IsLoading { get; set; }
    public string ErrorMessage { get; set; }
    public bool SubmittedSuccess { get; set; }

    public SupportViewModel(SupportService supportService, IJSRuntime jsRuntime)
    {
        _supportService = supportService;
        _jsRuntime = jsRuntime;
        Model = new SupportModel();
        Model.SubscriptionLevel = New<AccountStatusViewModel>().SubscriptionLevel;
    }

    public async Task<bool> SubmitSupportAsync()
    {
        if (New<AxCryptOnlineState>().IsOffline)
        {
            ErrorMessage = Texts.NoInternetErrorMessage;
            return false;
        }

        if (string.IsNullOrEmpty(Model.Body))
        {
            ErrorMessage = "Fill the required(marked *) fields!";
            return false;
        }

        bool submitted = false;
        Model.Subject = Texts.PromptSupport;
        if (AxCrypt.App.Components.Data.Utility.IsPasswordManager)
        {
            Model.Subject = $"{nameof(SubscriptionLevel.PasswordManager)} {Texts.PromptSupport}";
        }
        if (AxCrypt.App.Components.Data.Utility.IsPremiumUser)
        {
            Model.Subject = Texts.PrioritySupportTitle;
        }
        if (AxCrypt.App.Components.Data.Utility.IsBusinessUser)
        {
            Model.Subject = Texts.BusinessPrioritySupportTitle;
        }

        submitted = await _supportService.SendPremiumSupportRequestEmail(Model.Subject, Model.Body);
        SubmittedSuccess = submitted;
        return submitted;
    }

    public async Task InitializeScreenPropertiesAsync()
    {
        IsLoading = true;
        try
        {
            IsWideScreen = await Utility.IsWideScreenAsync(_jsRuntime);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error determining screen width: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
