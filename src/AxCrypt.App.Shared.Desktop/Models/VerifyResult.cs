namespace AxCrypt.App.Shared.Desktop.Models;

/// <summary>Result of AxCrypt file verification.</summary>
public class VerifyResult
{
    public bool IsValid { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string EncryptedDate { get; set; } = string.Empty;
}
