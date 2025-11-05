using AxCrypt.App.Shared.Utility;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.ViewModels;

public class FileDetails : Core.UI.ViewModel.ViewModelBase
{
    private ActiveFile _activeFile;

    public FileDetails(ActiveFile file)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        InitializeListItemProperties(file);
    }

    private void InitializeListItemProperties(ActiveFile file)
    {
        FileName = file.DecryptedFileInfo.Name;
        FileSize = file.EncryptedFileInfo.IsAvailable ? file.Size() : "0 bytes";
        Algorithm = Resolve.CryptoFactory.Create(file.Properties.CryptoId).Name;
        LastModifiedDate = file.EncryptedFileInfo.LastWriteTimeUtc.ToLocalTime().ToString(CultureInfo.CurrentCulture);
        FilePath = file.EncryptedFileInfo.FullName;
        CleanUpNeeded = file.IsDecrypted;

        FileExtension = Path.GetExtension(file.DecryptedFileInfo.Name);
        LastAccessedDate = file.Properties.LastActivityTimeUtc.ToLocalTime().ToString(CultureInfo.CurrentCulture);
        SharedWith = new List<string>();

        SetIconClass(file);
        InitializeOtherProperties(file);
    }

    private void InitializeOtherProperties(ActiveFile activeFile)
    {
        LogOnIdentity decryptIdentity = ValidateActiveFileIdentity(activeFile.Identity);
        IAxCryptDocument document = activeFile.EncryptedFileInfo.GetAxCryptDocument(decryptIdentity);
        if (document == null)
        {
            return;
        }

        IEnumerable<string> keySharedUsers = document.AsymmetricRecipients.Select(ksr => ksr.Email.Address).Distinct().Skip(1);
        if (keySharedUsers.Any())
        {
            SharedWith = keySharedUsers.ToList();
            UpdateViewState();
        }
    }

    private static LogOnIdentity ValidateActiveFileIdentity(LogOnIdentity activeFileIdentity)
    {
        if (activeFileIdentity != LogOnIdentity.Empty)
        {
            return activeFileIdentity;
        }

        return New<KnownIdentities>().DefaultEncryptionIdentity;
    }

    public ActiveFile ActiveFile
    {
        get
        {
            return _activeFile;
        }
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

    public string Algorithm
    {
        get { return GetProperty<string>(nameof(Algorithm)); }
        set { SetProperty(nameof(Algorithm), value); }
    }

    public string LastModifiedDate
    {
        get { return GetProperty<string>(nameof(LastModifiedDate)); }
        set { SetProperty(nameof(LastModifiedDate), value); }
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

    public bool CleanUpNeeded
    {
        get { return GetProperty<bool>(nameof(CleanUpNeeded)); }
        set { SetProperty(nameof(CleanUpNeeded), value); }
    }

    public IReadOnlyCollection<string> SharedWith
    {
        get { return GetProperty<IReadOnlyCollection<string>>(nameof(SharedWith)); }
        set { SetProperty(nameof(SharedWith), value); }
    }

    public string FileExtension
    {
        get { return GetProperty<string>(nameof(FileExtension)); }
        set { SetProperty(nameof(FileExtension), value); }
    }

    public string LastAccessedDate
    {
        get { return GetProperty<string>(nameof(LastAccessedDate)); }
        set { SetProperty(nameof(LastAccessedDate), value); }
    }

    public string IconClass
    {
        get { return GetProperty<string>(nameof(IconClass)); }
        set { SetProperty(nameof(IconClass), value); }
    }

    private void SetIconClass(ActiveFile activeFile)
    {
        if (activeFile.IsShared)
        {
            IconClass = "file-keySh-icon";
        }

        if (activeFile.IsMasterKeyShared)
        {
            IconClass = "file-masKey-icon";
        }

        if (activeFile.IsShared && activeFile.IsMasterKeyShared)
        {
            IconClass = "file-masshk-icon";
        }

        if (activeFile.IsDecrypted)
        {
            IconClass = "file-dec-icon";
            return;
        }

        if (IconClass == null)
        {
            IconClass = activeFile.DecryptedFileInfo.Name.GetIcon();
        }
    }

    //public override bool Equals(object obj)
    //{
    //    if (obj == null || GetType() != obj.GetType())
    //    {
    //        return false;
    //    }
    //    FileDetails other = (FileDetails)obj;
    //    return FileName == other.FileName && FilePath == other.FilePath;
    //}

    //public override int GetHashCode()
    //{
    //    return HashCode.Combine(FileName, FilePath);
    //}
}