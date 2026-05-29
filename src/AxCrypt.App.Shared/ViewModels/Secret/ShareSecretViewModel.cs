using AxCrypt.Abstractions;
using AxCrypt.Api.Model;
using AxCrypt.App.Entitlement.Services;
using AxCrypt.App.Shared.Data;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Models.Secret;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.Content;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Secrets;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using Microsoft.AspNetCore.Components;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.ViewModels.Secret;

public class ShareSecretViewModel : ManageSecretViewModel
{
    private LogOnIdentity _identity;

    public ShareSecretViewModel(SecretService secretService) : base(secretService)
    {
        _identity = New<KnownIdentities>().DefaultEncryptionIdentity;

        SetNewContactState();

        SharedSecretTitle = Secret.SecretTitle!;

        CanEnableAddShareSecret = true;
        EnableApplyButton = false;

        VisibilityType = SecretShareVisibility.Forever.ToString();
        PageTitle = Texts.ShareAccessTitle;
        VisibilityTypeList = ViewModelHelper.GetVisibilityTypeList();

        Secret.LoadAvailableGroupPublicKeysAsync(_identity);
        Secret.SetSharedAndNotSharedWith();
        ShareSecretUserList = new ObservableCollection<SecretSharedUserViewModel>(Secret.SharedWith.Select(user => new SecretSharedUserViewModel(user.UserEmail, user.Visibility, user.OwnerEmail, user.GroupName, AccountStatus.Verified)));
        AddedUsersTitle = $"Added users with access ({ShareSecretUserList.Count})";
    }

    public ObservableCollection<SecretSharedUserViewModel> ShareSecretUserList
    {
        get
        {
            return GetProperty<ObservableCollection<SecretSharedUserViewModel>>(nameof(ShareSecretUserList));
        }
        set
        {
            SetProperty(nameof(ShareSecretUserList), value);
        }
    }

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

    public string AddedUsersTitle
    {
        get { return GetProperty<string>(nameof(AddedUsersTitle)); }
        set { SetProperty(nameof(AddedUsersTitle), value); }
    }

    public bool CanEnableAddShareSecret
    {
        get { return GetProperty<bool>(nameof(CanEnableAddShareSecret)); }
        set { SetProperty(nameof(CanEnableAddShareSecret), value); }
    }

    public bool EnableApplyButton
    {
        get { return GetProperty<bool>(nameof(EnableApplyButton)); }
        set { SetProperty(nameof(EnableApplyButton), value); }
    }

    public EmailAddress UserEmailForContextMenuAction
    {
        get { return GetProperty<EmailAddress>(nameof(UserEmailForContextMenuAction)); }
        set { SetProperty(nameof(UserEmailForContextMenuAction), value); }
    }

    public bool CanEnableNewShareSecretUserEntry
    {
        get { return GetProperty<bool>(nameof(CanEnableNewShareSecretUserEntry)); }
        set { SetProperty(nameof(CanEnableNewShareSecretUserEntry), value); }
    }

    public bool CanContextMenuOpened
    {
        get { return GetProperty<bool>(nameof(CanContextMenuOpened)); }
        set { SetProperty(nameof(CanContextMenuOpened), value); }
    }

    public string SharedSecretTitle
    {
        get { return GetProperty<string>(nameof(SharedSecretTitle)); }
        set { SetProperty(nameof(SharedSecretTitle), value); }
    }

