using AxCrypt.Abstractions;
using AxCrypt.Api;
using AxCrypt.Api.Model;
using AxCrypt.App.Entitlement.Contracts;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System.Collections.ObjectModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.ViewModels;

public class ShareKeyViewModel : ViewModelBase
{
    public LogOnViewModel LogOnViewModel;
    private SharingListViewModel? _viewModel;
    public EmailAddress? UserEmailForContextMenuAction;
    private IFeatureUsageProvider? _userFeatureLimitUsage;

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

    public bool IsCloudFile { get; set; }

    public DialogResult PageResult
    { get { return GetProperty<DialogResult>(nameof(PageResult)); } set { SetProperty(nameof(PageResult), value); } }

    public ShareKeyViewModel()
    {
        LogOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
        SubscriptionLevel = LogOnViewModel!.SubscriptionLevel;
        SuggestedUnSharedUsers = new ObservableCollection<ShareKeyUser>();
    }

    public async Task SetSelectedFilesOrFolders(IEnumerable<string> filesOrFoldersPath, SharingListViewModel sharingListViewModel, bool isCloudFile = false)
    {
        _userFeatureLimitUsage = AxCServiceProviderExtension.GetService<IFeatureUsageProvider>();

        EnableApplyButton = false;
        PageResult = DialogResult.None;
        SelectedFilesOrFolders = filesOrFoldersPath;
        IsCloudFile = isCloudFile;
        _viewModel = sharingListViewModel;
        _viewModel.BindPropertyChanged<IEnumerable<UserPublicKey>>(nameof(SharingListViewModel.SharedWith), (aks) =>
        {
            ShareKeyUserList = aks.Distinct(UserPublicKey.EmailComparer).ToArray().Select(user =>
            {
                if (user != null! && !string.IsNullOrEmpty(user.GroupName))
                {
                    return new ShareKeyUser(user.Email, user.GroupName);
                }

                return new ShareKeyUser(user!.Email, AccountStatus.Verified);
            }).ToList();
        });

        _viewModel.BindPropertyChanged<bool>(nameof(SharingListViewModel.IsOnline), (bool isOnline) => { SetNewContactState(); });
        IEnumerable<ShareKeyUser> filteredGroupList = _viewModel!.NotSharedWith.Distinct().ToArray().Select(user => new ShareKeyUser(user.Email, user.GroupName)).ToList();
        SuggestedUnSharedUsers = new ObservableCollection<ShareKeyUser>(filteredGroupList);

        LogOnViewModel.ShareKeyDialog.Show();

        while (PageResult == DialogResult.None)
        {
            await Task.Delay(1000);
        }

        LogOnViewModel.ShareKeyDialog.Close();
    }

    /// <summary>
    /// Drop a single file from the active selection. Wired to the × on each
    /// file chip in the ShareKey dialog so users can prune their selection
    /// without re-opening the file picker. If the last file is removed the
    /// dialog auto-closes — sharing nothing makes no sense.
    /// </summary>
    public void RemoveSelectedFile(string path)
    {
        if (SelectedFilesOrFolders == null || string.IsNullOrEmpty(path))
        {
            return;
        }

        SelectedFilesOrFolders = SelectedFilesOrFolders
            .Where(f => !string.Equals(f, path, StringComparison.OrdinalIgnoreCase))
            .ToList();
        _viewModel!.UpdateFiles(SelectedFilesOrFolders);

        if (!SelectedFilesOrFolders.Any())
        {
            PageResult = DialogResult.Cancel;
            LogOnViewModel.ShareKeyDialog.Close();
        }
    }

    public bool ContextMenu { get; set; } = false;

    public bool DisableAddUserButton { get; set; }
    public bool EnableApplyButton { get; set; }
    public bool SyncPopup { get; set; } = false;
    public bool WarngPopup { get; set; } = false;
    public bool IsFirstClick { get; set; } = true;
    public bool IsAxCryptUser { get; set; } = true;
    public string? ErrorMessage { get; set; }

