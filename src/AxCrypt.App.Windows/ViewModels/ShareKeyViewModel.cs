using AxCrypt.Abstractions;
using AxCrypt.Api;
using AxCrypt.Api.Model;
using AxCrypt.App.Components.Models;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using Microsoft.AspNetCore.Components;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.ViewModels;

public class ShareKeyViewModel : ViewModelBase
{
    public SubscriptionLevel SubscriptionLevel { get; set; }
    public bool IsWideScreen { get; set; }

    public bool showSuggestionDropdown = false;
    public List<string>? NewUsersList { get; set; } = new List<string>();
    public List<UserPublicKey>? _sharedKeyUsersList = new List<UserPublicKey>() { };
    public IEnumerable<ShareKeyFile>? ShareKeyFileList { get; set; }
    public IList<ShareKeyUser>? _shareKeyUserList { get; set; } = new List<ShareKeyUser>();

    private EmailAddress? UserEmailForContextMenuAction;

    private LogOnIdentity? _identity;
    private SharingListViewModel _viewModel;
    private FileOperationViewModel? _fileOperationViewModel;
    private IEnumerable<string>? _shareKeyFileNameList;

    public void InitializeViewModel()
    {
        SubscriptionLevel = New<AccountStatusViewModel>().SubscriptionLevel;
        _identity = New<KnownIdentities>().DefaultEncryptionIdentity;
    }

    public void SetSelectedFilesOrFolders(IEnumerable<string> filesOrFoldersPath, SharingListViewModel sharingListViewModel, bool isFolder = false)
    {
        IsFolder = isFolder;
        SelectedFilesOrFolders = filesOrFoldersPath;
        _viewModel = sharingListViewModel;
        _viewModel.BindPropertyChanged<IEnumerable<UserPublicKey>>(nameof(SharingListViewModel.SharedWith), (aks) =>
        {
            _shareKeyUserList = aks.Distinct(UserPublicKey.EmailComparer).ToArray().Select(user =>
            {
                if (user != null && !string.IsNullOrEmpty(user.GroupName))
                {
                    return new ShareKeyUser(user.Email, user.GroupName);
                }

                return new ShareKeyUser(user.Email, AccountStatus.Verified);
            }).ToList();
        });

        _shareKeyUserList = _viewModel.SharedWith.Select(user => new ShareKeyUser(user.Email, AccountStatus.Verified)).ToList();
    }

    public bool contextMenu { get; set; } = false;
    public bool showDialog { get; set; } = false;
    public bool SyncPopup { get; set; } = false;
    public bool WarngPopup { get; set; } = false;
    public bool isFirstClick { get; set; } = true;
    public bool isAxCryptUser { get; set; } = true;
    public string? ErrorMessage { get; set; }

    public void contextMenuPopup(string email)
    {
        if (!EmailAddress.TryParse(email, out UserEmailForContextMenuAction))
        {
            //show an error message
            return;
        }
        contextMenu = !contextMenu;
    }

    public void OpenModal()
    {
        showDialog = true;
    }

    public void CloseModal()
    {
        showDialog = false;
    }

    public void openSyncPopup()
    {
        SyncPopup = true;
    }

    public void closeSyncPopup()
    {
        SyncPopup = false;
    }

    public void showSyncPopup()
    {
        isFirstClick = false;
        if (isFirstClick)
        {
            openSyncPopup();
            isFirstClick = false;
        }
    }

    public void showWarngPopup()
    {
        WarngPopup = true;
    }

    public void closeWarngPopup()
    {
        WarngPopup = false;
    }

    [Parameter]
    public EventCallback OnClose { get; set; }

    public void Close()
    {
        if (OnClose.HasDelegate)
        {
            OnClose.InvokeAsync(null);
        }
    }

    private string? recipientEmail;
    public string RecipientEmail
    {
        get
        {
            return recipientEmail;
        }
        set
        {
            if (recipientEmail != value)
            {
                recipientEmail = value;
                UpdateNewKeyShareUser();
            }
        }
    }

    private void UpdateNewKeyShareUser()
    {
        _viewModel.NewKeyShare = RecipientEmail.Trim();
        ClearErrorProviders();
    }

