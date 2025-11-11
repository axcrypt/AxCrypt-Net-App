using AxCrypt.App.Shared.Services.UI;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Content;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using Microsoft.Maui.Storage;
using System;
using System.Threading.Tasks;

namespace AxCrypt.App.Shared.Desktop.ViewModels;

public class ImportPrivateKeyViewModel : ViewModelBase
{
    private ImportPrivateKeysViewModel? _viewModel;

    public ImportPrivateKeyViewModel()
    {
        PageResult = DialogResult.None;
        ImportPrivateKeyDialog = new CommonDialogService();
    }

    public CommonDialogService ImportPrivateKeyDialog
    { get { return GetProperty<CommonDialogService>(nameof(ImportPrivateKeyDialog)); } set { SetProperty(nameof(ImportPrivateKeyDialog), value); } }

    public async Task ShowDialogAsync(UserSettings userSettings, KnownIdentities knownIdentities)
    {
        PageResult = DialogResult.None;
        PrivateKeyFileName = string.Empty;
        PasswordText = string.Empty;
        ErrorMessage = string.Empty;
        _viewModel = new ImportPrivateKeysViewModel(userSettings, knownIdentities);

        Initialized();

        ImportPrivateKeyDialog.Show();

        while (PageResult == DialogResult.None)
        {
            await Task.Delay(1000);
        }

        ImportPrivateKeyDialog.Close();
    }

    private void Initialized()
    {
        _viewModel!.BindPropertyChanged<bool>(nameof(_viewModel.ImportSuccessful), (ok) => { if (!ok) { ErrorMessage = Texts.FailedPrivateImport; } });
        _viewModel.BindPropertyChanged<bool>(nameof(_viewModel.ShowPassword), (show) => { ShowPassPhrase = show; });
    }

    public string? ErrorMessage { get; set; }
    public bool ShowPassPhrase { get; set; }
    public string? PrivateKeyFileName { get; set; }
    public string? PasswordText { get; set; }
    public DialogResult PageResult { get; set; }

    public async void ButtonOk_Click(EventArgs e)
    {
        _viewModel!.PasswordText = PasswordText!;
        if (!AdHocValidationDueToMonoLimitations())
        {
            PageResult = DialogResult.None;
            return;
        }

        await _viewModel.ImportFile.ExecuteAsync(null!);
        if (!_viewModel.ImportSuccessful)
        {
            PageResult = DialogResult.None;
            ErrorMessage = Texts.FailedPrivateImport;
            return;
        }
        PageResult = DialogResult.OK;
    }

    private bool AdHocValidationDueToMonoLimitations()
    {
        bool validated = true;
        try
        {
            if (_viewModel![nameof(ImportPrivateKeysViewModel.PrivateKeyFileName)].Length > 0)
            {
                ErrorMessage = Texts.FileNotFound;
                validated = false;
            }
            else
            {
                ErrorMessage = "";
            }

            if (_viewModel[nameof(ImportPrivateKeysViewModel.PasswordText)].Length > 0)
            {
                ErrorMessage = Texts.WrongPassphrase;
                validated = false;
            }
            else
            {
                ErrorMessage = "";
            }

            return validated;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.ToString();
            return validated;
        }
    }

    public async Task BrowsePrivateKeyFile()
    {
        FileResult? fileResult = await FilePicker.Default.PickAsync();
        if (fileResult != null)
        {
            PrivateKeyFileName = fileResult.FullPath;
            _viewModel!.PrivateKeyFileName = fileResult.FullPath;
        }
    }

    public void ClearErrorProviders()
    {
        ErrorMessage = "";
    }
}