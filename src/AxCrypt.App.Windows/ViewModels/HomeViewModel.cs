using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Core.UI;
using AxCrypt.Core;
using Microsoft.AspNetCore.Components;
using AxCrypt.Core.Session;
using AxCrypt.App.Windows.Code;
using AxCrypt.Common;
using AxCrypt.Core.Extensions;
using System.Globalization;
using AxCrypt.Abstractions;
using AxCrypt.Core.Crypto;
using AxCrypt.Mono;
using AxCrypt.App.Components;

using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.App.Windows.Services;

namespace AxCrypt.App.Windows.ViewModels;

public class HomeViewModel : ISignIn
{
    private readonly ICustomNavigationService _navigationManager;
    private MainViewModel _mainViewModel;
    private FileOperationViewModel _fileOperationViewModel;
    private KnownFoldersViewModel _knownFoldersViewModel;

    public HomeViewModel(ICustomNavigationService navigationManager)
    {
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));

        SetupViewModelsAndNotificationsBeforeAnyNotificationsAreSent();
        //AxCryptMainForm_ShownAsync();
    }

    private void SetupViewModelsAndNotificationsBeforeAnyNotificationsAreSent()
    {
        //New<LicensePolicy>();
        _mainViewModel = New<MainViewModel>();
        _fileOperationViewModel = New<FileOperationViewModel>();
        _knownFoldersViewModel = New<KnownFoldersViewModel>();

        New<SessionNotify>().AddCommand(async (notification) => await New<SessionNotificationHandler>().HandleNotificationAsync(notification));
    }

    public async void AxCryptMainForm_ShownAsync()
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

    private async Task SignInAsync(bool nav = true)
    {
        if (nav)
        {
            _navigationManager.NavigateTo("/login");
            return;
        }

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

    private async Task SetLanguageAsync(string cultureName)
    {
        Resolve.UserSettings.CultureName = cultureName;
        if (Resolve.Log.IsInfoEnabled)
        {
            Resolve.Log.LogInfo("Set new UI language culture to '{0}'.".InvariantFormat(Resolve.UserSettings.CultureName));
        }

        UpdateArabicStyle();

        InitializeContentResources();
        await SetWindowTitleTextAsync(_mainViewModel.LoggedOn);
        //_daysLeftPremiumLabel.UpdateText();
        await SetSoftwareStatus();
    }

    private void InitializeContentResources()
    {
        SetCulture();
    }

    private static void SetCulture()
    {
        if (String.IsNullOrEmpty(Resolve.UserSettings.CultureName))
        {
            return;
        }
        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Resolve.UserSettings.CultureName);
    }

    private async Task SetSignInSignOutStatusAsync(bool isSignedIn)
    {
        await SetWindowTitleTextAsync(isSignedIn);

        //bool isSignedInWithAxCryptId = New<KnownIdentities>().IsLoggedOnWithAxCryptId;

        //_createAccountToolStripMenuItem.Enabled = !isSignedIn;
        //_debugManageAccountToolStripMenuItem.Enabled = isSignedInWithAxCryptId;
        //_exportMyPrivateKeyToolStripMenuItem.Enabled = isSignedInWithAxCryptId;
        //_exportSharingKeyToolStripMenuItem.Enabled = isSignedInWithAxCryptId;
        //_importMyPrivateKeyToolStripMenuItem.Enabled = !isSignedIn;
        //_importOthersSharingKeyToolStripMenuItem.Enabled = isSignedInWithAxCryptId;
        //_inviteUserToolStripMenuItem.Enabled = New<AxCryptOnlineState>().IsOnline && isSignedIn;
        //_optionsEncryptionUpgradeModeToolStripMenuItem.Enabled = isSignedInWithAxCryptId;
        //_optionsChangePassphraseToolStripMenuItem.Enabled = New<AxCryptOnlineState>().IsOnline;
        //_passwordResetToolStripMenuItem.Enabled = !isSignedIn && !string.IsNullOrEmpty(New<UserSettings>().UserEmail);
        //_signInToolStripMenuItem.Visible = !isSignedIn;
        //_notifySignInToolStripMenuItem.Visible = !isSignedIn;
        //_signOutToolStripMenuItem.Visible = isSignedIn;
        //_notifySignOutToolStripMenuItem.Visible = isSignedIn;
        //_encryptionUpgradeMenuItem.Enabled = isSignedInWithAxCryptId;
    }

    public string Text { get; set; }

    private async Task SetWindowTitleTextAsync(bool isLoggedOn)
    {
        Text = await new Display().WindowTitleTextAsync(isLoggedOn);
    }

    private async Task SetSoftwareStatus()
    {
        //_softwareStatusButton.Image = Resources.bulb_green_40px;
        //_softwareStatusButton.Visible = true;
        VersionUpdateStatus status = _mainViewModel.VersionUpdateStatus;
        switch (status)
        {
            case VersionUpdateStatus.ShortTimeSinceLastSuccessfulCheck:
            case VersionUpdateStatus.IsUpToDate:
                //_softwareStatusButton.Visible = false;
                break;

            case VersionUpdateStatus.LongTimeSinceLastSuccessfulCheck:
                //_softwareStatusButton.ToolTipText = Texts.OldVersionTooltip;
                break;

            case VersionUpdateStatus.NewerVersionIsAvailable:
                //_softwareStatusButton.ToolTipText = Texts.NewVersionIsAvailableText.InvariantFormat(_mainViewModel.DownloadVersion.Version) + ' ' + Texts.ClickToDownloadText;
                break;

            case VersionUpdateStatus.Unknown:
                //_softwareStatusButton.ToolTipText = Texts.ClickToCheckForNewerVersionTooltip;
                break;
        }
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

    private void ShowRenewSubscriptionDialog()
    {
        if (!_mainViewModel.LoggedOn || !AxCryptUserAccountViewModel.HadAnyPaidSubscription)
        {
            return;
        }

        //using (RenewSubscriptionPromptDialog dialog = new RenewSubscriptionPromptDialog(this))
        //{
        //    if (dialog.HideDialog)
        //    {
        //        return;
        //    }

        //    if (dialog.ShowDialog(this) != DialogResult.OK)
        //    {
        //        return;
        //    }
        //}
    }
}
