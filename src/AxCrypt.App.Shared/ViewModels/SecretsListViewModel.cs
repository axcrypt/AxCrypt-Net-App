using AxCrypt.Api;
using AxCrypt.Api.Model.Secret;
using AxCrypt.App.Shared.Data;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Models.Secret;
using AxCrypt.App.Shared.Password;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Service.Secrets;
using AxCrypt.Core.UI;
using Microsoft.AspNetCore.Components;
using System.Collections.ObjectModel;
using System.Text;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.ViewModels;

public class SecretsListViewModel : AxCrypt.Core.UI.ViewModel.ViewModelBase
{
    private readonly int MaxRecentSecretsToShow = 10;
    private readonly string _sortOptionAll = Texts.SecretsAllItems;
    private readonly string _sortOptionRecent = Texts.SecretsRecentlyAdded;
    private readonly string _sortOptionShared = Texts.PromptSharedWith;

    private IStatusAlertService _StatusAlertService;

    public SecretsListViewModel()
    {
        _StatusAlertService = AxCServiceProviderExtension.StatusAlertService!;

        Initialize();
    }

    private void Initialize()
    {
        ClearErrorProviders();

        HasPaidSubscription = New<AccountStatusViewModel>().PlanState == PlanState.HasPremium || New<AccountStatusViewModel>().PlanState == PlanState.HasPasswordManager || New<AccountStatusViewModel>().PlanState == PlanState.HasBusiness;

        Keyword = "";
        SortOptionAllText = _sortOptionAll;
        SortOptionRecentText = _sortOptionRecent;
        SortOptionSharedText = _sortOptionShared;
        _CachedSecrets = new Dictionary<SecretFilterOption, ObservableCollection<SecretViewModel>>();
        FilteredSecrets = new ObservableCollection<SecretViewModel>();
        SearchedSecrets = new ObservableCollection<SecretViewModel>();
        Secrets = new ObservableCollection<SecretViewModel>();
        SelectedSecretListFilter = SecretFilterOption.All;
        SelectedSecretTypeFilter = 0;
        ShowSecretTypeCreateMenu = false;
        ShowSecretFilterMenu = false;
    }

    public void UpgradeFreeUser()
    {
        if (!HasPaidSubscription)
        {
            AxCServiceProviderExtension.UpgradeSubscriptionViewModel!.ShowUpgradeDialog();
            return;
        }
    }

    public async Task UpdateFreeUserSubscriptionAsync()
    {
        if (HasPaidSubscription)
        {
            return;
        }

        HasPaidSubscription = await ViewModelHelper.CheckFreeUserSecretsCountasync();
        HasNoSecretsCapability = HasPaidSubscription ? false : true;
    }

    public ObservableCollection<SecretViewModel> Secrets
    {
        get
        {
            return GetProperty<ObservableCollection<SecretViewModel>>(nameof(Secrets));
        }
        set
        {
            SetProperty(nameof(Secrets), value);
            if (!Secrets.Any())
            {
                _CachedSecrets = new Dictionary<SecretFilterOption, ObservableCollection<SecretViewModel>>();
                return;
            }

            SelectedSecretListFilter = SelectedSecretListFilter == SecretFilterOption.None ? SecretFilterOption.All : SelectedSecretListFilter;
            if (_CachedSecrets!.ContainsKey(SelectedSecretListFilter))
            {
                _CachedSecrets.Remove(SelectedSecretListFilter);
            }
            _CachedSecrets.Add(SelectedSecretListFilter, Secrets);
        }
    }

    private IDictionary<SecretFilterOption, ObservableCollection<SecretViewModel>>? _CachedSecrets;

    public ObservableCollection<SecretViewModel> FilteredSecrets
    {
        get
        {
            return GetProperty<ObservableCollection<SecretViewModel>>(nameof(FilteredSecrets));
        }
        set
        {
            SetProperty(nameof(FilteredSecrets), value);
            ShowSecretsList = FilteredSecrets?.Any() ?? false;
        }
    }

    public ObservableCollection<SecretViewModel> SearchedSecrets
    {
        get
        {
            return GetProperty<ObservableCollection<SecretViewModel>>(nameof(SearchedSecrets));
        }
        set
        {
            SetProperty(nameof(SearchedSecrets), value);
        }
    }

