using AxCrypt.Abstractions;
using AxCrypt.Api;
using AxCrypt.Api.Model;
using AxCrypt.App.Components.Models;
using AxCrypt.App.Components.Services;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using Microsoft.AspNetCore.Components;
using System.Collections.ObjectModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.ViewModels;

public class ShareKeyViewModel : LogOnViewModel
{
    private LogOnIdentity? _identity;
    public LogOnViewModel LogOnViewModel;
    private SharingListViewModel _viewModel;
    private EmailAddress? UserEmailForContextMenuAction;
    private FileOperationViewModel? _fileOperationViewModel;
    private IEnumerable<string>? _shareKeyFileNameList;

    public bool ShowSuggestionDropdown { get; set; }

    public SubscriptionLevel SubscriptionLevel { get; set; }
    public bool IsWideScreen { get; set; }

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

    public IEnumerable<string>? SelectedFilesOrFolders { get; set; }

    public ShareKeyViewModel(LogOnViewModel logOnViewModel, ProcessIndicatorService processIndicatorService) : base(processIndicatorService)
    {
        LogOnViewModel = logOnViewModel;
        SubscriptionLevel = logOnViewModel.SubscriptionLevel;
    }

    public void SetSelectedFilesOrFolders(IEnumerable<string> filesOrFoldersPath, SharingListViewModel sharingListViewModel, bool isFolder = false)
    {
        _isFolder = isFolder;
        SelectedFilesOrFolders = filesOrFoldersPath;
        _viewModel = sharingListViewModel;
        _viewModel.BindPropertyChanged<IEnumerable<UserPublicKey>>(nameof(SharingListViewModel.SharedWith), (aks) =>
        {
            ShareKeyUserList = aks.Distinct(UserPublicKey.EmailComparer).ToArray().Select(user =>
            {
                if (user != null && !string.IsNullOrEmpty(user.GroupName))
                {
                    return new ShareKeyUser(user.Email, user.GroupName);
                }

                return new ShareKeyUser(user.Email, AccountStatus.Verified);
            }).ToList();
        });

        _viewModel.BindPropertyChanged<bool>(nameof(SharingListViewModel.IsOnline), (bool isOnline) => { SetNewContactState(); });

        LogOnViewModel.ShareKeyDialog.Show();

        LogOnViewModel.UIStateChanged();
    }

    public bool ContextMenu { get; set; } = false;
    public bool ShowDialog { get; set; } = false;
    public bool SyncPopup { get; set; } = false;
    public bool WarngPopup { get; set; } = false;
    public bool IsFirstClick { get; set; } = true;
    public bool IsAxCryptUser { get; set; } = true;
    public string? ErrorMessage { get; set; }

    public string RecipientEmail
    {
        get { return GetProperty<string>(nameof(RecipientEmail)); }
        set
        {
            SetProperty(nameof(RecipientEmail), value);
            UpdateNewKeyShareUser();
        }
    }

    public ObservableCollection<ShareKeyUser> SuggestedUnSharedUsers
    {
        get
        {
            return GetProperty<ObservableCollection<ShareKeyUser>>(nameof(SuggestedUnSharedUsers));
        }
        set
        {
            SetProperty(nameof(SuggestedUnSharedUsers), value);
        }
    }

    public void contextMenuPopup(string email)
    {
        if (!EmailAddress.TryParse(email, out UserEmailForContextMenuAction))
        {
            //show an error message
            return;
        }
        ContextMenu = !ContextMenu;
    }

    public void OpenSyncPopup()
    {
        SyncPopup = true;
    }

    public void CloseSyncPopup()
    {
        SyncPopup = false;
    }

    public void ShowSyncPopup()
    {
        IsFirstClick = false;
        if (IsFirstClick)
        {
            OpenSyncPopup();
            IsFirstClick = false;
        }
    }

    public void closeWarngPopup()
    {
        WarngPopup = false;
    }

    private void UpdateNewKeyShareUser()
    {
        _viewModel.NewKeyShare = RecipientEmail.Trim();
        ClearErrorProviders();
    }

    private IEnumerable<ShareKeyUser> SuggestNotSharedWithByText(string suggestingText)
    {
        IEnumerable<UserPublicKey> filteredUserList = _viewModel.NotSharedWith.Where(nsw => string.IsNullOrEmpty(nsw.GroupName) && nsw.Email.Address.Contains(suggestingText));
        List<ShareKeyUser> filteredUnSharedUsersList = filteredUserList.Distinct(UserPublicKey.EmailComparer).ToArray().Select(user => new ShareKeyUser(user.Email, AccountStatus.Verified)).ToList();

        IEnumerable<UserPublicKey> filteredGroupList = _viewModel.NotSharedWith.Where(nsw => !string.IsNullOrEmpty(nsw.GroupName) && nsw.GroupName.Contains(suggestingText));
        IEnumerable<ShareKeyUser> filteredUnSharedGroupsList = filteredGroupList.Distinct().ToArray().Select(user => new ShareKeyUser(user.Email, user.GroupName)).ToList();

        filteredUnSharedUsersList.AddRange(filteredUnSharedGroupsList);
        return filteredUnSharedUsersList;
    }

