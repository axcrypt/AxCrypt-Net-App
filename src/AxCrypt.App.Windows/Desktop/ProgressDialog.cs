using AxCrypt.Abstractions;

namespace AxCrypt.App.Windows.Desktop;

public class ProgressDialog : IProgressDialog
{
    public Task<ProgressDialogClosingToken> Show(string title, string message)
    {
        return Task.FromResult(new ProgressDialogClosingToken());
    }
}