    public string Keyword
    {
        get { return GetProperty<string>(nameof(Keyword)); }
        set { SetProperty(nameof(Keyword), value); }
    }

    public SecretType SelectedSecretTypeFilter
    {
        get { return GetProperty<SecretType>(nameof(SelectedSecretTypeFilter)); }
        set { SetProperty(nameof(SelectedSecretTypeFilter), value); }
    }

    public SecretsFilter SelectedSecretFilter
    {
        get { return GetProperty<SecretsFilter>(nameof(SelectedSecretFilter)); }
        set { SetProperty(nameof(SelectedSecretFilter), value); }
    }

    public SecretFilterOption SelectedSecretListFilter
    {
        get { return GetProperty<SecretFilterOption>(nameof(SelectedSecretListFilter)); }
        set { SetProperty(nameof(SelectedSecretListFilter), value); }
    }

    public bool ShowSecretTypeCreateMenu { get { return GetProperty<bool>(nameof(ShowSecretTypeCreateMenu)); } set { SetProperty(nameof(ShowSecretTypeCreateMenu), value); } }

    public bool ShowSecretFilterMenu { get { return GetProperty<bool>(nameof(ShowSecretFilterMenu)); } set { SetProperty(nameof(ShowSecretFilterMenu), value); } }

    public bool HasPaidSubscription
    { get { return GetProperty<bool>(nameof(HasPaidSubscription)); } private set { SetProperty(nameof(HasPaidSubscription), value); } }

    public bool HasNoSecretsCapability { get { return GetProperty<bool>(nameof(HasNoSecretsCapability)); } private set { SetProperty(nameof(HasNoSecretsCapability), value); } }

    public bool ShowSecretsList
    { get { return GetProperty<bool>(nameof(ShowSecretsList)); } private set { SetProperty(nameof(ShowSecretsList), value); } }

    public string SortOptionAllText
    { get { return GetProperty<string>(nameof(SortOptionAllText)); } private set { SetProperty(nameof(SortOptionAllText), value); } }

    public string SortOptionRecentText
    { get { return GetProperty<string>(nameof(SortOptionRecentText)); } private set { SetProperty(nameof(SortOptionRecentText), value); } }

    public string SortOptionSharedText
    { get { return GetProperty<string>(nameof(SortOptionSharedText)); } private set { SetProperty(nameof(SortOptionSharedText), value); } }

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

    public bool CanShowErrorMessage
    {
        get { return GetProperty<bool>(nameof(CanShowErrorMessage)); }
        set { SetProperty(nameof(CanShowErrorMessage), value); }
    }

    public SecretsSortOrder SelectedSortOrder { get; set; } = SecretsSortOrder.None;
    public SecretFilterOption Filter { get; set; } = SecretFilterOption.All;
    public SecretsFilter SelectedFilter { get; set; } = SecretsFilter.All;

    public async Task OnInputChanged(ChangeEventArgs e)
    {
        Keyword = e.Value.ToString();
        await ApplyFilterOnSecrets();
    }

    public async Task SortSecrets(ChangeEventArgs e)
    {
        if (Enum.TryParse<SecretsSortOrder>(e.Value!.ToString(), out SecretsSortOrder sortOrder))
        {
            SelectedSortOrder = sortOrder;
        }

        await SortSecretsBy(SelectedSortOrder);
    }

    public async Task ExportFile(string fileType)
    {
        bool saveResult = false;

        switch (fileType)
        {
            case "TXT":
                saveResult = await ExportTextAsync();
                break;
            case "XML":
                saveResult = await ExportXml();
                break;
            default:
                _StatusAlertService?.Error("Unsupported file type.");
                return;
        }

        if (saveResult)
        {
            _StatusAlertService?.Success("Your file has been successfully downloaded!");
        }
        else
        {
            _StatusAlertService?.Error("Failed to download the file. Please check your internet connection and try again.");
        }
    }

