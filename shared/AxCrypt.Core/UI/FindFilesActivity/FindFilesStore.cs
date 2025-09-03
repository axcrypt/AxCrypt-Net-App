using AxCrypt.Core.IO;
using Newtonsoft.Json;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.UI.FindFilesActivity;

public class FindFilesStore
{
    public static readonly string FINDFILESLOGFILENAME = "SecuredFiles.txt";

    private IList<FindFilesLog> _securedFilesList = new List<FindFilesLog>();

    private IDataStore _findFilesLogStore => Resolve.WorkFolder.FileInfo.FileItemInfo(FINDFILESLOGFILENAME);

    public FindFilesStore()
    {
        if (!_findFilesLogStore.IsAvailable)
        {
            using Stream writeStream = _findFilesLogStore.OpenWrite();
            using StreamWriter writer = new StreamWriter(writeStream);
            writer.Write("[]");
        }

        using (New<FileLocker>().Acquire(_findFilesLogStore))
        {
            Initialize(_findFilesLogStore.OpenRead());
        }
    }

    private void Initialize(Stream readStream)
    {
        using StreamReader reader = new StreamReader(readStream);
        string securedFilesListJson = reader.ReadToEnd();

        if (string.IsNullOrWhiteSpace(securedFilesListJson))
        {
            _securedFilesList = new List<FindFilesLog>();
            return;
        }

        _securedFilesList = JsonConvert.DeserializeObject<IList<FindFilesLog>>(securedFilesListJson)!;
    }

    public IEnumerable<FindFilesLog> GetFindFilesLogs()
    {
        IEnumerable<FindFilesLog> securedfilesLogs = _securedFilesList
            .Where(log =>
                !string.IsNullOrWhiteSpace(log.FilePath) &&
                New<IDataStore>(log.FilePath).IsAvailable
            );

        return securedfilesLogs.OrderByDescending(log => log.DateTime);
    }

    public void Save(FindFilesLog securedfilelog)
    {
        if (securedfilelog == null)
        {
            return;
        }

        if (_securedFilesList.Any(e => e.FilePath.Equals(securedfilelog.FilePath, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _securedFilesList.Add(securedfilelog);
        SaveInternal();
    }

    public void PurgeIfExists(string filePath)
    {
        if (filePath == null)
        {
            return;
        }

        _securedFilesList = _securedFilesList
            .Where(e => !e.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        string extension = Path.GetExtension(filePath)?.Replace(".", "") ?? "";
        filePath = filePath.Replace($".{extension}", $"-{extension}.axx");
        _securedFilesList = _securedFilesList
            .Where(e => !e.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        SaveInternal();
    }

    public void Clear()
    {
        _securedFilesList.Clear();
        SaveInternal();
    }

    private void SaveInternal()
    {
        using Stream writeStream = _findFilesLogStore.OpenWrite();
        using StreamWriter writer = new StreamWriter(writeStream);

        string securedfilejson = "[]";
        if (_securedFilesList != null && _securedFilesList.Any())
        {
            securedfilejson = JsonConvert.SerializeObject(_securedFilesList, Formatting.Indented);
        }

        writer.Write(securedfilejson);
    }
}

public class FindFilesLog
{
    public DateTime DateTime { get; set; }

    public string FilePath { get; set; } = "";
}