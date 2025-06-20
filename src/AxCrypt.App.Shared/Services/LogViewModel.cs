using AxCrypt.Core.UI.ViewModel;

namespace AxCrypt.App.Shared.Services
{
    public class LogViewModel : ViewModelBase
    {
        private readonly object _lock = new();
        private readonly List<string> _logs = new();

        public bool IsDebugLogsVisible { get; set; }

        public IReadOnlyList<string> Logs
        {
            get
            {
                lock (_lock)
                {
                    return _logs.ToList(); // return snapshot
                }
            }
        }

        public async Task AddLogAsync(string log)
        {
            lock (_lock)
            {
                _logs.Add($"{DateTime.Now:T}: {log}");
            }

            LoadLogData();
        }

        public void LoadLogData()
        {
            UpdateViewState(); // Calls StateHasChanged via base class
        }

        public void ClearLogs()
        {
            lock (_lock)
            {
                _logs.Clear();
            }
        }
    }
}
