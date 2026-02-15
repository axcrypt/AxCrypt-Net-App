using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AxCrypt.Abstractions;
using AxCrypt.Api;
using AxCrypt.Api.Model;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.UI.ViewModels;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Desktop.Services;

namespace AxCrypt.App.Shared.Desktop.ViewModels
{
    public class ShareKeysViewModel : ViewModelBase
    {
        public LogOnViewModel LogOnViewModel { get; set; }
        private SharingListViewModel? _viewModel;
        private CloudFileOperationViewModel? _fileOperationViewModel;

        private List<UserPublicKey>? _sharedKeyUsersList = new List<UserPublicKey>() { };
        private IEnumerable<FilePickerItemViewModel>? _keySharingFileItemList;

        private ICustomNavigationService _navigationService;

        public ShareKeysViewModel()
        {
            LogOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
            bool hasEncryptionCapability = LogOnViewModel.License.Has(LicenseCapability.Premium);
        }

        public void InitializeValuesForShareKey(
            IEnumerable<string> shareKeyFileNameList,
            SharingListViewModel viewModel,
            IEnumerable<FilePickerItemViewModel> keySharingFileItemList,
            CloudFileOperationViewModel fileOperationViewModel,
            ICustomNavigationService navigationService
        )
        {
            _viewModel = viewModel;
            SelectedFilesOrFolders = shareKeyFileNameList;
            _keySharingFileItemList = keySharingFileItemList;
            _fileOperationViewModel = fileOperationViewModel;
            _navigationService = navigationService;

            _viewModel.BindPropertyChanged<IEnumerable<UserPublicKey>>(
                nameof(SharingListViewModel.SharedWith),
                (aks) =>
                {
                    ShareKeyUserList = aks.Distinct(UserPublicKey.EmailComparer)
                        .ToArray()
                        .Select(user =>
                        {
                            if (user != null! && !string.IsNullOrEmpty(user.GroupName))
                            {
                                return new ShareKeyUser(user.Email, user.GroupName);
                            }

                            return new ShareKeyUser(user!.Email, AccountStatus.Verified);
                        })
                        .ToList();
                }
            );

            _viewModel.BindPropertyChanged<bool>(
                nameof(SharingListViewModel.IsOnline),
                (bool isOnline) =>
                {
                    SetNewContactState();
                }
            );

            // ShareKeyUserList = new ObservableCollection<ShareKeyUser>(
            //     _sharedKeyUsersList!.Select(user => new ShareKeyUser(
            //         user.Email,
            //         AccountStatus.Verified
            //     ))
            // );
        }

        public IEnumerable<string>? SelectedFilesOrFolders { get; set; }

        public bool DisableAddUserButton { get; set; }

        public bool SyncPopup { get; set; } = false;

        public bool IsAxCryptUser { get; set; } = true;

        public bool IsWideScreen { get; set; }

        public bool WarngPopup { get; set; } = false;

        private IList<ShareKeyUser>? _shareKeyUserList;

        public IList<ShareKeyUser>? ShareKeyUserList
        {
            get => _shareKeyUserList;
            set
            {
                _shareKeyUserList = value;
                LogOnViewModel.UIStateChanged();
            }
        }

        public bool ShowUserSuggestion
        {
            get { return GetProperty<bool>(nameof(ShowUserSuggestion)); }
            set { SetProperty(nameof(ShowUserSuggestion), value); }
        }

        public ObservableCollection<ShareKeyUser> SuggestedUnSharedUsers
        {
            get
            {
                return GetProperty<ObservableCollection<ShareKeyUser>>(
                    nameof(SuggestedUnSharedUsers)
                );
            }
            set { SetProperty(nameof(SuggestedUnSharedUsers), value); }
        }

        public string KeySharingUserEmail
        {
            get { return GetProperty<string>(nameof(KeySharingUserEmail)); }
            set
            {
                SetProperty(nameof(KeySharingUserEmail), value);
                UpdateNewKeyShare();
            }
        }

        public bool EnableApplyButton
        {
            get { return GetProperty<bool>(nameof(EnableApplyButton)); }
            set { SetProperty(nameof(EnableApplyButton), value); }
        }

