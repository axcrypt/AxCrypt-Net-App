using AxCrypt.App.Shared.Utility;

namespace AxCrypt.App.Shared.Services.Interface;

public interface IFilePasswordWindowService
{
    bool ShouldSuppressMainWindowReload { get; }

    Task<DialogResult> ShowWindow(string? encryptedFileFullName);

    void Close();
}