    public async Task AddUserToSharedListAsync()
    {
        if (!ViewModelHelper.IsAxCryptOnline())
        {
            ShowHideOfflineError(false);
            return;
        }

        string shareGroupText = string.Empty;
        UserPublicKey groupPublicKey = ValidShareKeyUserGroup();
        EmailAddress addedUserEmailAddress = ValidSharingUserEmail();
        
        if (groupPublicKey != null!)
        {
            addedUserEmailAddress = groupPublicKey.Email;
            shareGroupText = SecretSharingUserEmail.Trim();
        }

        if (!await ValidUserToShareSecret(addedUserEmailAddress))
        {
            return;
        }

        if (VisibilityType == "None")
        {
            ErrorMessage = "Visibility option cannot be selected none.";
            return;
        }

        CanEnableAddShareSecret = false;
        AddUserEmailToSharedList(addedUserEmailAddress, shareGroupText);
        SecretSharingUserEmail = "";
        VisibilityType = SecretShareVisibility.Forever.ToString();
        EnableApplyButton = true;
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

    private async Task<bool> ValidUserToShareSecret(EmailAddress addedUserEmailAddress)
    {
        if (addedUserEmailAddress == EmailAddress.Empty)
        {
            return false;
        }

        if (addedUserEmailAddress.Address == New<UserSettings>().UserEmail)
        {
            return false;
        }

        if (ShareSecretUserList.Any(user => user.UserEmail == addedUserEmailAddress))
        {
            ErrorMessage = "Email already exists!";
            return false;
        }

        int maxAllowedUsersCount = await ViewModelHelper.MaxAllowedUsersCountToShare();
        if (ShareSecretUserList.Count >= maxAllowedUsersCount)
        {
            ErrorMessage = $"Cannot add more users. Maximum allowed is {maxAllowedUsersCount}.";
            return false;
        }

        return true;
    }

    private void AddUserEmailToSharedList(EmailAddress addedUserEmailAddress, string shareGroupText)
    {
        SecretShareVisibility parsedVisibility;
        if (!Enum.TryParse(VisibilityType, out parsedVisibility))
        {
            ErrorMessage = "Invalid visibility type selected!";
            return;
        }

        ShareSecretUserList.Add(new SecretSharedUserViewModel(addedUserEmailAddress, parsedVisibility, _identity.UserEmail.Address, shareGroupText));
        UpdateUIElementsOnChange();
    }

    private void UpdateUIElementsOnChange()
    {
        EnableApplyButton = true;
        CanEnableAddShareSecret = true; // re-enable Add after each successful addition
        //IsAnyUsersAdded = ShareSecretUserList.Any();
        //NoUsersAdded = ShareSecretUserList.Count == 0;

        //AddedUsersTitle = $"Added users with access ({ShareSecretUserList.Count})";
        UpdateViewState();
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

    private void RemoveSharedUserInternal(EmailAddress addedUserEmailAddress)
    {
        SecretSharedUserViewModel selectedSharedKeyUser = ShareSecretUserList.Single(ss => ss.UserEmail == addedUserEmailAddress);
        if (selectedSharedKeyUser == null!)
        {
            return;
        }

        ShareSecretUserList.Remove(selectedSharedKeyUser);
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

        if (!await New<UserEntitlementService>().UserHasCapability(LimitedCapability.ShareSecret, New<AccountStatusViewModel>().SubscriptionLevel))
        {
            return false;
        }

        using (ProcessIndicator processIndicator = new ProcessIndicator())
        {
            SecretClientModel theSecret = Secret.ToClientModel(Secret.SecretGuid);

            IEnumerable<AxCrypt.Core.Secrets.SecretSharedUser> secretSharedUsers = ShareSecretUserList.Select(us => new SecretSharedUser(us.UserEmail, us.Visibility));
            if (!secretSharedUsers.Any())
            {
                new List<AxCrypt.Core.Secrets.SecretSharedUser>();
            }

            theSecret.Share = new ShareSecret(secretSharedUsers, _identity.UserEmail.Address, New<INow>().Utc);
            Secret.SharedWith = ShareSecretUserList;
            return await PersonalSecrets.ShareAsync(theSecret);
        }
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

    #region Suggest popup for Secrets

    public ObservableCollection<SecretSharedUserViewModel> SuggestedUnSharedUsers
    {
        get
        {
            return GetProperty<ObservableCollection<SecretSharedUserViewModel>>(nameof(SuggestedUnSharedUsers));
        }
        set
        {
            SetProperty(nameof(SuggestedUnSharedUsers), value);
        }
    }

    public bool ShowUserSuggestion
    {
        get { return GetProperty<bool>(nameof(ShowUserSuggestion)); }
        set { SetProperty(nameof(ShowUserSuggestion), value); }
    }

    public void HideUnSharedUsersSuggestionPopup()
    {
        ShowUserSuggestion = false;
        SuggestedUnSharedUsers = new ObservableCollection<SecretSharedUserViewModel>();
    }

    public void ShowUnSharedUsersSuggestionPopup()
    {
        ShowUserSuggestion = true;
        IEnumerable<SecretSharedUserViewModel> filteredUnSharedUsersList = Secret!.NotSharedWith.Distinct().ToArray().Select(user => new SecretSharedUserViewModel(user.Email, SecretShareVisibility.None, Secret.OwnerEmail, user.GroupName)).ToList();
        if (!filteredUnSharedUsersList.Any())
        {
            ShowUserSuggestion = false;
            return;
        }
        SuggestedUnSharedUsers = new ObservableCollection<SecretSharedUserViewModel>(filteredUnSharedUsersList);
    }

    public void SelectSuggestion(string selectedGroup, string selectedEmail)
    {
        ShowUserSuggestion = false;
        if (!string.IsNullOrEmpty(selectedGroup))
        {
            SecretSharingUserEmail = selectedGroup;
            return;
        }

        SecretSharingUserEmail = selectedEmail;
    }

    public void PerformSearch(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        IEnumerable<SecretSharedUserViewModel> filteredUnSharedUsersList = SuggestNotSharedWithByText(query.ToLower());
        if (!filteredUnSharedUsersList.Any())
        {
            ShowUserSuggestion = false;
            ClearErrorProviders();
            return;
        }

        ShowUserSuggestion = true;
        SuggestedUnSharedUsers = new ObservableCollection<SecretSharedUserViewModel>(filteredUnSharedUsersList);
    }

    public void OnItemTapped(SecretSharedUserViewModel selectedItem)
    {
        SecretSharingUserEmail = selectedItem.DisplayText;
        ShowUserSuggestion = false;
    }

    public void OnEmailInput(ChangeEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Value?.ToString()!))
        {
            PerformSearch(e.Value?.ToString()!);
        }
        else
        {
            ClearEmailSuggestions();
        }
    }

    private void ClearEmailSuggestions()
    {
        ShowUserSuggestion = false;
        ClearErrorProviders();
    }

    private IEnumerable<SecretSharedUserViewModel> SuggestNotSharedWithByText(string suggestingText)
    {
        suggestingText = suggestingText.ToLower();
        IEnumerable<UserPublicKey> filteredUserList = Secret.NotSharedWith.Where(nsw => string.IsNullOrEmpty(nsw.GroupName) && nsw.Email.Address.Contains(suggestingText));
        List<SecretSharedUserViewModel> filteredUnSharedUsersList = filteredUserList.Distinct(UserPublicKey.EmailComparer).ToArray().Select(user => new SecretSharedUserViewModel(user.Email, SecretShareVisibility.None, _identity.UserEmail.Address, "", AccountStatus.Verified)).ToList();

        IEnumerable<UserPublicKey> filteredGroupList = Secret.NotSharedWith.Where(nsw => !string.IsNullOrEmpty(nsw.GroupName) && nsw.GroupName.ToLower().Contains(suggestingText));
        IEnumerable<SecretSharedUserViewModel> filteredUnSharedGroupsList = filteredGroupList.Distinct().ToArray().Select(user => new SecretSharedUserViewModel(user.Email, SecretShareVisibility.None, _identity.UserEmail.Address, user.GroupName)).ToList();

        filteredUnSharedUsersList.AddRange(filteredUnSharedGroupsList);
        return filteredUnSharedUsersList;
    }

    private UserPublicKey ValidShareKeyUserGroup()
    {
        string shareUserText = SecretSharingUserEmail.Trim();
        return Secret.GetValidGroupPublicKey(shareUserText);
    }

    private static void UpdateKnownKeys(IEnumerable<UserPublicKey> sharedWith)
    {
        using (KnownPublicKeys knownPublicKeys = New<KnownPublicKeys>())
        {
            IEnumerable<UserPublicKey> previouslyUnknown = sharedWith.Where(shared => !knownPublicKeys.PublicKeys.Any(known => known.Email == shared.Email));
            foreach (UserPublicKey newPublicKey in previouslyUnknown)
            {
                knownPublicKeys.AddOrReplace(newPublicKey);
            }
        }
    }
    #endregion
}