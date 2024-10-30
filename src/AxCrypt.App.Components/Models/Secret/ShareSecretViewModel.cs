using AxCrypt.Api.Model;
using AxCrypt.Abstractions;
using AxCrypt.Content;
using AxCrypt.Core.Secrets;
using AxCrypt.Core.UI;
using System.Collections.ObjectModel;
using AxCrypt.Core.Crypto;
using AxCrypt.Cryptor.Model;
using AxCrypt.Core.Runtime;
using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.App.Components.Utility;
using AxCrypt.App.Components.Password;
using AxCrypt.App.Components.Helpers;

namespace AxCrypt.App.Components.Models.Secret
{
    public class ShareSecretViewModel : ManageSecretViewModel
    {
        private LogOnIdentity _identity;
        private readonly SecretService _secretService;
        private SecretViewModel _secret;

        public SecretViewModel Secret
        {
            get { return GetProperty<SecretViewModel>(nameof(Secret)); }
            private set { SetProperty(nameof(Secret), value); }
        }

        public ShareSecretViewModel(SecretService secretService)
        {
            _secretService = secretService;
            InitializeSecret();
            _identity = New<KnownIdentities>().DefaultEncryptionIdentity;

            SetNewContactState();
            bool hasEncryptionCapability = New<AccountStatusViewModel>().PlanState == PlanState.HasPasswordManager || New<AccountStatusViewModel>().PlanState == PlanState.HasPremium || New<AccountStatusViewModel>().PlanState == PlanState.HasBusiness;

            SharedSecretTitle = _secretService.CurrentSecret.SecretTitle;
            ShareSecretUserList = new ObservableCollection<SecretSharedUserViewModel>(_secretService.CurrentSecret.SharedWith.Select(user => new SecretSharedUserViewModel(user.UserEmail, user.Visibility, user.OwnerEmail, AccountStatus.Verified)));

            CanEnableAddShareSecret = true;
            EnableApplyButton = false;
            AddedUsersTitle = $"Added users with access ({ShareSecretUserList.Count})";

            VisibilityType = SecretShareVisibility.Forever.ToString();
            PageTitle = Texts.ShareAccessTitle;
            VisibilityTypeList = ViewModelHelper.GetVisibilityTypeList();
        }

        public void InitializeSecret()
        {
            _secret = _secretService.GetCurrentSecret();
        }

        public ObservableCollection<SecretSharedUserViewModel> ShareSecretUserList { get; set; } = new ObservableCollection<SecretSharedUserViewModel>();

        public string VisibilityType
        { get { return GetProperty<string>(nameof(VisibilityType)); } set { SetProperty(nameof(VisibilityType), value); } }

        public IList<string> VisibilityTypeList
        {
            get { return GetProperty<IList<string>>(nameof(VisibilityTypeList)); }
            set { SetProperty(nameof(VisibilityTypeList), value); }
        }

        public string SecretSharingUserEmail
        {
            get { return GetProperty<string>(nameof(SecretSharingUserEmail)); }
            set
            {
                SetProperty(nameof(SecretSharingUserEmail), value);
                ClearErrorProviders();
            }
        }

        public string AddedUsersTitle { get; set; }

        public bool IsAnyUsersAdded { get; set; }

        public bool NoUsersAdded { get; set; }

        public bool CanEnableAddShareSecret { get; set; }

        public bool EnableApplyButton
        {
            get { return GetProperty<bool>(nameof(EnableApplyButton)); }
            set { SetProperty(nameof(EnableApplyButton), value); }
        }

        public EmailAddress UserEmailForContextMenuAction { get; set; }

        public bool CanEnableNewShareSecretUserEntry { get; set; }

        public bool CanContextMenuOpened { get; set; }

        public string SharedSecretTitle { get; set; }

        public async void AddUserToSharedListAsync()
        {
            if (!ViewModelHelper.IsAxCryptOnline())
            {
                ShowHideOfflineError(false);
                return;
            }

            EmailAddress addedUserEmailAddress = ValidSharingUserEmail();
            if (!ValidUserToShareSecret(addedUserEmailAddress))
            {
                return;
            }

            CanEnableAddShareSecret = false;
            await AddUserEmailToSharedList(addedUserEmailAddress);
            SecretSharingUserEmail = "";
        }

        private EmailAddress ValidSharingUserEmail()
        {
            if (string.IsNullOrWhiteSpace(SecretSharingUserEmail))
            {
                return EmailAddress.Empty;
            }

            EmailAddress addedUserEmailAddress;
            if (!EmailAddress.TryParse(SecretSharingUserEmail.Trim(), out addedUserEmailAddress))
            {
                ErrorMessage = Texts.BadEmail;
                return EmailAddress.Empty;
            }

            return addedUserEmailAddress;
        }

        private bool ValidUserToShareSecret(EmailAddress addedUserEmailAddress)
        {
            if (addedUserEmailAddress == EmailAddress.Empty)
            {
                return false;
            }

            if (addedUserEmailAddress.Address == New<UserSettings>().UserEmail)
            {
                return false;
            }

            if (_secretService.CurrentSecret.SharedWith.Any(user => user.UserEmail == addedUserEmailAddress))
            {
                ErrorMessage = "Email already exists!";
                return false;
            }

            int maxAllowedUsersCount = ViewModelHelper.MaxAllowedUsersCountToShare();
            int currentUserCount = _secretService.CurrentSecret.SharedWith.Count();

            if (currentUserCount >= maxAllowedUsersCount)
            {
                ErrorMessage = $"Cannot add more users. Maximum allowed is {maxAllowedUsersCount}.";
                return false;
            }

            return true;
        }

