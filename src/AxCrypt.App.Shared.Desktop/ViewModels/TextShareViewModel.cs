using AxCrypt.Abstractions;
using AxCrypt.Api.Model.TextEncryption;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.Content;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Cryptor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.ViewModels;

public enum TextShareExpireOption
{
    None,
    OneHour,
    OneDay,
    OneWeek,
    OneMonth,
    Forever
}

public class TextShareViewModel : ViewModelBase
{
    private TextEncryptionViewModel _textEncryptionViewModel;
    public TextShareViewModel(TextEncryptionViewModel textEncryptionViewModel)
    {
        _textEncryptionViewModel = textEncryptionViewModel;
    }

    public string UserInput { get; set; } = "";

    public Passphrase Passphrase { get; set; } = Passphrase.Empty;

    public IList<EmailAddress> ReceiverList { get; set; } = new List<EmailAddress>();

    public string ExpiresInOption { get; set; } = nameof(TextShareExpireOption.OneHour);

    public DateTime ExpiresIn { get; set; }

    public string ErrorMessage { get; set; } = "";

    public string PlainText { get; set; } = "";

    public Uri? SharedLink { get; set; } = null;

    public IEnumerable<KeyValuePair<string, string>> ExpiryOptionsList
    {
        get
        {
            return new List<KeyValuePair<string, string>>()
            {
                new(nameof(TextShareExpireOption.OneHour), "1 hour"),
                new(nameof(TextShareExpireOption.OneDay), "1 day"),
                new(nameof(TextShareExpireOption.OneWeek), "1 week"),
                new(nameof(TextShareExpireOption.OneMonth), "1 month"),
                new(nameof(TextShareExpireOption.Forever), Texts.ForeverText),
            };
        }
    }

    private bool _isVisible = false;
    public bool IsVisible
    {
        get
        {
            return _isVisible;
        }
        set
        {
            _isVisible = value;
            UpdateViewState();
        }
    }

    public void ShowDialog()
    {
        IsVisible = true;
    }

    public void HideDialog()
    {
        ReceiverList = new List<EmailAddress>();
        ErrorMessage = "";
        UserInput = "";
        IsVisible = false;
    }

    public string HoveredElement { get; set; } = string.Empty;
    public bool IsHovered { get; set; } = false;

    public void ShowPopup(string element)
    {
        IsHovered = true;
        HoveredElement = element;
    }

    public void HidePopup()
    {
        IsHovered = false;
        HoveredElement = string.Empty;
    }

    private static readonly int MaxShareEmailAllowedFree = 1;
    private static readonly int MaxShareEmailsAllowedPremium = 3;
    private static readonly int MaxShareEmailsAllowedBusiness = 5;

    public int MaxAllowedUsersCountToShare()
    {
        LicenseCapabilities Capability = New<LicensePolicy>().Capabilities;

        if (Capability.Has(LicenseCapability.Business))
        {
            return MaxShareEmailsAllowedBusiness;
        }

        if (Capability.Has(LicenseCapability.Premium))
        {
            return MaxShareEmailsAllowedPremium;
        }

        if (Capability.Has(LicenseCapability.ShareSecretFree))
        {
            return MaxShareEmailAllowedFree;
        }
        return 0;
    }

    public void SetExpiresIn()
    {
        if (ExpiresInOption == "")
        {
            return;
        }

        if (!Enum.TryParse(ExpiresInOption, out TextShareExpireOption expiresInOption))
        {
            return;
        }

        switch (expiresInOption)
        {
            case TextShareExpireOption.OneHour:
                ExpiresIn = New<INow>().Utc.AddHours(1);
                break;
            case TextShareExpireOption.OneDay:
                ExpiresIn = New<INow>().Utc.AddDays(1);
                break;
            case TextShareExpireOption.OneWeek:
                ExpiresIn = New<INow>().Utc.AddDays(7);
                break;
            case TextShareExpireOption.OneMonth:
                ExpiresIn = New<INow>().Utc.AddMonths(1);
                break;
            case TextShareExpireOption.Forever:
                ExpiresIn = DateTime.MaxValue;
                break;
            default:
                ErrorMessage = "Invalid expires in option selected!";
                break;
        }
    }

