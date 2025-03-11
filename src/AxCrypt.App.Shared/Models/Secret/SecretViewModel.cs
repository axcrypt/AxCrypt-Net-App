using AxCrypt.Abstractions;
using AxCrypt.Api.Model.Groups;
using AxCrypt.Api.Model.Secret;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Models.Secret;

public class SecretViewModel : ViewModelBase
{
    private LogOnIdentity? _identity;

    public SecretViewModel(Cryptor.Model.SecretClientModel secret)
    {
        SecretGuid = secret.Id;
        DBId = secret.DBId;
        SecretType = secret.Type;
        CreatedUtc = secret.CreatedUtc;
        UpdatedUtc = secret.UpdatedUtc;
        SharedWith = secret.Share?.SharedWith?.Select(ss => new SecretSharedUserViewModel(ss.UserEmail, ss.VisibilityType, secret.Share.OwnerEmail)) ?? new List<SecretSharedUserViewModel>();
        _identity = New<KnownIdentities>().DefaultEncryptionIdentity;
        OwnerEmail = !string.IsNullOrEmpty(secret.Share?.OwnerEmail) ? secret.Share.OwnerEmail : _identity.UserEmail.Address;

        switch (secret.Type)
        {
            case SecretType.Legacy:
            case SecretType.Password:
                Initialize(secret.Password);
                break;

            case SecretType.Card:
                Initialize(secret.Card);
                break;

            case SecretType.Note:
                Initialize(secret.Note);
                break;

            default:
                break;
        }
    }

    //Inialize Empty Values
    public SecretViewModel(SecretType secretType, SecretPasswordViewModel password, SecretCardViewModel card, SecretNoteViewModel note, IEnumerable<SecretSharedUserViewModel> share)
    {
        SecretGuid = Guid.Empty;
        DBId = 0;
        SecretType = secretType;
        CreatedUtc = New<INow>().Utc;
        UpdatedUtc = New<INow>().Utc;
        Password = password;
        Card = card;
        Note = note;
        SharedWith = share;
    }

    private void Initialize(Core.Secrets.SecretPassword secretPwd)
    {
        Password = new SecretPasswordViewModel(secretPwd.Title, secretPwd.Url, secretPwd.Username, secretPwd.Description, secretPwd.TheSecret);
    }

    private void Initialize(Core.Secrets.SecretCard secretCard)
    {
        Card = new SecretCardViewModel(secretCard.Number, secretCard.Description, secretCard.NameOnCard, secretCard.SecurityCode, secretCard.ExpirationDate);
    }

    private void Initialize(Core.Secrets.SecretNote secretNote)
    {
        Note = new SecretNoteViewModel(secretNote.Description, secretNote.Note);
    }

    public Guid SecretGuid
    { get { return GetProperty<Guid>(nameof(SecretGuid)); } set { SetProperty(nameof(SecretGuid), value); } }

    public long DBId { get; private set; }

    public DateTime CreatedUtc { get; private set; }

    public DateTime UpdatedUtc { get; private set; }

    public SecretType SecretType
    { get { return GetProperty<SecretType>(nameof(SecretType)); } private set { SetProperty(nameof(SecretType), value); } }

    public SecretPasswordViewModel Password
    { get { return GetProperty<SecretPasswordViewModel>(nameof(Password)); } set { SetProperty(nameof(Password), value); } }

    public SecretCardViewModel Card
    { get { return GetProperty<SecretCardViewModel>(nameof(Card)); } set { SetProperty(nameof(Card), value); } }

    public SecretNoteViewModel Note
    { get { return GetProperty<SecretNoteViewModel>(nameof(Note)); } set { SetProperty(nameof(Note), value); } }

    public IEnumerable<SecretSharedUserViewModel> SharedWith
    { get { return GetProperty<IEnumerable<SecretSharedUserViewModel>>(nameof(SharedWith)); } set { SetProperty(nameof(SharedWith), value); } }

    public IEnumerable<UserPublicKey> NotSharedWith
    { get { return GetProperty<IEnumerable<UserPublicKey>>(nameof(NotSharedWith)); } private set { SetProperty(nameof(NotSharedWith), value.ToList()); } }

    public string OwnerEmail
    { get { return GetProperty<string>(nameof(OwnerEmail)); } set { SetProperty(nameof(OwnerEmail), value); } }

    public string? SecretTitle
    {
        get
        {
            switch (SecretType)
            {
                case SecretType.Legacy:
                case SecretType.Password:
                    return Password.SecretDesc;

                case SecretType.Card:
                    return Card.SecretDesc;

                case SecretType.Note:
                    return Note.SecretDesc;

                default:
                    throw new InvalidOperationException();
            }
        }
    }

    public void SetSharedAndNotSharedWith()
    {
        EmailAddress userEmail = _identity.ActiveEncryptionKeyPair.UserEmail;
        using (KnownPublicKeys knownPublicKeys = New<KnownPublicKeys>())
        {
            NotSharedWith = knownPublicKeys.PublicKeys.Where(upk => upk.Email != userEmail && upk.Email.Address != New<UserSettings>().LicenseAuthorityEmail && !SharedWith.Any(sw => upk.Email == sw.UserEmail)).OrderBy(e => e.Email.Address);
        }
    }

    public void LoadAvailableGroupPublicKeysAsync(LogOnIdentity identity)
    {
        try
        {
            IEnumerable<GroupKeyPairApiModel> groups = identity.UserGroupKeyPairs;
            foreach (GroupKeyPairApiModel group in groups)
            {
                if (string.IsNullOrEmpty(group.Public))
                {
                    continue;
                }
                IAsymmetricPublicKey groupPublicKey = New<IAsymmetricFactory>().CreatePublicKey(group.Public);
                using (KnownPublicKeys knownPublicKeys = New<KnownPublicKeys>())
                {
                    knownPublicKeys.AddOrReplace(new UserPublicKey(EmailAddress.Parse(group.User), groupPublicKey, group.GroupName));
                }
            }
        }
        catch
        {
            return;
        }
    }

    public UserPublicKey GetValidGroupPublicKey(string groupName, IEnumerable<EmailAddress> groupEmails = null)
    {
        UserPublicKey groupPublicKey = null;
        using (KnownPublicKeys knownPublicKeys = New<KnownPublicKeys>())
        {
            if (!string.IsNullOrEmpty(groupName))
            {
                groupPublicKey = knownPublicKeys.PublicKeys.FirstOrDefault(a => a.GroupName == groupName);
            }
            if (groupEmails != null && groupEmails.Any())
            {
                groupPublicKey = knownPublicKeys.PublicKeys.FirstOrDefault(a => groupEmails.Contains(a.Email));
            }
        }

        return groupPublicKey;
    }
}