    /// <summary>
    /// Get all secrets that match a specified keyword and belong to the current user
    /// </summary>
    /// <param name="keyword">Keyword to search with</param>
    /// <returns>SecretCollection containing found secrets</returns>
    public async Task FindSecrets()
    {
        if (_CachedSecrets!.ContainsKey(SecretFilterOption.All))
        {
            Secrets = _CachedSecrets[SecretFilterOption.All];
            ApplyFilter();
            return;
        }

        SecretClientCollection secrets;
        await using (ProcessIndicator processIndicator = new ProcessIndicator())
        {
            secrets = await PersonalSecrets.SelectBySearch(Keyword ?? "");
        }

        InitializeSecrets(secrets);
    }

    private async Task FindSharedWithSecrets()
    {
        if (_CachedSecrets!.ContainsKey(SecretFilterOption.Shared))
        {
            Secrets = _CachedSecrets[SecretFilterOption.Shared];
            ApplyFilter();
            return;
        }

        FilteredSecrets = new ObservableCollection<SecretViewModel>();
        SecretClientCollection sharedWithSecrets;
        await using (ProcessIndicator processIndicator = new ProcessIndicator())
        {
            sharedWithSecrets = await SharedSecrets.SelectBySearch(Keyword ?? "");
        }

        InitializeSecrets(sharedWithSecrets);
    }

    private void InitializeSecrets(SecretClientCollection secrets)
    {
        IEnumerable<SecretViewModel> secretsList = secrets.Select(sc => { return new SecretViewModel(sc); });
        Secrets = new ObservableCollection<SecretViewModel>(secretsList);
        ApplyFilter();
    }

    public async Task FilterSecretsBy(SecretsFilter type)
    {
        if (SelectedSecretFilter == type)
        {
            return;
        }

        SelectedSecretFilter = type;
        await ApplyFilterOnSecrets();
    }

    public async Task ApplyFilterOnSecrets()
    {
        await using (ProcessIndicator processIndicator = new ProcessIndicator())
        {
            await Task.Run(() =>
            {
                ApplyFilter();
            });
        }
    }

    private void ApplyFilter()
    {
        SearchSecrets();

        IList<SecretViewModel> filteredSecrets = FilterSecretsByType();
        FilteredSecrets = new ObservableCollection<SecretViewModel>(filteredSecrets);

        switch (SelectedSecretListFilter)
        {
            case SecretFilterOption.None:
            case SecretFilterOption.All:
                FilteredSecrets = new ObservableCollection<SecretViewModel>(FilteredSecrets);
                SortOptionAllText = _sortOptionAll + $" ({FilteredSecrets.Count})";
                SortOptionRecentText = _sortOptionRecent;
                SortOptionSharedText = _sortOptionShared;
                break;

            case SecretFilterOption.Recently:
                FilteredSecrets = new ObservableCollection<SecretViewModel>(FilteredSecrets.OrderByDescending(x => x.UpdatedUtc).Take(MaxRecentSecretsToShow));
                SortOptionAllText = _sortOptionAll;
                SortOptionRecentText = _sortOptionRecent + $" ({FilteredSecrets.Count})";
                SortOptionSharedText = _sortOptionShared;
                break;

            case SecretFilterOption.Shared:
                FilteredSecrets = new ObservableCollection<SecretViewModel>(FilteredSecrets.OrderByDescending(x => x.SharedWith != null).Take(MaxRecentSecretsToShow));
                SortOptionAllText = _sortOptionAll;
                SortOptionRecentText = _sortOptionRecent;
                SortOptionSharedText = _sortOptionShared + $" ({FilteredSecrets.Count})";
                break;

            default:
                break;
        }
    }

    private void SearchSecrets()
    {
        IEnumerable<SecretClientModel> allSecrets = Secrets.Select(se => se.ToClientModel(se.SecretGuid)).ToList();
        IEnumerable<SecretClientModel> searchResult = PersonalSecrets.SearchInSecrets(allSecrets, Keyword);

        SearchedSecrets = new ObservableCollection<SecretViewModel>(searchResult.Select(sc => { return new SecretViewModel(sc); }));
    }

    private IList<SecretViewModel> FilterSecretsByType()
    {
        int totalCount = SearchedSecrets.Count;
        SecretViewModel[] secrets = SearchedSecrets.ToArray();

        IList<SecretViewModel> filteredSecrets = new List<SecretViewModel>();
        for (int i = 0; i < totalCount; i++)
        {
            SecretViewModel secretModel = secrets[i];
            if (SelectedSecretFilter != 0 && secretModel.SecretType.ToString() != SelectedSecretFilter.ToString())
            {
                continue;
            }

            filteredSecrets.Add(secretModel);
        }

        return filteredSecrets;
    }