        public string ErrorMessage
        {
            get { return GetProperty<string>(nameof(ErrorMessage)); }
            set { SetProperty(nameof(ErrorMessage), value); }
        }

        private EmailAddress? UserEmailForContextMenuAction;

        public void contextMenuPopup(string email)
        {
            if (!EmailAddress.TryParse(email, out UserEmailForContextMenuAction))
            {
                //show an error message
                return;
            }
            ContextMenu = !ContextMenu;
        }

        public void closeWarngPopup()
        {
            WarngPopup = false;
        }

        public void OpenSyncPopup()
        {
            SyncPopup = true;
        }

        public void CloseSyncPopup()
        {
            SyncPopup = false;
        }

        public void HideUnSharedUsersSuggestionPopup()
        {
            ShowUserSuggestion = false;
            SuggestedUnSharedUsers = new ObservableCollection<ShareKeyUser>();
        }

        public void ShowUnSharedUsersSuggestionPopup()
        {
            ShowUserSuggestion = true;
            IEnumerable<ShareKeyUser> filteredUnSharedUsersList = _viewModel!
                .NotSharedWith.Distinct()
                .ToArray()
                .Select(user => new ShareKeyUser(user.Email, user.GroupName))
                .ToList();
            if (!filteredUnSharedUsersList.Any())
            {
                ShowUserSuggestion = false;
                return;
            }
            SuggestedUnSharedUsers = new ObservableCollection<ShareKeyUser>(
                filteredUnSharedUsersList
            );
        }

        public void SelectSuggestion(string selectedGroup, string selectedEmail)
        {
            ShowUserSuggestion = false;
            if (!string.IsNullOrEmpty(selectedGroup))
            {
                KeySharingUserEmail = selectedGroup;
                return;
            }

            KeySharingUserEmail = selectedEmail;
        }

        public void PerformSearchSuggestions(string query)
        {
            ClearErrorProviders();
            SuggestedUnSharedUsers = new ObservableCollection<ShareKeyUser>();
            ShowUserSuggestion = true;
            if (string.IsNullOrWhiteSpace(query))
            {
                IEnumerable<ShareKeyUser> filteredGroupList = _viewModel!
                    .NotSharedWith.Distinct()
                    .ToArray()
                    .Select(user => new ShareKeyUser(user.Email, user.GroupName))
                    .ToList();
                SuggestedUnSharedUsers = new ObservableCollection<ShareKeyUser>(filteredGroupList);
                return;
            }

            IEnumerable<ShareKeyUser> filteredUnSharedUsersList = SuggestNotSharedWithByText(query);
            if (!filteredUnSharedUsersList.Any())
            {
                ShowUserSuggestion = false;
                return;
            }

            SuggestedUnSharedUsers = new ObservableCollection<ShareKeyUser>(
                filteredUnSharedUsersList
            );
        }

        private IEnumerable<ShareKeyUser> SuggestNotSharedWithByText(string suggestingText)
        {
            IEnumerable<UserPublicKey> filteredUserList = _viewModel!.NotSharedWith.Where(nsw =>
                string.IsNullOrEmpty(nsw.GroupName) && nsw.Email.Address.Contains(suggestingText)
            );
            List<ShareKeyUser> filteredUnSharedUsersList = filteredUserList
                .Distinct(UserPublicKey.EmailComparer)
                .ToArray()
                .Select(user => new ShareKeyUser(user.Email, AccountStatus.Verified))
                .ToList();

            IEnumerable<UserPublicKey> filteredGroupList = _viewModel.NotSharedWith.Where(nsw =>
                !string.IsNullOrEmpty(nsw.GroupName) && nsw.GroupName.Contains(suggestingText)
            );
            IEnumerable<ShareKeyUser> filteredUnSharedGroupsList = filteredGroupList
                .Distinct()
                .ToArray()
                .Select(user => new ShareKeyUser(user.Email, user.GroupName))
                .ToList();

            filteredUnSharedUsersList.AddRange(filteredUnSharedGroupsList);
            return filteredUnSharedUsersList;
        }

