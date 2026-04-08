using AxCrypt.App.Shared.Utility;

namespace AxCrypt.App.Shared.Services.Interface;

public interface IFilePasswordWindowService
{
    Task<DialogResult> ShowWindow(string? encryptedFileFullName);

    void Close();
}
