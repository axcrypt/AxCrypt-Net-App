using AxCrypt.Api.Model;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Core.UI;
using AxCrypt.Core;
using Microsoft.AspNetCore.Components;
using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.Core.Session;
using AxCrypt.App.Windows.Code;

namespace AxCrypt.App.Windows.ViewModels;

public class HomeViewModel: ISignIn
{
    private readonly NavigationManager _navigationManager;
    private MainViewModel _mainViewModel;
    private FileOperationViewModel _fileOperationViewModel;
    private KnownFoldersViewModel _knownFoldersViewModel;
    public HomeViewModel(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        
        SetupViewModelsAndNotificationsBeforeAnyNotificationsAreSent();
    }

    private void SetupViewModelsAndNotificationsBeforeAnyNotificationsAreSent()
    {
        New<LicensePolicy>();
        _mainViewModel = New<MainViewModel>();
        _fileOperationViewModel = New<FileOperationViewModel>();
        _knownFoldersViewModel = New<KnownFoldersViewModel>();

        New<SessionNotify>().AddCommand(async (notification) => await New<SessionNotificationHandler>().HandleNotificationAsync(notification));
    }

    private async void AxCryptMainForm_ShownAsync(object sender, EventArgs e)
    {
        New<IRuntimeEnvironment>().FirstInstanceIsReady();
        UpdateArabicStyle();
        await SignInAsync();
    }

    public bool IsSigningIn { get; set; }

    public async Task SignIn()
    {
        await _fileOperationViewModel.IdentityViewModel.LogOnAsync.ExecuteAsync(null);
    }

    private async Task SignInAsync()
    {
        SignUpSignIn signUpSignIn = new SignUpSignIn(_navigationManager)
        {
            //Version = _apiVersion,
            UserEmail = New<UserSettings>().UserEmail,
        };

        await signUpSignIn.DialogsAsync(this);

        New<UserSettings>().UserEmail = signUpSignIn.UserEmail;

        if (signUpSignIn.StopAndExit)
        {
            await new ApplicationManager().StopAndExit();
            return;
        }

        await SetSignInSignOutStatusAsync(_mainViewModel.LoggedOn);
        if (_mainViewModel.LoggedOn && Thread.CurrentThread.CurrentUICulture.Name != Resolve.UserSettings.CultureName)
        {
            await SetLanguageAsync(Resolve.UserSettings.CultureName);
        }

        ShowRenewSubscriptionDialog();
    }

    private void UpdateArabicStyle()
    {
        //if (Resolve.UserSettings.CultureName == "ar-AR")
        //{
        //    this.RightToLeft = RightToLeft.Yes;
        //    return;
        //}

        //this.RightToLeft = RightToLeft.No;
    }

}
