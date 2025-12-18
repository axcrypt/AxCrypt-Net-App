using AxCrypt.App.Shared.Helpers;
using AxCrypt.Core.UI;

namespace AxCrypt.App.Shared.Services.UI
{
    public class CustomProgressBar : IDisposable
    {
        private readonly ProgressBarService? _progressBarService;

        public CustomProgressBar()
        {
            try
            {
                _progressBarService = AxCServiceProviderExtension.ProgressBarService;
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

        public IProgressContext ProgressContext
        {
            set
            {
                _progressBarService.ProgressContext = value;
            }
        }


        public void Dispose()
        {
            _progressBarService?.Hide();
        }
    }

}
