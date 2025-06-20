using AxCrypt.Core.IO;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.UI.FileActivity;

public class FileActivityStore : ViewModelBase
{
    private IDataContainer _workContainer;
    public static readonly string FileActivityLogFileName = "FileActivityLog.txt";
    private IList<string> Settings = new List<string>();

    private IDataStore FileActivityLogStore => _workContainer.FileItemInfo(FileActivityLogFileName);

    public FileActivityStore()
    {
        _workContainer = Resolve.WorkFolder.FileInfo;
        IDataStore dataStore = FileActivityLogStore;
        if (!dataStore.IsAvailable)
        {
            using Stream writeStream = dataStore.OpenWrite();
            using StreamWriter writer = new StreamWriter(writeStream);
            writer.Write(string.Empty);
        }

        using (New<FileLocker>().Acquire(dataStore))
        {
            Initialize(dataStore.OpenRead());
        }
    }

    private void Initialize(Stream readStream)
    {
        using StreamReader reader = new StreamReader(readStream);
        IList<string> lines = new List<string>();
        while (!reader.EndOfStream)
        {
            string? line = reader.ReadLine();
            if (!string.IsNullOrWhiteSpace(line))
            {
                lines.Add(line);
            }
        }

        Settings = lines;
    }

    public IEnumerable<string> GetFileActivityLogs()
    {
        return Settings;
    }

    public void Save(string logLine)
    {
        Settings.Add(logLine);

        using Stream writeStream = FileActivityLogStore.OpenWrite();
        using StreamWriter writer = new StreamWriter(writeStream);
        foreach (string line in Settings)
        {
            writer.WriteLine(line);
        }

        UpdateViewState();
    }

    public void Clear()
    {
        Settings.Clear();
        using Stream writeStream = FileActivityLogStore.OpenWrite();
        using StreamWriter writer = new StreamWriter(writeStream);
        writer.Write(string.Empty);
    }
}