    public string KeySharingUserEmail
    {
        get { return GetProperty<string>(nameof(KeySharingUserEmail)); }
        set
        {
            SetProperty(nameof(KeySharingUserEmail), value);
            UpdateNewKeyShare();
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
        ShowUnSharedUsersSuggestionPopup();

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

    public void HideUnSharedUsersSuggestionPopup()
    {
        ShowUserSuggestion = false;
        SuggestedUnSharedUsers = new ObservableCollection<ShareKeyUser>();
    }

    public void ShowUnSharedUsersSuggestionPopup()
    {
        ShowUserSuggestion = true;
        IEnumerable<ShareKeyUser> filteredUnSharedUsersList = _viewModel!.NotSharedWith.Distinct().ToArray().Select(user => new ShareKeyUser(user.Email, user.GroupName)).ToList();
        if (!filteredUnSharedUsersList.Any())
        {
            ShowUserSuggestion = false;
            return;
        }
        SuggestedUnSharedUsers = new ObservableCollection<ShareKeyUser>(filteredUnSharedUsersList);
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
            IEnumerable<ShareKeyUser> filteredGroupList = _viewModel!.NotSharedWith.Distinct().ToArray().Select(user => new ShareKeyUser(user.Email, user.GroupName)).ToList();
            SuggestedUnSharedUsers = new ObservableCollection<ShareKeyUser>(filteredGroupList);
            return;
        }

        IEnumerable<ShareKeyUser> filteredUnSharedUsersList = SuggestNotSharedWithByText(query);
        if (!filteredUnSharedUsersList.Any())
        {
            ShowUserSuggestion = false;
            return;
        }

        SuggestedUnSharedUsers = new ObservableCollection<ShareKeyUser>(filteredUnSharedUsersList);
    }

    private IEnumerable<ShareKeyUser> SuggestNotSharedWithByText(string suggestingText)
    {
        IEnumerable<UserPublicKey> filteredUserList = _viewModel!.NotSharedWith.Where(nsw => string.IsNullOrEmpty(nsw.GroupName) && nsw.Email.Address.Contains(suggestingText));
        List<ShareKeyUser> filteredUnSharedUsersList = filteredUserList.Distinct(UserPublicKey.EmailComparer).ToArray().Select(user => new ShareKeyUser(user.Email, AccountStatus.Verified)).ToList();

        IEnumerable<UserPublicKey> filteredGroupList = _viewModel.NotSharedWith.Where(nsw => !string.IsNullOrEmpty(nsw.GroupName) && nsw.GroupName.Contains(suggestingText));
        IEnumerable<ShareKeyUser> filteredUnSharedGroupsList = filteredGroupList.Distinct().ToArray().Select(user => new ShareKeyUser(user.Email, user.GroupName)).ToList();

        filteredUnSharedUsersList.AddRange(filteredUnSharedGroupsList);
        return filteredUnSharedUsersList;
    }

    public async Task AddShareKeyUser()
    {
        bool flowControl = AllowKeyShareWithRemainingLimits();
        if (!flowControl)
        {
            return;
        }

        if (DisableAddUserButton)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(KeySharingUserEmail) || KeySharingUserEmail == Texts.AddEmailPromptText)
        {
            return;
        }

        EmailAddress addedUserEmailAddress = ShareKeyUserEmailAddress();
        UserPublicKey groupPublicKey = ValidShareKeyUserGroup();
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

        if ((New<AxCryptOnlineState>().IsOffline) && !await AddShareKeyWhenOffline(addedUserEmailAddress))
        {
            return;
        }

        DisableAddUserButton = true;
        ShareKeyUser sharedUser = null!;
        if (groupPublicKey! == null!)
        {
            AccountStatus accountStatus = AccountStatus.Verified;
            if (!New<AxCryptOnlineState>().IsOffline)
            {
                accountStatus = await ShareNewContactAsync();
            }

            try
            {
                sharedUser = new ShareKeyUser(EmailAddress.Parse(_viewModel!.NewKeyShare), accountStatus);
            }
            catch (Exception ex)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.KeySharingOffline);
            }
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

    private bool AllowKeyShareWithRemainingLimits()
    {
        FeatureUsage keyShareUsage = _userFeatureLimitUsage!.GetUsage(FeatureKey.KeyShare);
        if (keyShareUsage.IsExhausted)
        {
            New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, "You have already exceeded the sharing limit. To key share more files, please upgrade your plan.", DoNotShowAgainOptions.None);
            SetNewContactState();
            return false;
        }

        int keyShareUserCount = ShareKeyUserList!.Count;
        ShareKeyUserList = ShareKeyUserList!.Take(keyShareUsage.Remaining).ToList();
        if (keyShareUserCount >= keyShareUsage.Remaining)
        {
            New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, $"You can only share with {keyShareUsage.Remaining} more user(s). If you want to share with additional users, please upgrade your plan.", DoNotShowAgainOptions.None);
            return false;
        }

        return true;
    }

    private EmailAddress ShareKeyUserEmailAddress()
    {
        if (EmailAddress.TryParse(KeySharingUserEmail.Trim(), out EmailAddress addedUserEmailAddress))
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
        await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.KeySharingOffline);
    }

    private void SetNewContactState()
    {
        int availableCount = _userFeatureLimitUsage!.GetUsage(FeatureKey.KeyShare).Remaining;

        if (!New<LicensePolicy>().Capabilities.Has(LicenseCapability.KeySharing) && availableCount == 0)
        {
            KeySharingUserEmail = $"[{Texts.PremiumFeatureToolTipText}]";
            return;
        }

        //KeySharingUserEmail = Texts.AddEmailPromptText;
    }

    private async Task<AccountStatus> ShareNewContactAsync()
    {
        if (string.IsNullOrEmpty(_viewModel!.NewKeyShare))
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
                KeySharingUserEmail = string.Empty;
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
        if (_viewModel![nameof(AxCrypt.Core.UI.ViewModel.SharingListViewModel.NewKeyShare)].Length > 0)
        {
            ErrorMessage = Texts.InvalidEmail;
            return false;
        }

        return true;
    }

    private void UpdateNewKeyShare()
    {
        _viewModel!.NewKeyShare = KeySharingUserEmail.Trim();
        ClearErrorProviders();
    }

    public async Task ApplyShareKeys()
    {
        if (!EnableApplyButton && !_viewModel!.SharedWith.Any())
        {
            return;
        }

        if (EnableApplyButton && !string.IsNullOrEmpty(KeySharingUserEmail))
        {
            return;
        }

        PageResult = DialogResult.OK;
    }

    public async Task RemoveSharedKey()
    {
        UserPublicKey userToRemove = _viewModel!.SharedWith.First(su => su.Email == UserEmailForContextMenuAction!);
        ShareKeyUser selectedSharedKeyUser = ShareKeyUserList!.Single(skul => skul.UserEmail == UserEmailForContextMenuAction!.Address);

        if (userToRemove != null!)
        {
            await _viewModel.RemoveKeyShares.ExecuteAsync(new UserPublicKey[] { (UserPublicKey)userToRemove });
            ShareKeyUserList!.Remove(selectedSharedKeyUser);
        }

        EnableApplyButton = true;
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

    public void GoToLearmoreLink()
    {
        New<Abstractions.IBrowser>().OpenUri(new Uri("https://axcrypt.net/information/group/"));
    }

    public void GoToCreateGroupLink()
    {
        New<Abstractions.IBrowser>().OpenUri(new Uri("https://account.axcrypt.net/Group/Index"));
    }
}