    public async Task FilterSecretsBy(SecretFilterOption secretFilter)
    {
        if (SelectedSecretListFilter == secretFilter)
        {
            return;
        }

        SelectedSecretListFilter = secretFilter;
        UpdateSecretListFilterStyle(secretFilter);

        await FindSecrets();
        await ApplyFilterOnSecrets();
    }

    public async Task FilterSharedSecrets(SecretFilterOption secretFilter)
    {
        if (SelectedSecretListFilter == secretFilter)
        {
            return;
        }

        SelectedSecretListFilter = secretFilter;
        UpdateSecretListFilterStyle(secretFilter);

        await FindSharedWithSecrets();

        await ApplyFilterOnSecrets();
    }

    private void UpdateSecretListFilterStyle(SecretFilterOption secretFilter = SecretFilterOption.None)
    {
        switch (secretFilter)
        {
            case SecretFilterOption.None:
            case SecretFilterOption.All:
                LoadCachedSecretsByFilter(SecretFilterOption.All);
                break;

            case SecretFilterOption.Recently:
                LoadCachedSecretsByFilter(SecretFilterOption.All);
                break;

            case SecretFilterOption.Shared:
                LoadCachedSecretsByFilter(SecretFilterOption.Shared);
                break;

            default:
                break;
        }
    }

    private void LoadCachedSecretsByFilter(SecretFilterOption filterOption)
    {
        if (_CachedSecrets!.ContainsKey(filterOption))
        {
            Secrets = _CachedSecrets[filterOption];
        }
    }

    public async Task SortSecretsBy(SecretsSortOrder sortorderOption)
    {
        await ApplyFilterOnSecrets();

        ShowSecretFilterMenu = false;
        List<SecretViewModel> combinedSecrets = new List<SecretViewModel>();
        if (sortorderOption == SecretsSortOrder.ByContent)
        {
            combinedSecrets.AddRange(FilteredSecrets.OrderBy(s => SecDescription(s)));
            FilteredSecrets = new ObservableCollection<SecretViewModel>(combinedSecrets);
            return;
        }

        combinedSecrets.AddRange(FilteredSecrets);
        FilteredSecrets = new ObservableCollection<SecretViewModel>(combinedSecrets);
    }

    private static string? SecDescription(SecretViewModel? secret)
    {
        if (secret?.SecretType == SecretType.Card)
        {
            return secret.Card.SecretDesc;
        }

        if (secret?.SecretType == SecretType.Note)
        {
            return secret.Note.SecretDesc;
        }

        return secret?.Password.SecretDesc;
    }

    private void ClearErrorProviders()
    {
        ErrorMessage = "";
    }

    public async Task<bool> ExportTextAsync()
    {
        LogOnIdentity identity = New<AxCrypt.Core.UI.KnownIdentities>().DefaultEncryptionIdentity;
        byte[] txtData = GetSecretsTxtData();
        if (txtData == null)
        {
            return false;
        }

        string downloadsFolderPath = GetDownloadsFolderPath();
        if (downloadsFolderPath == null)
        {
            await New<IPopup>().ShowAsync(PopupButtons.Ok, "Alert", "Could not determine the Downloads folder path.");
            return false;
        }

        string fileName = identity.UserEmail.Address + ".secrets.txt";
        string filePath = Path.Combine(downloadsFolderPath, fileName);

        int count = 1;
        while (File.Exists(filePath))
        {
            string tempFileName = $"{identity.UserEmail.Address}.secrets({count}).txt";
            filePath = Path.Combine(downloadsFolderPath, tempFileName);
            count++;
        }

        await File.WriteAllBytesAsync(filePath, txtData);
        return true;
    }

