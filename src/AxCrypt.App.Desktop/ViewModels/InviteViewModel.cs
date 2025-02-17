using AxCrypt.Api.Model;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.Content;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Desktop.ViewModels;

public class InviteViewModel
{
    private IStatusAlertService _alerService;

    public InviteViewModel(IStatusAlertService alertService)
    {
        //_alerService = AxCServiceProviderExtension.StatusAlertService!;
        _alerService = alertService!;
    }

    public string? ErrorMessage { get; set; }

    public bool InviteUserDisabled { get; set; }

    public string? InvitedUser { get; set; }

    public async Task InviteFriend()
    {
        if (InviteUserDisabled)
        {
            return;
        }

        if (!AdHocValidationDueToMonoLimitations())
        {
            return;
        }

        IEnumerable<UserPublicKey> userPublicKey = await EnsureUserAccountStatusAndGetInvitedUserPublicKey();
        if (userPublicKey != null)
        {
            OnUserInviteCompleted();
        }
    }

    private bool AdHocValidationDueToMonoLimitations()
    {
        bool validated = AdHocValidateAllFieldsIndependently();
        return validated;
    }

    private bool AdHocValidateAllFieldsIndependently()
    {
        return AdHocValidateUserEmail();
    }

    private bool AdHocValidateUserEmail()
    {
        ErrorMessage = "";
        if (String.IsNullOrEmpty(InvitedUser) || !InvitedUser.IsValidEmail())
        {
            ErrorMessage = Texts.BadEmail;
            return false;
        }
        return true;
    }

    private async Task<IEnumerable<UserPublicKey>> EnsureUserAccountStatusAndGetInvitedUserPublicKey()
    {
        EmailAddress invitedEmail = EmailAddress.Parse(InvitedUser);
        AccountStatus accountStatus = await invitedEmail.GetValidEmailAccountStatusAsync(New<KnownIdentities>().DefaultEncryptionIdentity);
        if (!ShowInviteUserDialog(accountStatus))
        {
            return null;
        }

        IEnumerable<EmailAddress> invitedEmails = new EmailAddress[] { invitedEmail };
        return await invitedEmails.ToAvailableKnownPublicKeysAsync(New<KnownIdentities>().DefaultEncryptionIdentity);
    }

    private bool ShowInviteUserDialog(AccountStatus accountStatus)
    {
        if (accountStatus == AccountStatus.Offline || accountStatus == AccountStatus.Unknown)
        {
            ShowOfflineOrLocalError();
            return false;
        }
        if (accountStatus != AccountStatus.NotFound)
        {
            return true;
        }

        return true;
    }

    private void ShowOfflineOrLocalError()
    {
        InviteUserDisabled = true;
        InvitedUser = $"[{Texts.OfflineIndicatorText}]";
        ErrorMessage = Texts.KeySharingOffline;
    }

    public void OnUserInviteCompleted()
    {
        _alerService.Success($"You send invitation to {InvitedUser} successfully");
        Initialize();
    }

    public void Initialize()
    {
        InvitedUser = "";
        ErrorMessage = "";
        InviteUserDisabled = false;
    }
}