        private async Task AddUserEmailToSharedList(EmailAddress addedUserEmailAddress)
        {
            SecretShareVisibility parsedVisibility;
            if (!Enum.TryParse(VisibilityType, out parsedVisibility))
            {
                ErrorMessage = "Invalid visibility type selected!";
                return;
            }

            if (_secretService.CurrentSecret.SharedWith is List<SecretSharedUserViewModel> sharedWithList)
            {
                sharedWithList.Add(new SecretSharedUserViewModel(addedUserEmailAddress, parsedVisibility, _identity.UserEmail.Address));
            }
            else
            {
                List<SecretSharedUserViewModel> updatedList = _secretService.CurrentSecret.SharedWith.ToList();
                updatedList.Add(new SecretSharedUserViewModel(addedUserEmailAddress, parsedVisibility, _identity.UserEmail.Address));
                _secretService.CurrentSecret.SharedWith = updatedList;
            }

            UpdateUIElementsOnChange();
        }

        private void UpdateUIElementsOnChange()
        {
            EnableApplyButton = true;
            //CanEnableAddShareSecret = true;
            //IsAnyUsersAdded = ShareSecretUserList.Any();
            //NoUsersAdded = ShareSecretUserList.Count == 0;

            //AddedUsersTitle = $"Added users with access ({ShareSecretUserList.Count})";
        }

        public void UpdatedSelectedUser(SecretSharedUserViewModel user)
        {
            UserEmailForContextMenuAction = user.UserEmail;
            CanContextMenuOpened = !CanContextMenuOpened;
        }

        public void RemoveSelectedSharedUser()
        {
            RemoveSharedUserInternal(UserEmailForContextMenuAction);
            CanContextMenuOpened = !CanContextMenuOpened;
            UserEmailForContextMenuAction = EmailAddress.Empty;
        }

        /*private void RemoveSharedUserInternal(EmailAddress addedUserEmailAddress)
        {
            SecretSharedUserViewModel selectedSharedKeyUser = _secretService.CurrentSecret.SharedWith.Single(ss => ss.UserEmail == addedUserEmailAddress);
            if (selectedSharedKeyUser == null)
            {
                return;
            }

            _secretService.CurrentSecret.SharedWith.Remove(selectedSharedKeyUser);
            UpdateUIElementsOnChange();
        }*/

        private void RemoveSharedUserInternal(EmailAddress addedUserEmailAddress)
        {
            List<SecretSharedUserViewModel> sharedWithList = _secretService.CurrentSecret.SharedWith as List<SecretSharedUserViewModel>;

            if (sharedWithList == null)
            {
                sharedWithList = _secretService.CurrentSecret.SharedWith.ToList();
                _secretService.CurrentSecret.SharedWith = sharedWithList;
            }

            SecretSharedUserViewModel selectedSharedKeyUser = sharedWithList.SingleOrDefault(ss => ss.UserEmail == addedUserEmailAddress);
            if (selectedSharedKeyUser == null)
            {
                return;
            }

            sharedWithList.Remove(selectedSharedKeyUser);
            UpdateUIElementsOnChange();
        }

        public void RefreshSharedSecretUserInfo()
        {
            return;
        }

        public async Task<bool> ApplyShareSecret()
        {
            if (!EnableApplyButton)
            {
                return false;
            }

            if (!ViewModelHelper.IsAxCryptOnline())
            {
                ShowHideOfflineError(false);
                return false;
            }

            SecretClientModel theSecret = _secret.ToClientModel(_secret.SecretGuid);

            IEnumerable<AxCrypt.Core.Secrets.SecretSharedUser> secretSharedUsers = _secretService.CurrentSecret.SharedWith.Select(us => new SecretSharedUser(us.UserEmail, us.Visibility));
            if (!secretSharedUsers.Any())
            {
                new List<AxCrypt.Core.Secrets.SecretSharedUser>();
            }

            theSecret.Share = new ShareSecret(secretSharedUsers, _identity.UserEmail.Address, New<INow>().Utc);
            await PersonalSecrets.ShareAsync(theSecret);
            _secret.SharedWith = _secretService.CurrentSecret.SharedWith;
            _secretService.CurrentSecret = new SecretViewModel(theSecret);
            ShareSecretUserList.Clear();
            return true;
        }

        private void SetNewContactState()
        {
            ShowHideOfflineError(ViewModelHelper.IsAxCryptOnline());
        }

        private void ShowHideOfflineError(bool isOnline)
        {
            CanEnableNewShareSecretUserEntry = isOnline;
            if (!isOnline)
            {
                SecretSharingUserEmail = $"[{Texts.OfflineIndicatorText}]";
                ErrorMessage = Texts.KeySharingOffline;
                return;
            }

            SecretSharingUserEmail = "";
            ClearErrorProviders();
        }

        private void ClearErrorProviders()
        {
            ErrorMessage = "";
        }
    }
}