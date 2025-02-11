using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
using AxCrypt.Core;
using AxCrypt.Core.Extensions;
using static AxCrypt.Abstractions.TypeResolve;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;

namespace AxCrypt.App.Desktop.Implementation;

public class FileWatcher : IFileWatcher
{
    private IDataStore _fileInfo;

    private FileSystemWatcher _fileSystemWatcher;

    private DelayedAction _delayedAction;

    private List<FileWatcherEventArgs> _notifications = new List<FileWatcherEventArgs>();

    public FileWatcher(string path, DelayedAction delayedAction)
    {
        if (delayedAction == null)
        {
            throw new ArgumentNullException("delayedAction");
        }

        _delayedAction = delayedAction;
        _delayedAction.Action += (sender, e) => { OnDelayedNotification(); };

        _fileInfo = New<IDataStore>(path);
        _fileSystemWatcher = new FileSystemWatcher(_fileInfo.FullName);
        _fileSystemWatcher.Created += (sender, e) => FileSystemChanged(new FileWatcherEventArgs(e.FullPath));
        _fileSystemWatcher.Deleted += (sender, e) => FileSystemChanged(new FileWatcherEventArgs(e.FullPath));
        _fileSystemWatcher.Renamed += (sender, e) => FileSystemChanged(new FileWatcherEventArgs(new string[] { e.OldFullPath, e.FullPath }));
        _fileSystemWatcher.Error += (sender, e) => FileSystemChanged(new FileWatcherEventArgs(_fileInfo.FullName));

        _fileSystemWatcher.Filter = String.Empty;
        _fileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime;
        _fileSystemWatcher.EnableRaisingEvents = true;
    }

    public bool IncludeSubdirectories
    {
        get
        {
            return _fileSystemWatcher.IncludeSubdirectories;
        }
        set
        {
            if (_fileSystemWatcher.IncludeSubdirectories == value)
            {
                return;
            }
            _fileSystemWatcher.EnableRaisingEvents = false;
            _fileSystemWatcher.IncludeSubdirectories = value;
            _fileSystemWatcher.EnableRaisingEvents = true;
        }
    }

    protected virtual void OnDelayedNotification()
    {
        List<FileWatcherEventArgs> notifications;
        lock (_notifications)
        {
            if (!_notifications.Any())
            {
                return;
            }
            notifications = new List<FileWatcherEventArgs>(_notifications);
            _notifications.Clear();
        }

        HashSet<string> allFiles = new HashSet<string>();
        foreach (FileWatcherEventArgs notification in notifications)
        {
            allFiles.UnionWith(notification.FullNames);
        }

        if (!allFiles.Any())
        {
            return;
        }

        OnChanged(new FileWatcherEventArgs(allFiles));
    }

    protected virtual void OnChanged(FileWatcherEventArgs eventArgs)
    {
        EventHandler<FileWatcherEventArgs> fileChanged = FileChanged;
        if (fileChanged != null)
        {
            fileChanged(null, eventArgs);
        }
    }

    private void FileSystemChanged(FileWatcherEventArgs e)
    {
        if (Resolve.Log.IsInfoEnabled)
        {
            Resolve.Log.LogInfo("Watcher says {0} changed.".InvariantFormat(e.FullNames.Aggregate(String.Empty, (acc, fullName) => acc + "'" + fullName + "' ")));
        }
        lock (_notifications)
        {
            _notifications.Add(e);
        }
        _delayedAction.StartIdleTimer();
    }

    #region IFileWatcher Members

    public event EventHandler<FileWatcherEventArgs> FileChanged;

    #endregion IFileWatcher Members

    private bool _disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        if (disposing)
        {
            if (_fileSystemWatcher != null)
            {
                _fileSystemWatcher.Dispose();
                _fileSystemWatcher = null;
            }
            if (_delayedAction != null)
            {
                _delayedAction.Dispose();
                _delayedAction = null;
            }
        }
        _disposed = true;
    }

    #region IDisposable Members

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion IDisposable Members
}