        public async Task ApplyShareKeys()
        {
            try
            {
                if (!EnableApplyButton)
                {
                    return;
                }

                await _fileOperationViewModel!.ShareKey(
                    _keySharingFileItemList!,
                    _viewModel!.SharedWith
                );

                _navigationService.NavigateTo("/");
            }
            catch (Exception ex)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, ex.Message);
                return;
            }
        }

        private void UpdateNewKeyShare()
        {
            _viewModel!.NewKeyShare = KeySharingUserEmail.Trim();
            ClearErrorProviders();
        }

        public async Task AddShareKeyUser()
        {
            if (DisableAddUserButton)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(KeySharingUserEmail) || KeySharingUserEmail == Texts.AddEmailPromptText)
            {
                return;
            }

            EmailAddress addedUserEmailAddress = ShareKeyUserEmailAddress();
            UserPublicKey? groupPublicKey = ValidShareKeyUserGroup();
            if (addedUserEmailAddress == EmailAddress.Empty && groupPublicKey == null!)
            {
                ErrorMessage = Texts.InvalidEmail;
                LogOnViewModel.UIStateChanged();
                return;
            }

            if (groupPublicKey != null!)
            {
                addedUserEmailAddress = groupPublicKey.Email;
            }

            if (ShareKeyUserList!.Any(user => user.UserEmail == addedUserEmailAddress.Address))
            {
                return;
            }

            if (New<AxCryptOnlineState>().IsOffline && !await AddShareKeyWhenOffline(addedUserEmailAddress))
            {
                return;
            }

            DisableAddUserButton = true;
            await AddEmailToKeyShareListAsync(groupPublicKey!);
        }

        private async Task AddEmailToKeyShareListAsync(UserPublicKey groupPublicKey)
        {
            using (await New<IProgressDialog>().Show(Texts.ProgressIndicatorWaitMessage, Texts.ProgressIndicatorWaitMessage))
            {
                ShareKeyUser sharedUser = null!;
                if (groupPublicKey! == null!)
                {
                    AccountStatus accountStatus = AccountStatus.Verified;
                    if (!New<AxCryptOnlineState>().IsOffline)
                    {
                        accountStatus = await ShareNewContactAsync();
                    }

                    sharedUser = new ShareKeyUser(EmailAddress.Parse(_viewModel!.NewKeyShare), accountStatus);
                }
                else
                {
                    _viewModel!.NewKeyShare = groupPublicKey.Email.Address;
                    await _viewModel.AddNewKeyShare.ExecuteAsync(_viewModel.NewKeyShare);

                    string shareGroupText = KeySharingUserEmail.Trim();
                    sharedUser = new ShareKeyUser(groupPublicKey.Email, shareGroupText);
                }

                KeySharingUserEmail = string.Empty;
                DisableAddUserButton = false;
                EnableApplyButton = true;
                LogOnViewModel.UIStateChanged();
            }
        }

        private EmailAddress ShareKeyUserEmailAddress()
        {
            if (
                EmailAddress.TryParse(
                    KeySharingUserEmail.Trim(),
                    out EmailAddress addedUserEmailAddress
                )
            )
            {
                return addedUserEmailAddress;
            }

            return EmailAddress.Empty;
        }

        private UserPublicKey ValidShareKeyUserGroup()
        {
            if (LogOnViewModel.SubscriptionLevel != SubscriptionLevel.Business)
            {
                return null!;
            }

            string shareUserText = KeySharingUserEmail.Trim();
            return _viewModel!.GetValidGroupPublicKey(shareUserText);
        }

        private async Task<bool> AddShareKeyWhenOffline(EmailAddress userEmail)
        {
            if (!_viewModel!.NotSharedWith.Any(ur => ur.Email == userEmail))
            {
                await DisplayOfflineWarningMessageAsync();
                return false;
            }

            await _viewModel.AddKeyShares.ExecuteAsync(new List<EmailAddress> { userEmail });
            return true;
        }

        private async Task DisplayOfflineWarningMessageAsync()
        {
            await New<IPopup>()
                .ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.KeySharingOffline);
        }

        private void SetNewContactState()
        {
            if (!New<LicensePolicy>().Capabilities.Has(LicenseCapability.KeySharing))
            {
                KeySharingUserEmail = $"[{Texts.PremiumFeatureToolTipText}]";
                return;
            }

            //KeySharingUserEmail = Texts.AddEmailPromptText;
        }

        private async Task<AccountStatus> ShareNewContactAsync()
        {
            if (string.IsNullOrEmpty(KeySharingUserEmail))
            {
                return AccountStatus.Unknown;
            }

            AccountStatus accountStatus = await VerifyNewKeyShareStatus();
            switch (accountStatus)
            {
                case AccountStatus.Unverified:
                case AccountStatus.Verified:
                case AccountStatus.NotFound:
                    break;

                default:
                    return accountStatus;
            }

            try
            {
                await _viewModel!.AddNewKeyShare.ExecuteAsync(_viewModel.NewKeyShare);
                if (
                    _viewModel
                        .SharedWith.Where(sw => sw.Email.ToString() == _viewModel.NewKeyShare)
                        .Any()
                )
                {
                    return accountStatus;
                }

                if (New<AxCryptOnlineState>().IsOffline)
                {
                    ShowHideOfflineError();
                }
            }
            catch (BadRequestApiException braex)
            {
                New<IReport>().Exception(braex);
                ErrorMessage = Texts.InvalidEmail;
            }

            return AccountStatus.Unknown;
        }

        private async Task<AccountStatus> VerifyNewKeyShareStatus()
        {
            await _viewModel!.UpdateNewKeyShareStatus.ExecuteAsync(null!);
            AccountStatus sharedUserAccountStatus = _viewModel.NewKeyShareStatus;

            if (sharedUserAccountStatus == AccountStatus.Offline)
            {
                ShowHideOfflineError();
                return sharedUserAccountStatus;
            }

            if (sharedUserAccountStatus != AccountStatus.NotFound)
            {
                return sharedUserAccountStatus;
            }

            return sharedUserAccountStatus;
        }

        public async Task RemoveSharedKey()
        {
            UserPublicKey userToRemove = _viewModel!.SharedWith.First(su =>
                su.Email == UserEmailForContextMenuAction!
            );
            ShareKeyUser selectedSharedKeyUser = ShareKeyUserList!.Single(skul =>
                skul.UserEmail == UserEmailForContextMenuAction!.Address
            );

            if (userToRemove != null!)
            {
                await _viewModel.RemoveKeyShares.ExecuteAsync(
                    new UserPublicKey[] { (UserPublicKey)userToRemove }
                );
                ShareKeyUserList!.Remove(selectedSharedKeyUser);
            }

            EnableApplyButton = true;
            CloseContextMenu();
        }

        public bool ContextMenu { get; set; } = false;

        private void CloseContextMenu()
        {
            ContextMenu = false;
            UserEmailForContextMenuAction = EmailAddress.Empty;
            return;
        }

        public async Task RefreshShare()
        {
            try
            {
                if (UserEmailForContextMenuAction! == EmailAddress.Empty)
                {
                    return;
                }

                bool isGroup = _viewModel!.GetValidGroupPublicKey("", new List<EmailAddress>() { UserEmailForContextMenuAction! }) != null!;
                if (isGroup && !New<LicensePolicy>().Capabilities.Has(LicenseCapability.Business))
                {
                    CloseContextMenu();
                    return;
                }

                await _viewModel.RefreshKnownContact.ExecuteAsync(new List<EmailAddress>() { UserEmailForContextMenuAction! });
                EnableApplyButton = true;
            }
            catch (Exception ex)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, ex.Message);
                CloseContextMenu();
                EnableApplyButton = false;
            }
        }

        private void ShowHideOfflineError()
        {
            if (!New<IInternetState>().Connected)
            {
                KeySharingUserEmail = $"[{Texts.OfflineIndicatorText}]";
                ErrorMessage = Texts.KeySharingOffline;
                return;
            }

            KeySharingUserEmail = "";
            ClearErrorProviders();
        }

        private void ClearErrorProviders()
        {
            ErrorMessage = "";
        }
    }
}
