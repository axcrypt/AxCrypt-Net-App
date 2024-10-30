using AxCrypt.Api.Model.Secret;

namespace AxCrypt.App.Components.Models.Secret
{
    public class ManageSecretViewModel : Core.UI.ViewModel.ViewModelBase
    {
        public SecretViewModel Initialize(SecretType type)
        {
            return new SecretViewModel(type, SecretPasswordViewModel.Empty, SecretCardViewModel.Empty, SecretNoteViewModel.Empty, new List<SecretSharedUserViewModel>());
        }

        private SecretViewModel _secret;

        public SecretViewModel Secret
        { get { return _secret; } }

        public void SetSecretsForShare(SecretViewModel secrets)
        {
            _secret = secrets;
        }

        public string ErrorMessage
        {
            get
            {
                return GetProperty<string>(nameof(ErrorMessage));
            }
            set
            {
                SetProperty(nameof(ErrorMessage), value);
                if (value != "")
                    CanShowErrorMessage = true;
                else
                    CanShowErrorMessage = false;
            }
        }

        public bool CanShowErrorMessage { get; set; }

        public bool HasPaidSubscription
        {
            get { return GetProperty<bool>(nameof(HasPaidSubscription)); }
            set { SetProperty(nameof(HasPaidSubscription), value); }
        }

        public string PageTitle { get; set; }
       
    }
}