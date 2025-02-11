using AxCrypt.App.Shared.Services;
using System;

namespace AxCrypt.App.Desktop.Services.UI;

public class CustomProgressBar : IDisposable
{
    private readonly ProgressBarService? _progressBarService;

    public CustomProgressBar()
    {
        try
        {
            _progressBarService = AxCServiceProvider.ProgressBarService;
            if (_progressBarService != null)
            {
                _progressBarService.Show();
            }
        }
        catch (Exception exp)
        {
            Console.WriteLine(nameof(CustomProgressBar) + " " + exp.Message);
        }
    }

    public string? Filename
    {
        set
        {
            _progressBarService.Filename = value;
        }
    }

    public double Percentage
    {
        set
        {
            _progressBarService.Percentage = value;
        }
    }


    public void Dispose()
    {
        _progressBarService?.Hide();
    }
}
