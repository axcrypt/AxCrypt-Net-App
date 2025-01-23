using AxCrypt.App.Shared.Services;

namespace AxCrypt.App.Shared.Utility.View;

public class ProcessIndicator : IDisposable
{
    private readonly ProcessIndicatorService? _processIndicatorService;

    public ProcessIndicator(ProcessIndicatorService processIndicatorService)
    {
        try
        {
            _processIndicatorService = processIndicatorService;
            if (_processIndicatorService != null)
            {
                _processIndicatorService.Show();
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
}