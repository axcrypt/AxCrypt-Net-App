using AxCrypt.Core.Extensions;
using AxCrypt.Api.Model;
using AxCrypt.Core.UI;
using AxCrypt.App.Components.Services.Interface;
using AxCrypt.Core.Crypto.Asymmetric;

using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.App.Windows.Services;

namespace AxCrypt.App.Windows.ViewModels;

public class InviteViewModel
{
    private IStatusAlertService _alerService;

    public InviteViewModel()
    {
        _alerService = AxCServiceProvider.StatusAlertService!;
    }

    public string? ErrorMessage { get;  set; }
    public string? InvitedUser { get; set; }

    public void OnInputFocus()
    {
        ErrorMessage = string.Empty;
    }

    public async Task InviteFriend()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrEmpty(InvitedUser))
        {
            ErrorMessage = "Please enter a valid email.";
            return;
        }

        bool isInvited = await EnsureUserAccountStatusAndGetInvitedUserPublicKey();
        if (isInvited)
        {
            _alerService.Success($"You send invitation to {InvitedUser} successfully");
            InvitedUser = "";
            ErrorMessage = "";
        }

        return;
    }

    private async Task<bool> EnsureUserAccountStatusAndGetInvitedUserPublicKey()
    {
        try
        {
            EmailAddress invitedEmail = EmailAddress.Parse(InvitedUser);
            AccountStatus accountStatus = await invitedEmail.GetValidEmailAccountStatusAsync(New<KnownIdentities>().DefaultEncryptionIdentity);

            IEnumerable<EmailAddress> invitedEmails = new EmailAddress[] { invitedEmail };
            IEnumerable<UserPublicKey> inviteKey = await invitedEmails.ToAvailableKnownPublicKeysAsync(New<KnownIdentities>().DefaultEncryptionIdentity);
            if (inviteKey != null && inviteKey.Any())
            {
                return true;
            }
        }

        catch (Exception ex)
        {
            ErrorMessage = $"{ex.Message}";
            return false;
        }

        return true;
    }
}

