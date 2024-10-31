using AxCrypt.Core.Extensions;
using AxCrypt.Api.Model;
using AxCrypt.Core.UI;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.ViewModels;

public class InviteViewModel
{
    public InviteViewModel()
    {
    }

    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? InvitedUser { get; set; }
    public bool ShowInvitePopup { get; private set; } = true;

    public void OnInputFocus()
    {
        ErrorMessage = string.Empty;
    }

    public async Task InviteFriend()
    {
        if (string.IsNullOrEmpty(InvitedUser) || InvitedUser != "")
        {
            ErrorMessage = "Please enter a valid email.";
            return;
        }

        ErrorMessage = string.Empty;

        await EnsureUserAccountStatusAndGetInvitedUserPublicKey();
        ShowInvitePopup = false;
        IsSuccess = true;
        return;
    }

    private async Task<IEnumerable<Core.Crypto.Asymmetric.UserPublicKey>> EnsureUserAccountStatusAndGetInvitedUserPublicKey()
    {
        EmailAddress invitedEmail = EmailAddress.Parse(InvitedUser);
        AccountStatus accountStatus = await invitedEmail.GetValidEmailAccountStatusAsync(New<KnownIdentities>().DefaultEncryptionIdentity);

        IEnumerable<EmailAddress> invitedEmails = new EmailAddress[] { invitedEmail };
        return await invitedEmails.ToAvailableKnownPublicKeysAsync(New<KnownIdentities>().DefaultEncryptionIdentity);
    }

    public void CloseSuccessPopup()
    {
        IsSuccess = false;
        InvitedUser = string.Empty;
        ShowInvitePopup = true;
    }
}

