using AxCrypt.App.Shared.Services;

namespace AxCrypt.App.Shared.Utility.View.ViewHelper;

public class FileOperationProcessIndicator : IDisposable, IAsyncDisposable
{
    private readonly FileOperationProcessIndicatorService? _fileOperationProcessIndicatorService;

    public FileOperationProcessIndicator()
    {
        try
        {
            _fileOperationProcessIndicatorService = AxCServiceProvider.GetService<FileOperationProcessIndicatorService>();
            if (_fileOperationProcessIndicatorService != null)
            {
                _fileOperationProcessIndicatorService.Show();
            }
        }
        catch (Exception exp)
        {
            Console.WriteLine(exp.Message);
        }
    }

    public void Dispose()
    {
        _fileOperationProcessIndicatorService?.Hide();
    }

    public async ValueTask DisposeAsync()
    {
        _fileOperationProcessIndicatorService?.Hide();
    }
}