    public byte[] GetSecretsTxtData()
    {
        FilterSecretsByType();
        if (FilteredSecrets == null || FilteredSecrets.Count == 0)
        {
            return null!;
        }

        StringBuilder sb = new StringBuilder();

        foreach (SecretViewModel secretViewModel in FilteredSecrets)
        {
            sb.Append("Type: ");
            sb.Append(secretViewModel.SecretType.ToString());
            sb.Append(Environment.NewLine);

            switch (secretViewModel.SecretType)
            {
                case SecretType.Legacy:
                case SecretType.Password:
                    sb.Append("Url: ");
                    sb.Append(secretViewModel.Password.Url.Trim());
                    sb.Append(Environment.NewLine);
                    sb.Append("Description: ");
                    sb.Append(secretViewModel.Password.SecretDesc?.Trim());
                    sb.Append(Environment.NewLine);
                    sb.Append("Username: ");
                    sb.Append(secretViewModel.Password.Username.Trim());
                    sb.Append(Environment.NewLine);
                    sb.Append("Secret: ");
                    sb.Append(secretViewModel.Password.SecretValue.Trim());
                    sb.Append(Environment.NewLine);
                    break;

                case SecretType.Card:
                    sb.Append("CardNumber: ");
                    sb.Append(secretViewModel.Card.CardNumber.Trim());
                    sb.Append(Environment.NewLine);
                    sb.Append("Description: ");
                    sb.Append(secretViewModel.Card.SecretDesc?.Trim());
                    sb.Append(Environment.NewLine);
                    sb.Append("NameOnCard: ");
                    sb.Append(secretViewModel.Card.NameOnCard.Trim());
                    sb.Append(Environment.NewLine);
                    sb.Append("SecurityCode: ");
                    sb.Append(secretViewModel.Card.SecurityCode.Trim());
                    sb.Append(Environment.NewLine);
                    sb.Append("ExpirationDate: ");
                    sb.Append(secretViewModel.Card.ExpirationDate.Trim());
                    sb.Append(Environment.NewLine);
                    break;

                case SecretType.Note:
                    sb.Append("Description: ");
                    sb.Append(secretViewModel.Note.SecretDesc?.Trim());
                    sb.Append(Environment.NewLine);
                    sb.Append("Note: ");
                    sb.Append(secretViewModel.Note.Note.Trim());
                    sb.Append(Environment.NewLine);
                    break;
            }
            sb.Append(Environment.NewLine);
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<bool> ExportXml()
    {
        LogOnIdentity identity = New<AxCrypt.Core.UI.KnownIdentities>().DefaultEncryptionIdentity;
        Stream xmlStream = await GetRawXML();
        if (xmlStream == Stream.Null && New<AxCryptOnlineState>().IsOffline)
        {
            _StatusAlertService?.Error("Could not download the file when offline.");
            return false;
        }

        string downloadsFolderPath = GetDownloadsFolderPath();
        if (downloadsFolderPath == null)
        {
            await New<IPopup>().ShowAsync(PopupButtons.Ok, "Alert", "Could not determine the Downloads folder path.");
            return false;
        }

        string fileName = identity.UserEmail.Address + ".secrets.xml";
        string filePath = Path.Combine(downloadsFolderPath, fileName);

        int count = 1;
        while (File.Exists(filePath))
        {
            string tempFileName = $"{identity.UserEmail.Address}.secrets({count}).xml";
            filePath = Path.Combine(downloadsFolderPath, tempFileName);
            count++;
        }

        using (StreamReader reader = new StreamReader(xmlStream))
        {
            string xmlData = await reader.ReadToEndAsync();
            await File.WriteAllTextAsync(filePath, xmlData);
        }
        return true;
    }

    private async Task<Stream> GetRawXML()
    {
        LogOnIdentity identity = New<AxCrypt.Core.UI.KnownIdentities>().DefaultEncryptionIdentity;
        SecretsListRequestOptions requestOptions = new SecretsListRequestOptions(identity.UserEmail.Address)
        {
            GetRawXml = true
        };

        string userSecrets = await New<LogOnIdentity, ISecretsService>(identity).ExportXMLSecretsAsync(requestOptions);

        if (userSecrets == null)
        {
            return Stream.Null;
        }

        byte[] byteArray = Encoding.UTF8.GetBytes(userSecrets);
        Stream stream = new MemoryStream(byteArray);
        return stream;
    }

    private string GetDownloadsFolderPath()
    {
        string downloadsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        return downloadsFolderPath;
    }
}