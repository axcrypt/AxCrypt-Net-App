using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.Content;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.UI;
using AxCrypt.Cryptor;
using Microsoft.AspNetCore.Components.Web;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.ViewModels;

public class TextEncryptionViewModel
{
    public int MaxAllowedCharators = 0;

    public string? InputText { get; set; }

    public string? EncryptedText { get; set; }

    public string? Password { get; set; }

    public string? ErrorMessage { get; set; }

    public string? SelectedTap { get; set; }


    public IStatusAlertService _StatusAlertService;

    public TextEncryptionViewModel()
    {
        _StatusAlertService = AxCServiceProviderExtension.StatusAlertService!;
    }

    public void Initialize()
    {
        MaxAllowedCharators = GetMaxCharaterLimit();
        IsCustomPasswordMode = false;
    }

    private bool isPasswordVisible = false;

    public string PasswordInputType => isPasswordVisible ? "text" : "password";

    public string PasswordIconClass =>
        isPasswordVisible ? "hide-password-ico" : "show-password-ico";

    public void TogglePassword()
    {
        isPasswordVisible = !isPasswordVisible;
    }

    public string HoveredElement { get; set; } = string.Empty;
    public bool IsHovered { get; set; } = false;

    public void ShowPopup(string element)
    {
        IsHovered = true;
        HoveredElement = element;
    }

    public void HidePopup()
    {
        IsHovered = false;
        HoveredElement = string.Empty;
    }

    public bool IsCustomPasswordMode = false;

    public void ShowHideCustomPassword(MouseEventArgs args)
    {
        IsCustomPasswordMode = !IsCustomPasswordMode;
        Password = "";
    }

    private int GetMaxCharaterLimit()
    {
        if (AxCServiceProviderExtension.LogOnViewModel!.UserHas(Core.Runtime.LicenseCapability.TextEncryptionBusiness))
        {
            return 2000;
        }

        if (AxCServiceProviderExtension.LogOnViewModel!.UserHas(Core.Runtime.LicenseCapability.TextEncryptionPremium))
        {
            return 1000;
        }

        return 0;
    }

    public async Task EncryptText()
    {
        ErrorMessage = "";
        EncryptedText = "";
        if (string.IsNullOrWhiteSpace(InputText))
        {
            ErrorMessage = "Input text is required.";
            return;
        }

        if (InputText.Length > MaxAllowedCharators)
        {
            ErrorMessage = $"Text should not exceed {MaxAllowedCharators} characters.";
            return;
        }

        try
        {
            EncryptedText = await TextCryptor.EncryptTextAsync(Identity(), InputText, null);
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to encrypt text due to {ex.Message}";
        }
    }

    public void DecryptText()
    {
        ErrorMessage = "";
        InputText = "";
        if (string.IsNullOrWhiteSpace(EncryptedText))
        {
            ErrorMessage = "Encrypted text is required.";
            return;
        }

        try
        {
            InputText = TextCryptor.DecryptText(Identity(), EncryptedText, null);
            if (string.IsNullOrEmpty(InputText))
            {
                ErrorMessage = Texts.WrongPassphrase;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Failed to decrypt text!";
        }
    }

    private LogOnIdentity Identity()
    {
        LogOnIdentity logOnIdentity = New<KnownIdentities>().DefaultEncryptionIdentity;
        if (!string.IsNullOrEmpty(Password))
        {
            logOnIdentity = new LogOnIdentity(Password);
        }

        return logOnIdentity;
    }

    public void ResetBtn()
    {
        InputText = "";
        Password = "";
        EncryptedText = "";
        ErrorMessage = "";
        IsCustomPasswordMode = false;
    }

    public async Task<bool> ExportTextAsync()
    {
        string filename = "encryptedtext";
        string? exportText = "";
        if (SelectedTap == "TextEncryption")
        {
            exportText = EncryptedText;
            filename = "encryptedtext";
        }
        else if (SelectedTap == "TextDecryption")
        {
            exportText = InputText;
            filename = "text";
        }

        if (string.IsNullOrEmpty(exportText))
            return false;

        return await DownloadTextAsFileAsync(exportText, filename);
    }

    private async Task<bool> DownloadTextAsFileAsync(string exportText, string downloadFileName)
    {
        byte[] txtData = Encoding.UTF8.GetBytes(exportText);
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

        string fileName = Core.Resolve.UserSettings.UserEmail + $"_{downloadFileName}.txt";
        string filePath = Path.Combine(downloadsFolderPath, fileName);

        int count = 1;
        while (File.Exists(filePath))
        {
            string tempFileName = $"{Core.Resolve.UserSettings.UserEmail}._{downloadFileName}_({count}).txt";
            filePath = Path.Combine(downloadsFolderPath, tempFileName);
            count++;
        }

        await File.WriteAllBytesAsync(filePath, txtData);
        _StatusAlertService?.Success($"Your file has been successfully downloaded at {filePath}");
        return true;
    }

    private string GetDownloadsFolderPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }

    public void EncryptSendEmailBtn()
    {
        if (string.IsNullOrEmpty(EncryptedText))
        {
            return;
        }

        string to = "";
        string subject = "Sharing AxCrypt Encrypted Text";

        string body =
        "Hello,\n\n" +
        "This text is encrypted — use AxCrypt to view the original message.\n\n" +
        "Encrypted Text:\n" +
        EncryptedText + "\n\n";

        string mailto = $"mailto:{to}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";

        Process.Start(new ProcessStartInfo(mailto) { UseShellExecute = true });
    }

    public void EncryptShareLinkBtn()
    {

    }
}
