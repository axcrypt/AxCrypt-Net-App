using AxCrypt.Abstractions;
using AxCrypt.Api;
using AxCrypt.Api.Model;
using AxCrypt.App.Components.Models;
using AxCrypt.App.Components.Services;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.UI;
using Microsoft.AspNetCore.Components;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.ViewModels;

public class ShareKeyViewModel : ComponentBase
{
    [Inject]
    FileShareService FileShareService { get; set; }

    public SubscriptionLevel SubscriptionLevel { get; set; }
    public bool IsWideScreen { get; set; }

    public bool showSuggestionDropdown = false;
    public List<string> NewUsersList { get; set; } = new List<string>();
    public List<UserPublicKey> _sharedKeyUsersList = new List<UserPublicKey>() { };
    public IEnumerable<ShareKeyFile>? ShareKeyFileList { get; set; }
    public IList<ShareKeyUser> ShareKeyUserList { get; set; } = new List<ShareKeyUser>();

    public EmailAddress? UserEmailForContextMenuAction { get; set; }

    public bool contextMenu { get; set; } = false;
    public bool showDialog { get; set; } = false;
    public bool SyncPopup { get; set; } = false;
    public bool WarngPopup { get; set; } = false;
    public bool isFirstClick { get; set; } = true;
    public bool isAxCryptUser { get; set; } = true;

    public void contextMenuPopup(EmailAddress email)
    {
        UserEmailForContextMenuAction = email;
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
        FileShareService.ViewModel.NewKeyShare = RecipientEmail.Trim();
        ClearErrorProviders();
    }

    public async void AddShareKeyUser()
    {
        if (string.IsNullOrWhiteSpace(RecipientEmail))
        {
            return;
        }

        EmailAddress addedUserEmailAddress;
        if (!EmailAddress.TryParse(RecipientEmail.Trim(), out addedUserEmailAddress))
        {
            return;
        }

        if (ShareKeyUserList.Any(user => user.UserEmail == addedUserEmailAddress))
        {
            return;
        }

        if (!IsConnected())
        {
            ShowHideOfflineError();
            return;
        }

        await AddEmailToKeyShareListAsync(addedUserEmailAddress);
        RecipientEmail = string.Empty;
        StateHasChanged();
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
            await ShareSelectedIndices(new List<EmailAddress>() { addedUserEmailAddress });

            AccountStatus accountStatus = await ShareNewContactAsync();
            if (accountStatus != AccountStatus.Unknown)
            {
                RecipientEmail = string.Empty;
            }

            AddKeySharingUserList(addedUserEmailAddress, accountStatus);
        }
    }

    private Task ShareSelectedIndices(IEnumerable<EmailAddress> newKeyShareUserList)
    {
        try
        {
            return FileShareService.ViewModel.AddKeyShares.ExecuteAsync(newKeyShareUserList);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    private async Task<AccountStatus> ShareNewContactAsync()
    {
        if (string.IsNullOrEmpty(RecipientEmail))
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
            await FileShareService.ViewModel.AddNewKeyShare.ExecuteAsync(FileShareService.ViewModel.NewKeyShare);
            if (FileShareService.ViewModel.SharedWith.Where(sw => sw.Email.ToString() == FileShareService.ViewModel.NewKeyShare).Any())
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
            //ErrorMessage = Texts.InvalidEmail;
        }

        return AccountStatus.Unknown;
    }

    private async Task<AccountStatus> VerifyNewKeyShareStatus()
    {
        await FileShareService.ViewModel.UpdateNewKeyShareStatus.ExecuteAsync(null);
        AccountStatus sharedUserAccountStatus = FileShareService.ViewModel.NewKeyShareStatus;

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
        ShareKeyUserList.Add(new ShareKeyUser(addedUserEmailAddress, accountStatus));
    }

    [Parameter]
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
                        await FileShareService.ViewModel.ShareFolders.ExecuteAsync(FileShareService.ViewModel.SharedWith);
                    }
                    break;

                case false:
                    using (await New<IProgressDialog>().Show(Texts.ProgressIndicatorWaitMessage, Texts.ProgressIndicatorWaitMessage))
                    {
                        await FileShareService.ViewModel.ShareFiles.ExecuteAsync(FileShareService.ViewModel.SharedWith);
                    }
                    break;
            }

            SelectedFilesOrFoldersList = Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task RemoveSharedKey()
    {
        await FileShareService.ViewModel.RemoveKeyShares.ExecuteAsync(_sharedKeyUsersList.Where(user => user.Email == UserEmailForContextMenuAction));
        RemoveKeySharingUserList(UserEmailForContextMenuAction);
        contextMenu = !contextMenu;
        UserEmailForContextMenuAction = EmailAddress.Empty;
    }

    private void RemoveKeySharingUserList(EmailAddress addedUserEmailAddress)
    {
        ShareKeyUser selectedSharedKeyUser = ShareKeyUserList.Single(skul => skul.UserEmail == addedUserEmailAddress);
        if (selectedSharedKeyUser == null)
        {
            return;
        }

        ShareKeyUserList.Remove(selectedSharedKeyUser);
        //UpdateUIElementsOnChange();
    }

    public void RefreshShare()
    {
        return;
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

    [Parameter]
    public IEnumerable<string> SelectedFilesOrFoldersList { get; set; } = new List<string>();

    public class EmailSuggestion
    {
        public string? Email { get; set; }
        public string? GroupName { get; set; }
        public string? Type { get; set; }
    }

    public List<EmailSuggestion> EmailSuggestions = new List<EmailSuggestion>();

    public List<EmailSuggestion> AllSuggestions = new List<EmailSuggestion>
    {
        new EmailSuggestion { Email = "user1@example.com", Type = "individual" },
        new EmailSuggestion { Email = "admin1@example.com", Type = "admin" },
        new EmailSuggestion { GroupName = "Finance Team", Type = "group" },
        new EmailSuggestion { Email = "desktopuser@example.com", Type = "desktop-contact" },
    };

    public void OnEmailInput(ChangeEventArgs e)
    {
        RecipientEmail = e.Value?.ToString();

        if (!string.IsNullOrEmpty(RecipientEmail))
        {
            EmailSuggestions = AllSuggestions
                .Where(s => s.Email?.Contains(RecipientEmail, StringComparison.OrdinalIgnoreCase) == true ||
                            s.GroupName?.Contains(RecipientEmail, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
            showSuggestionDropdown = EmailSuggestions.Any();
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

    [Parameter]
    public string Files { get; set; }

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