    public async Task ApplyAsync()
    {
        ErrorMessage = "";
        if (string.IsNullOrEmpty(PlainText))
        {
            ErrorMessage = "Text is required to share";
            return;
        }

        if (!ReceiverList.Any())
        {
            ErrorMessage = "Please add at least one receiver.";
            return;
        }

        int maxUsersCount = MaxAllowedUsersCountToShare();
        if (ReceiverList.Count > maxUsersCount)
        {
            ErrorMessage = $"You can't add more users than the maximum allowed({maxUsersCount}).";
            return;
        }

        IEnumerable<UserPublicKey> availablePublicKeys = null;
        if (ReceiverList != null && ReceiverList.Any())
        {
            using (ProcessIndicator indicator = new ProcessIndicator())
            {
                availablePublicKeys = await GetPublicKeysAsync(ReceiverList);
            }
        }

        await InternalApplyShareAsync(availablePublicKeys, Passphrase);
    }

    private async Task InternalApplyShareAsync(IEnumerable<UserPublicKey>? availablePublicKeys, Passphrase passphrase)
    {
        SetExpiresIn();
        LogOnIdentity logOnIdentity = New<KnownIdentities>().DefaultEncryptionIdentity;

        using (ProcessIndicator indicator = new ProcessIndicator())
        {
            string encryptedText = await TextCryptor.EncryptTextAsync(passphrase.EncryptionIdentity(), PlainText, availablePublicKeys);
            _textEncryptionViewModel.EncryptedText = encryptedText;

            TextEncryptionApiModel textEncryptionApiModel = new TextEncryptionApiModel()
            {
                EncryptedText = encryptedText,
                Recipients = ReceiverList.Select(su => su.Address),
                Owner = logOnIdentity.UserEmail.Address,
                VisibleUntil = ExpiresIn,
                CreatedUtc = New<INow>().Utc,
                UpdatedUtc = New<INow>().Utc,
            };


            Guid sharedSecretId = await TextShareApiHelper.ShareTextAsync(logOnIdentity, textEncryptionApiModel);
            if (sharedSecretId != Guid.Empty)
            {
                Uri baseApiUri = AxCrypt.Core.Resolve.UserSettings.RestApiBaseUrl;
                string accountWebDomain = baseApiUri.ToString().Replace(AxCrypt.Core.Resolve.UserSettings.RestApiBaseUrl.PathAndQuery, "/");
                SharedLink = new Uri($"{accountWebDomain}GlobalTextEncryption/Decrypt?email={logOnIdentity.UserEmail}&id={sharedSecretId}");
            }
        }
    }

    private async Task<IEnumerable<UserPublicKey>> GetPublicKeysAsync(IEnumerable<EmailAddress> emails)
    {
        IList<EmailAddress> notKnownUsers = new List<EmailAddress>();
        List<UserPublicKey> availablePublicKeys = new List<UserPublicKey>();

        using (KnownPublicKeys knownPublicKeys = New<KnownPublicKeys>())
        {
            foreach (EmailAddress email in emails)
            {
                UserPublicKey grouppublicKey = knownPublicKeys.PublicKeys.FirstOrDefault(pk => pk.Email == email && !string.IsNullOrEmpty(pk.GroupName));
                if (grouppublicKey != null)
                {
                    availablePublicKeys.Add(grouppublicKey);
                    continue;
                }

                notKnownUsers.Add(email);
            }

            IEnumerable<UserPublicKey>? keys = await TextShareApiHelper.GetAsync(knownPublicKeys, notKnownUsers);
            if (keys != null)
            {
                availablePublicKeys.AddRange(keys);
            }
        }
        return availablePublicKeys;
    }
}
