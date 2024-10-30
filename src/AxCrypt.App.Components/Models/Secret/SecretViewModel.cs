using AxCrypt.Abstractions;
using AxCrypt.Api.Model.Secret;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Components.Models.Secret
{
    public class SecretViewModel : ViewModelBase
    {
        private LogOnIdentity _identity;

        public SecretViewModel(AxCrypt.Cryptor.Model.SecretClientModel secret)
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

        private void Initialize(AxCrypt.Core.Secrets.SecretPassword secretPwd)
        {
            Password = new SecretPasswordViewModel(secretPwd.Title, secretPwd.Url, secretPwd.Username, secretPwd.Description, secretPwd.TheSecret);
        }

        private void Initialize(AxCrypt.Core.Secrets.SecretCard secretCard)
        {
            Card = new SecretCardViewModel(secretCard.Number, secretCard.Description, secretCard.NameOnCard, secretCard.SecurityCode, secretCard.ExpirationDate);
        }

        private void Initialize(AxCrypt.Core.Secrets.SecretNote secretNote)
        {
            Note = new SecretNoteViewModel(secretNote.Description, secretNote.Note);
        }

        public Guid SecretGuid { get; private set; }
        public long DBId { get; private set; }
        public DateTime CreatedUtc { get; private set; }
        public DateTime UpdatedUtc { get; private set; }
        public SecretType SecretType { get; set; }
        public SecretsFilter SecretsFilterType { get; set; }
        public SecretPasswordViewModel Password { get; set; }
        public SecretCardViewModel Card { get; set; }
        public SecretNoteViewModel Note { get; set; }
        public IEnumerable<SecretSharedUserViewModel> SharedWith { get; set; }
        public string OwnerEmail { get; set; }

        public string SecretTitle
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
    }
}