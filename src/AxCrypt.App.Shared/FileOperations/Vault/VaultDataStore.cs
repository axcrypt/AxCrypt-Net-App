using System;
using System.Collections.Generic;
using System.Linq;
using AxCrypt.Abstractions;
using AxCrypt.Core;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.FileOperations.Vault;

public interface IVaultDataStore
{
    VaultDataStore Create(string filePath, string saveFilePath);

    VaultDataStore Create(IDataStore fileStore, string currentPath);

    public IDataContainer BaseContainer { get; }

    public IDataStore File { get; }

    public IDataContainer CurrentPath { get; }
}

public class VaultDataStore : IVaultDataStore
{
    IDataStore _fileInfo;

    public VaultDataStore() { }

    private string? _currentPath = null;

    public VaultDataStore Create(string path, string currentPath)
    {
        _currentPath = currentPath;
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        string normalized = path.NormalizeFilePath();
        try
        {
            _fileInfo = New<IDataStore>(normalized);
            return this;
        }
        catch (Exception ex)
        {
            throw new FileOperationException(
                $"Can't create {nameof(VaultDataStore)}.",
                normalized,
                ErrorStatus.Exception,
                ex
            );
        }
    }

    public VaultDataStore Create(IDataStore fileStore, string currentPath)
    {
        _fileInfo = fileStore;
        _currentPath = currentPath;
        return this;
    }

    public IDataContainer BaseContainer
    {
        get { return New<IDataContainer>(Resolve.UserSettings.VaultEncryptDataPath); }
    }

    public IDataContainer CurrentPath
    {
        get
        {
            if (_currentPath == null)
            {
                return BaseContainer;
            }

            return New<IDataContainer>(_currentPath);
        }
    }

    public IDataStore File
    {
        get { return _fileInfo; }
    }
}