    public async void AddShareKeyUser()
    {
        if (string.IsNullOrWhiteSpace(RecipientEmail) || RecipientEmail == Texts.AddEmailPromptText)
        {
            return;
        }

        EmailAddress addedUserEmailAddress = ShareKeyUserEmailAddress();
        UserPublicKey groupPublicKey = ValidShareKeyUserGroup();
        if (addedUserEmailAddress == EmailAddress.Empty && groupPublicKey == null)
        {
            ErrorMessage = Texts.InvalidEmail;
            return;
        }

        if (groupPublicKey != null)
        {
            addedUserEmailAddress = groupPublicKey.Email;
        }

        if (ShareKeyUserList.Any(user => user.UserEmail == addedUserEmailAddress.Address))
        {
            return;
        }

        if (New<AxCryptOnlineState>().IsOffline && !await AddShareKeyWhenOffline(addedUserEmailAddress))
        {
            return;
        }
        ShareKeyUser sharedUser = null;
        if (groupPublicKey == null)
        {
            AccountStatus accountStatus = AccountStatus.Verified;
            if (!New<AxCryptOnlineState>().IsOffline)
            {
                accountStatus = await ShareNewContactAsync();
            }

            sharedUser = new ShareKeyUser(EmailAddress.Parse(_viewModel.NewKeyShare), accountStatus);
        }
        else
        {
            _viewModel.NewKeyShare = groupPublicKey.Email.Address;
            await _viewModel.AddNewKeyShare.ExecuteAsync(_viewModel.NewKeyShare);

            string shareGroupText = RecipientEmail.Trim();
            sharedUser = new ShareKeyUser(groupPublicKey.Email, shareGroupText);
        }

        RecipientEmail = string.Empty;
        LogOnViewModel.UIStateChanged();
    }

    private EmailAddress ShareKeyUserEmailAddress()
    {
        if (EmailAddress.TryParse(RecipientEmail.Trim(), out EmailAddress addedUserEmailAddress))
        {
            return addedUserEmailAddress;
        }

        return EmailAddress.Empty;
    }

    private UserPublicKey ValidShareKeyUserGroup()
    {
        string shareUserText = RecipientEmail.Trim();
        return _viewModel.GetValidGroupPublicKey(shareUserText);
    }

    private async Task<bool> AddShareKeyWhenOffline(EmailAddress userEmail)
    {
        if (!_viewModel.NotSharedWith.Any(ur => ur.Email == userEmail))
        {
            await DisplayOfflineWarningMessageAsync();
            return false;
        }

        await _viewModel.AddKeyShares.ExecuteAsync(new List<EmailAddress> { userEmail });
        return true;
    }