    public void SuggestUnSharedUserEmailList()
    {
        _viewModel.NewKeyShare = RecipientEmail.Trim();

        IEnumerable<ShareKeyUser> filteredUnSharedUsersList = SuggestNotSharedWithByText(RecipientEmail);
        if (!filteredUnSharedUsersList.Any())
        {
            ClearErrorProviders();
            return;
        }

        foreach (ShareKeyUser user in filteredUnSharedUsersList)
        {
            EmailSuggestion emailSuggestion = new EmailSuggestion();
            emailSuggestion.Email = user.UserEmail;
            emailSuggestion.GroupName = user.GroupName;
            emailSuggestion.Type = user.Image;

            AllSuggestions.Add(emailSuggestion);
        }

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

        if (_shareKeyUserList.Any(user => user.UserEmail == addedUserEmailAddress.Address))
        {
            return;
        }

        if (New<AxCryptOnlineState>().IsOffline && !await AddShareKeyWhenOffline(addedUserEmailAddress))
        {
            return;
        }

        ShareKeyUser shareKeyUser = null;
        if (groupPublicKey == null)
        {
            AccountStatus accountStatus = AccountStatus.Verified;
            if (!New<AxCryptOnlineState>().IsOffline)
            {
                accountStatus = await ShareNewContactAsync();
            }

            shareKeyUser = new ShareKeyUser(EmailAddress.Parse(_viewModel.NewKeyShare), accountStatus);
        }
        else
        {
            _viewModel.NewKeyShare = groupPublicKey.Email.Address;
            await _viewModel.AddNewKeyShare.ExecuteAsync(_viewModel.NewKeyShare);

            string shareGroupText = RecipientEmail.Trim();
            shareKeyUser = new ShareKeyUser(groupPublicKey.Email, shareGroupText);
        }

        await AddEmailToKeyShareListAsync(addedUserEmailAddress);
        RecipientEmail = string.Empty;
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

    public static bool IsConnected()
    {
        NetworkAccess current = Connectivity.Current.NetworkAccess;
        return current == NetworkAccess.Internet;
    }

    private async Task AddEmailToKeyShareListAsync(EmailAddress addedUserEmailAddress)
    {
        using (await New<IProgressDialog>().Show(Texts.ProgressIndicatorWaitMessage, Texts.ProgressIndicatorWaitMessage))
        {
            await ShareSelectedIndices(new List<string> { addedUserEmailAddress.Address });

            AccountStatus accountStatus = await ShareNewContactAsync();
            if (accountStatus != AccountStatus.Unknown)
            {
                RecipientEmail = string.Empty;
            }

            AddKeySharingUserList(addedUserEmailAddress, accountStatus);
        }
    }

    private Task ShareSelectedIndices(IEnumerable<string> newKeyShareUserList)
    {
        return _viewModel.AddKeyShares.ExecuteAsync(newKeyShareUserList.Select(user => EmailAddress.Parse(user)));
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

    private void AddKeySharingUserList(EmailAddress addedUserEmailAddress, AccountStatus accountStatus)
    {
        _shareKeyUserList.Add(new ShareKeyUser(addedUserEmailAddress, accountStatus));
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
        if (_viewModel[nameof(SharingListViewModel.NewKeyShare)].Length > 0)
        {
            ErrorMessage = Texts.InvalidEmail;
            return false;
        }

        return true;
    }

    public bool IsFolder { get; set; }

    public async Task ApplyShareKeys()
    {
        try
        {
            switch (IsFolder)
            {
                case true:
                    using (await New<IProgressDialog>().Show(Texts.ProgressIndicatorWaitMessage, Texts.ProgressIndicatorWaitMessage))
                    {
                        await _viewModel.ShareFolders.ExecuteAsync(_viewModel.SharedWith);
                    }
                    break;

                case false:
                    using (await New<IProgressDialog>().Show(Texts.ProgressIndicatorWaitMessage, Texts.ProgressIndicatorWaitMessage))
                    {
                        await _viewModel.ShareFiles.ExecuteAsync(_viewModel.SharedWith);
                    }
                    break;
            }

            SelectedFilesOrFolders = Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task RemoveSharedKey()
    {
        await _viewModel.RemoveKeyShares.ExecuteAsync(new UserPublicKey[] { (UserPublicKey)_viewModel.SharedWith.First(su => su.Email == UserEmailForContextMenuAction) });

        RemoveKeySharingUserList(UserEmailForContextMenuAction);
        contextMenu = !contextMenu;
        UserEmailForContextMenuAction = EmailAddress.Empty;
    }

    private void RemoveKeySharingUserList(EmailAddress addedUserEmailAddress)
    {
        ShareKeyUser selectedSharedKeyUser = _shareKeyUserList.Single(skul => skul.UserEmail == addedUserEmailAddress.Address);
        if (selectedSharedKeyUser == null)
        {
            return;
        }

        _shareKeyUserList.Remove(selectedSharedKeyUser);
        //UpdateUIElementsOnChange();
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

    public IEnumerable<string>? SelectedFilesOrFolders { get; set; }

    public class EmailSuggestion()
    {
        public string? Email { get; set; }
        public string? GroupName { get; set; }
        public string? Type { get; set; }
    }

    public List<EmailSuggestion> EmailSuggestions = new List<EmailSuggestion>();

    public List<EmailSuggestion> AllSuggestions = new List<EmailSuggestion>();

    public void OnEmailInput(ChangeEventArgs e)
    {
        RecipientEmail = e.Value?.ToString();

        if (!string.IsNullOrEmpty(RecipientEmail))
        {
            EmailSuggestions = AllSuggestions.Where(s => s.Email?.Contains(RecipientEmail, StringComparison.OrdinalIgnoreCase) == true ||
                            s.GroupName?.Contains(RecipientEmail, StringComparison.OrdinalIgnoreCase) == true).ToList();
            showSuggestionDropdown = EmailSuggestions.Any();

            SuggestUnSharedUserEmailList();
        }
        else
        {
            showSuggestionDropdown = false;
        }
    }

    public void SelectSuggestion(string suggestion)
    {
        RecipientEmail = suggestion;
        showSuggestionDropdown = false;
    }

    public List<string> SelectedFiles { get; set; } = new List<string>();

    public void GropuLink()
    {
        New<Abstractions.IBrowser>().OpenUri(new Uri("https://account.axcrypt.net/"));
    }

    public class ShareKeyFile
    {
        public string Name { get; set; }
    }
}
