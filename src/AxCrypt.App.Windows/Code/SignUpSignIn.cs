using AxCrypt.Api.Model;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Core.UI;
using Microsoft.AspNetCore.Components;
using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.App.Windows.Services;

namespace AxCrypt.App.Windows.Code;

internal class SignUpSignIn
{
    public string UserEmail { get; set; }

    public bool StopAndExit { get; set; }

    public ApiVersion Version { get; set; }

    private readonly ICustomNavigationService _navigationManager;

    public SignUpSignIn(ICustomNavigationService navigationManager)
    {
        _navigationManager = navigationManager;
    }

    public async Task DialogsAsync(ISignIn signingInState)
    {
        SignupSignInViewModel viewModel = new SignupSignInViewModel(signingInState, new NameOf(nameof(Texts.WelcomeToAxCrypt)), new NameOf(nameof(Texts.MessageAskAboutStartTrialForWindows)))
        {
            UserEmail = UserEmail,
            Version = Version,
        };

        viewModel.BindPropertyChanged(nameof(viewModel.UserEmail), (string email) => UserEmail = email);
        viewModel.BindPropertyChanged(nameof(viewModel.StopAndExit), (bool stop) => StopAndExit = stop);
        //ewModel.BindPropertyChanged(nameof(viewModel.TopControlsEnabled), (bool enabled) => SetTopControlsEnabled(parent, enabled));

        viewModel.CreateAccount = (e) =>
        {
            _navigationManager.NavigateTo($"/signup?UserEmail={UserEmail}");
            return Task.CompletedTask;
        };

        viewModel.SignInCommandAsync = signingInState.SignIn;

        viewModel.RestoreWindow = () =>
        {
            if (New<UserSettings>().RestoreFullWindow)
            {
                //Styling.RestoreWindowWithFocus(parent);
            }
            return Task.FromResult<object>(null);
        };

        await viewModel.DoAll.ExecuteAsync(null);
    }
}