    private async Task DisplayOfflineWarningMessageAsync()
    {
        await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.KeySharingOffline);
    }

    private void SetNewContactState()
    {
        if (!New<LicensePolicy>().Capabilities.Has(LicenseCapability.KeySharing))
        {
            RecipientEmail = $"[{Texts.PremiumFeatureToolTipText}]";
            return;
        }

        //RecipientEmail = Texts.AddEmailPromptText;
    }

    public static bool IsConnected()
    {
        NetworkAccess current = Connectivity.Current.NetworkAccess;
        return current == NetworkAccess.Internet;
    }

    private async Task<AccountStatus> ShareNewContactAsync()
    {
        if (string.IsNullOrEmpty(_viewModel.NewKeyShare))
        {
            return AccountStatus.Unknown;
        }

        if (!AdHocValidationDueToMonoLimitations())
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
            await _viewModel.AddNewKeyShare.ExecuteAsync(_viewModel.NewKeyShare);
            if (_viewModel.SharedWith.Where(sw => sw.Email.ToString() == _viewModel.NewKeyShare).Any())
            {
                return accountStatus;
            }

            if (New<AxCryptOnlineState>().IsOffline)
            {
                ShowHideOfflineError();
                await DisplayOfflineWarningMessageAsync();
                RecipientEmail = string.Empty;
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
        await _viewModel.UpdateNewKeyShareStatus.ExecuteAsync(null);
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

    private bool AdHocValidationDueToMonoLimitations()
    {
        bool validated = AdHocValidateAllFieldsIndependently();
        return validated;
    }

    private bool AdHocValidateAllFieldsIndependently()
    {
        return AdHocValidateNewKeyShare();
    }

    private bool AdHocValidateNewKeyShare()
    {
        if (_viewModel[nameof(Core.UI.ViewModel.SharingListViewModel.NewKeyShare)].Length > 0)
        {
            ErrorMessage = Texts.InvalidEmail;
            return false;
        }

        return true;
    }

    private bool _isFolder;

    public async Task ApplyShareKeys()
    {
        switch (_isFolder)
        {
            case true:

                await _viewModel.ShareFolders.ExecuteAsync(_viewModel.SharedWith);
                break;

            case false:
                await _viewModel.ShareFiles.ExecuteAsync(_viewModel.SharedWith);
                break;
        }

        SelectedFilesOrFolders = Enumerable.Empty<string>();
        _isFolder = false;
        LogOnViewModel.ShareKeyDialog.Close();
        LogOnViewModel.UIStateChanged();
    }

    public async Task RemoveSharedKey()
    {
        UserPublicKey userToRemove = _viewModel.SharedWith.First(su => su.Email == UserEmailForContextMenuAction);
        ShareKeyUser selectedSharedKeyUser = ShareKeyUserList.Single(skul => skul.UserEmail == UserEmailForContextMenuAction.Address);

        if (userToRemove != null)
        {
            await _viewModel.RemoveKeyShares.ExecuteAsync(new UserPublicKey[] { (UserPublicKey)userToRemove });
            ShareKeyUserList.Remove(selectedSharedKeyUser);
        }

        CloseContextMenu();
    }

    private void CloseContextMenu()
    {
        ContextMenu = false;
        UserEmailForContextMenuAction = EmailAddress.Empty;
        return;
    }

    public async Task RefreshShare()
    {
        if (UserEmailForContextMenuAction == EmailAddress.Empty)
        {
            return;
        }

        bool isGroup = _viewModel.GetValidGroupPublicKey("", new List<EmailAddress>() { UserEmailForContextMenuAction }) != null;
        if (isGroup && !New<LicensePolicy>().Capabilities.Has(LicenseCapability.Business))
        {
            CloseContextMenu();
            return;
        }

        await _viewModel.RefreshKnownContact.ExecuteAsync(new List<EmailAddress>() { UserEmailForContextMenuAction });
    }

    private void ShowHideOfflineError()
    {
        if (!IsConnected())
        {
            RecipientEmail = $"[{Texts.OfflineIndicatorText}]";
            //ErrorMessage = Texts.KeySharingOffline;
            return;
        }

        RecipientEmail = "";
        ClearErrorProviders();
    }

    private void ClearErrorProviders()
    {
        //ErrorMessage = "";
    }

    public List<EmailSuggestion> EmailSuggestions { get; set; }

    public List<EmailSuggestion> AllSuggestions { get; set; }

    public void OnEmailInput(ChangeEventArgs e)
    {
        RecipientEmail = e.Value?.ToString();

        if (!string.IsNullOrEmpty(RecipientEmail))
        {
            UpdateEmailSuggestions();
        }
        else
        {
            ClearEmailSuggestions();
        }
    }

    private void UpdateEmailSuggestions()
    {
        AllSuggestions ??= new List<EmailSuggestion>();

        IEnumerable<ShareKeyUser> filteredUnSharedUsersList = SuggestNotSharedWithByText(RecipientEmail);
        if (filteredUnSharedUsersList.Any())
        {
            foreach (ShareKeyUser user in filteredUnSharedUsersList)
            {
                if (!AllSuggestions.Any(s => s.Email == user.UserEmail))
                {
                    AllSuggestions.Add(new EmailSuggestion { Email = user.UserEmail, GroupName = user.GroupName, Type = user.Image });
                }
            }
        }

        EmailSuggestions = AllSuggestions.Where(s => s.Email?.Contains(RecipientEmail, StringComparison.OrdinalIgnoreCase) == true || s.GroupName?.Contains(RecipientEmail, StringComparison.OrdinalIgnoreCase) == true).ToList();

        ShowSuggestionDropdown = EmailSuggestions.Any();

        //ClearErrorProviders();
    }

    private void ClearEmailSuggestions()
    {
        ShowSuggestionDropdown = false;
        EmailSuggestions.Clear();
        ClearErrorProviders();
    }

    public void SelectSuggestion(string suggestion)
    {
        RecipientEmail = suggestion;
        ShowSuggestionDropdown = false;
    }

    public void GropuLink()
    {
        New<Abstractions.IBrowser>().OpenUri(new Uri("https://account.axcrypt.net/"));
    }

    public class EmailSuggestion()
    {
        public string? Email { get; set; }
        public string? GroupName { get; set; }
        public string? Type { get; set; }
    }

    public class ShareKeyFile
    {
        public string Name { get; set; }
    }
}
