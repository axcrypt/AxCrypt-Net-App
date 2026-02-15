using AxCrypt.Content;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.IO;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Core.UI;
using System.Windows.Input;
using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.Core.Extensions;
using AxCrypt.App.Shared.Desktop.UI.Services;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using System;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Services;

namespace AxCrypt.App.Shared.Desktop.ViewModels.RecentFiles
{
    public class DesktopFilePasswordViewModel : ViewModelBase
    {
        // MobileFilePasswordViewModel is a wrapper for working with desktop implementation of FilePasswordViewModel.
        // We must work with it in background thread, so we can't inherit from it.
        private FilePasswordViewModel? _filePasswordViewModel;

        private IDataStore? _encryptedFile;

        private IDataStore? _keyFile;

        private bool _isPasswordSubmitting;

        public DesktopFilePasswordViewModel(IDataStore dataStore, ICommand submitPasswordCommand)
        {
            _encryptedFile = dataStore;
            SubmitFilePasswordCommand = submitPasswordCommand;

            InitializePropertyValues();
            Initialize(dataStore.FullName);
        }

        private async void Initialize(string fileName)
        {
            try
            {
                FilePasswordDialogViewModel filePasswordDialog = AxCServiceProvider.GetService<FilePasswordDialogViewModel>();
                await filePasswordDialog.ShowFilePasswordDialog(fileName);
                if (filePasswordDialog.DialogResult == Shared.Utility.DialogResult.Cancel)
                {
                    return;
                }

                _filePasswordViewModel = filePasswordDialog.ViewModel;

                BindPropertyChangedEvents();
                BindToInternalViewModel();
                IsFilePasswordAsked = true;
                SubmitFilePasswordCommand!.Execute(null);
            }
            catch (Exception ex)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, ex.Message);
            }
        }

        private void InitializePropertyValues()
        {
            PassphraseText = string.Empty;
            KeyFileName = Texts.KeyFilePrompt;
            ExpandOrHideAdditionalOptionsCommand = new Command(ExpandOrHideAdditionalOptions);
            CancelCommand = new Command(Cancel);
            ChooseKeyFileCommand = new Command<object>(ChooseKeyFile);
        }

        private void BindPropertyChangedEvents()
        {
            BindPropertyChangedInternal<string>(nameof(PassphraseText), passphrase => _filePasswordViewModel!.PasswordText = passphrase);
        }

        public IDataStore EncryptedFile
        {
            get
            {
                return _encryptedFile!;
            }
        }

        public string PassphraseText { get { return GetProperty<string>(nameof(PassphraseText)); } set { SetProperty(nameof(PassphraseText), value); } }

        public string FileName { get { return GetProperty<string>(nameof(FileName)); } set { SetProperty(nameof(FileName), value); } }

        public bool IsLegacyFile { get { return GetProperty<bool>(nameof(IsLegacyFile)); } set { SetProperty(nameof(IsLegacyFile), value); } }

        public string KeyFileName { get { return GetProperty<string>(nameof(KeyFileName)); } set { SetProperty(nameof(KeyFileName), value); } }

        public bool IsAdditionalOptionsAvailable { get { return GetProperty<bool>(nameof(IsAdditionalOptionsAvailable)); } set { SetProperty(nameof(IsAdditionalOptionsAvailable), value); } }

        public bool IsFilePasswordAsked { get { return GetProperty<bool>(nameof(IsFilePasswordAsked)); } set { SetProperty(nameof(IsFilePasswordAsked), value); } }

        private void BindToInternalViewModel()
        {
            _filePasswordViewModel!.BindPropertyChanged<string>(nameof(FilePasswordViewModel.PasswordText), passphraseText => PassphraseText = passphraseText);
            _filePasswordViewModel.BindPropertyChanged<string>(nameof(FilePasswordViewModel.FileName), fileName => FileName = fileName);
            _filePasswordViewModel.BindPropertyChanged<bool>(nameof(FilePasswordViewModel.IsLegacyFile), isLegasy => IsLegacyFile = isLegasy);
        }

        public ICommand? SubmitFilePasswordCommand { get; private set; }

        public ICommand? ExpandOrHideAdditionalOptionsCommand { get; private set; }

        public ICommand? ChooseKeyFileCommand { get; private set; }

        public ICommand? CancelCommand { get; private set; }

        public async Task<Passphrase?> SubmitFilePassword()
        {
            // Ignore multiple password submitting (when user presses button more then one time).
            if (_isPasswordSubmitting)
            {
                return null!;
            }

            _isPasswordSubmitting = true;
            try
            {
                string errorMessage = ErrorMessage();

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, errorMessage);
                    return null!;
                }

                Passphrase passphrase = await Task.Run(() => _filePasswordViewModel!.Passphrase);
                IsFilePasswordAsked = false;
                return passphrase;
            }
            finally
            {
                _isPasswordSubmitting = false;
            }
        }

        private string ErrorMessage()
        {
            if (_filePasswordViewModel![nameof(FilePasswordViewModel.KeyFileName)].Length > 0)
            {
                return Texts.FileNotFound;
            }

            if (_filePasswordViewModel[nameof(FilePasswordViewModel.PasswordText)].Length == 0)
            {
                return string.Empty;
            }

            if (String.IsNullOrEmpty(_filePasswordViewModel.FileName))
            {
                return Texts.UnknownLogOn;
            }
            else
            {
                return _filePasswordViewModel.ValidationError.ToValidationMessage();
            }
        }

        private void ExpandOrHideAdditionalOptions()
        {
            IsAdditionalOptionsAvailable = !IsAdditionalOptionsAvailable;
        }

        private void Cancel()
        {
            IsFilePasswordAsked = false;
            _filePasswordViewModel = new FilePasswordViewModel("");
        }

        private async void ChooseKeyFile(object anchorView)
        {
            IFilePicker filePicker = New<IFilePicker>();
            FilePickerParameters paramaters = new FilePickerParameters
            {
                DisplayngAnchorView = anchorView,
            };

            _keyFile = await filePicker.ChooseFileAsync(paramaters);
            _filePasswordViewModel!.KeyFileName = string.Empty;

            string newFileName = Texts.KeyFilePrompt;
            if (_keyFile != null)
            {
                newFileName = _keyFile.Name;
            }

            _filePasswordViewModel.KeyFileName = _keyFile?.FullName!;
            KeyFileName = newFileName;
        }
    }
}
