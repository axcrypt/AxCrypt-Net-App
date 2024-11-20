using AxCrypt.Abstractions;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.IO;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.Core;

namespace AxCrypt.App.Components.ViewModels;

public class FileDetails : Core.UI.ViewModel.ViewModelBase
{
    private ActiveFile _activeFile;
    private LogOnIdentity _identity;

    public FileDetails()
    {
        SharedWith = new List<string>();
    }

    public FileDetails(ActiveFile file)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }
        _identity = New<KnownIdentities>().DefaultEncryptionIdentity;

        _activeFile = file;
        FileName = file.DecryptedFileInfo.Name;
        FileSize = _activeFile.Size();
        FileExtension = Path.GetExtension(file.DecryptedFileInfo.Name);
        LastModifiedDate = file.Properties.LastActivityTimeUtc.ToString();
        LastAccessedDate = file.DecryptedFileInfo.LastAccessTimeUtc.ToString();
        Algorithm = Resolve.CryptoFactory.Create(file.Properties.CryptoId).Name;
        FilePath = file.EncryptedFileInfo.FullName;
        FileExt = GetFileExtention(file.DecryptedFileInfo.Name);
        SharedWith = new List<string>();
        LoadPropertiesAsync();
    }

    public string FileName
    {
        get { return GetProperty<string>(nameof(FileName)); }
        set { SetProperty(nameof(FileName), value); }
    }

    public string FileSize
    {
        get { return GetProperty<string>(nameof(FileSize)); }
        set { SetProperty(nameof(FileSize), value); }
    }

    public string FileSizeFormatted
    {
        get { return GetProperty<string>(nameof(FileSizeFormatted)); }
        set { SetProperty(nameof(FileSizeFormatted), value); }
    }

    public string FileExtension
    {
        get { return GetProperty<string>(nameof(FileExtension)); }
        set { SetProperty(nameof(FileExtension), value); }
    }

    public string LastModifiedDate
    {
        get { return GetProperty<string>(nameof(LastModifiedDate)); }
        set { SetProperty(nameof(LastModifiedDate), value); }
    }

    public string LastAccessedDate
    {
        get { return GetProperty<string>(nameof(LastAccessedDate)); }
        set { SetProperty(nameof(LastAccessedDate), value); }
    }

    public string Algorithm
    {
        get { return GetProperty<string>(nameof(Algorithm)); }
        set { SetProperty(nameof(Algorithm), value); }
    }

    public string FilePath
    {
        get { return GetProperty<string>(nameof(FilePath)); }
        set { SetProperty(nameof(FilePath), value); }
    }

    public bool IsChecked
    {
        get { return GetProperty<bool>(nameof(IsChecked)); }
        set { SetProperty(nameof(IsChecked), value); }
    }

    public IReadOnlyCollection<string> SharedWith
    {
        get { return GetProperty<IReadOnlyCollection<string>>(nameof(SharedWith)); }
        set { SetProperty(nameof(SharedWith), value); }
    }

    public ActiveFile ActiveFile
    {
        get
        {
            return _activeFile;
        }
    }

    private async void LoadPropertiesAsync()
    {
        if (!_activeFile.IsShared && !_activeFile.IsMasterKeyShared)
        {
            return;
        }

        string ownAccount = _identity.UserEmail.Address;
        EncryptedProperties properties = await LoadPropertiesAsync(_activeFile.EncryptedFileInfo, _activeFile.Identity);
        if (properties == null)
        {
            return;
        }
        SharedWith = properties.SharedKeyHolders.Select(key => key.Email.Address).Where(address => address != ownAccount).ToList().Any() ? properties.SharedKeyHolders.Select(key => key.Email.Address).Where(address => address != ownAccount).ToList() : new List<string>();
    }

    public async Task<EncryptedProperties> LoadPropertiesAsync(IDataStore file, LogOnIdentity identity)
    {
        try
        {
            if (identity == LogOnIdentity.Empty)
            {
                identity = New<KnownIdentities>().DefaultEncryptionIdentity;
            }

            return EncryptedProperties.Create(file, identity);
        }
        catch (Exception ex)
        {
            New<IReport>().Exception(ex);
            return null;
        }
    }

    public string FileExt { get; set; } /*= GetFileExtention(FileName);*/

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }
        FileDetails other = (FileDetails)obj;
        return FileName == other.FileName && FilePath == other.FilePath;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FileName, FilePath);
    }

    public string GetFileExtention(string fileExt)
    {
        if (string.IsNullOrEmpty(fileExt)) return string.Empty;

        string extention = Path.GetExtension(fileExt);

        return extention.StartsWith(".") ? extention.Substring(1) : extention;
    }
}