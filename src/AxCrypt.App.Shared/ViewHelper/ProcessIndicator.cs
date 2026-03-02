using AxCrypt.App.Shared.Services;

namespace AxCrypt.App.Shared.Utility.View;

public class ProcessIndicator : IDisposable, IAsyncDisposable
{
    private readonly ProcessIndicatorService? _processIndicatorService;

    public ProcessIndicator(bool isFullScreen = false)
    {
        try
        {
            _processIndicatorService = AxCServiceProvider.GetService<ProcessIndicatorService>();
            if (_processIndicatorService != null)
            {
                _processIndicatorService.Show(null, null, isFullScreen);
            }
        }
        catch (Exception exp)
        {
            Console.WriteLine(exp.Message);
        }
    }

    public void Dispose()
    {
        _processIndicatorService?.Hide();
    }

    public async ValueTask DisposeAsync()
    {
        _processIndicatorService?.Hide();
    }
}